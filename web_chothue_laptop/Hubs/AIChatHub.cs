using Microsoft.AspNetCore.SignalR;

namespace web_chothue_laptop.Hubs
{
    public class AIChatHub : Hub
    {
        public async Task JoinConversation(string sessionId)
        {
            var groupName = $"ai_chat:{sessionId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveConversation(string sessionId)
        {
            var groupName = $"ai_chat:{sessionId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}