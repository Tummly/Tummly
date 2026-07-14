using Microsoft.AspNetCore.SignalR;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Hubs;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class SignalRNotificationRealtimePublisher
        : INotificationRealtimePublisher
    {
        public const string NotificationCreatedEvent = "NotificationCreated";

        private readonly IHubContext<NotificationsHub> _hub;

        public SignalRNotificationRealtimePublisher(
            IHubContext<NotificationsHub> hub
        )
        {
            _hub = hub;
        }

        public Task PublishCreatedAsync(NotificationDto notification)
        {
            return _hub.Clients
                .User(notification.UserId.ToString())
                .SendAsync(NotificationCreatedEvent, notification);
        }
    }
}
