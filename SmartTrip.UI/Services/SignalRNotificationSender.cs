using Microsoft.AspNetCore.SignalR;
using SmartTrip.Application.Interfaces;
using SmartTrip.UI.Hubs;

namespace SmartTrip.UI.Services
{
    public class SignalRNotificationSender : INotificationSender
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationSender(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(string userId, string title, string message)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", title, message);
            }
        }
    }
}