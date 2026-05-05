namespace SmartTrip.Application.Interfaces
{
    public interface INotificationSender
    {
        Task SendNotificationAsync(string userId, string title, string message);
    }
}