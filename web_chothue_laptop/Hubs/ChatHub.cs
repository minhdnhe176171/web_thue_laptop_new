using Microsoft.AspNetCore.SignalR;
using web_chothue_laptop.Services;

namespace web_chothue_laptop.Hubs
{
    public class ChatHub : Hub
    {
        private readonly RedisService _redisService;

        public ChatHub(RedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task JoinCustomerGroup(long customerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"customer_{customerId}");
            await Clients.Group("staff").SendAsync("CustomerJoined", customerId);
        }

        public async Task JoinStaffGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        }

        public async Task SendMessageToSupport(long customerId, string customerName, string message)
        {
            var conversationId = $"customer_{customerId}";
            var chatMessage = new ChatMessage
            {
                SenderId = customerId.ToString(),
                SenderName = customerName,
                SenderType = "customer",
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            // Save to Redis
            await _redisService.SaveMessageAsync(conversationId, chatMessage);
            await _redisService.AddActiveCustomerAsync(customerId, customerName);

            // Send to all staff
            await Clients.Group("staff").SendAsync("ReceiveMessage", new
            {
                CustomerId = customerId,
                CustomerName = customerName,
                Message = message,
                Timestamp = chatMessage.Timestamp,
                SenderType = "customer"
            });

            // Send confirmation to customer
            await Clients.Group($"customer_{customerId}").SendAsync("MessageSent", new
            {
                Message = message,
                Timestamp = chatMessage.Timestamp
            });
        }

        public async Task SendMessageToCustomer(long customerId, string staffName, string message, long staffId)
        {
            var conversationId = $"customer_{customerId}";
            var chatMessage = new ChatMessage
            {
                SenderId = staffId.ToString(),
                SenderName = staffName,
                SenderType = "staff",
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            // Save to Redis
            await _redisService.SaveMessageAsync(conversationId, chatMessage);

            // Send to customer
            await Clients.Group($"customer_{customerId}").SendAsync("ReceiveMessage", new
            {
                StaffName = staffName,
                Message = message,
                Timestamp = chatMessage.Timestamp,
                SenderType = "staff"
            });

            // Send confirmation to all staff
            await Clients.Group("staff").SendAsync("MessageSent", new
            {
                CustomerId = customerId,
                StaffName = staffName,
                Message = message,
                Timestamp = chatMessage.Timestamp
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}