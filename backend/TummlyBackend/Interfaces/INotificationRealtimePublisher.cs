using TummlyBackend.DTOs.Notifications;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Pushes newly created Notifications to the recipient's SignalR connection.
    /// </summary>
    public interface INotificationRealtimePublisher
    {
        Task PublishCreatedAsync(NotificationDto notification);
    }
}
