using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SmartTrip.UI.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[SignalR] Користувач підключився. UserId: {userId ?? "NULL"}");
            
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }
    }
}