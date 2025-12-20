using Microsoft.AspNetCore.SignalR;

namespace web_chothue_laptop.Hubs
{
    public class BookingHub : Hub
    {
        public async Task JoinCustomerGroup(long customerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"customer_{customerId}");
        }

        public async Task LeaveCustomerGroup(long customerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"customer_{customerId}");
        }
    }
}





