using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class NullNotificationRealtimePublisher
        : INotificationRealtimePublisher
    {
        public Task PublishCreatedAsync(NotificationDto notification) =>
            Task.CompletedTask;
    }
}
