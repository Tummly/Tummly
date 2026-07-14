using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class TrackingOperatorNotificationsService
        : IOperatorNotificationsService
    {
        public List<ProduceNotificationRequest> Produced { get; } = [];

        public ProduceNotificationResult NextResult { get; set; } =
            new() { Status = ProduceNotificationStatus.Created };

        public Exception? ThrowOnProduce { get; set; }

        public Task<ProduceNotificationResult> ProduceAsync(
            ProduceNotificationRequest request
        )
        {
            if (ThrowOnProduce != null)
            {
                throw ThrowOnProduce;
            }

            Produced.Add(request);
            return Task.FromResult(NextResult);
        }

        public Task<IReadOnlyList<NotificationDto>> ListAsync(int userId) =>
            Task.FromResult<IReadOnlyList<NotificationDto>>([]);

        public Task<int> GetUnreadCountAsync(int userId) =>
            Task.FromResult(0);

        public Task<bool> MarkOneReadAsync(int userId, int notificationId) =>
            Task.FromResult(false);

        public Task<int> MarkInboxReadAsync(int userId) =>
            Task.FromResult(0);

        public Task<int> MarkVisibleReadAsync(
            int userId,
            NotificationListFilter filter
        ) => Task.FromResult(0);

        public Task<NotificationPreferencesDto> GetPreferencesAsync(
            int userId
        ) => Task.FromResult(new NotificationPreferencesDto());

        public Task<NotificationPreferencesDto> SetPreferencesAsync(
            int userId,
            NotificationPreferencesDto preferences
        ) => Task.FromResult(preferences);

        public Task<EnsureSeedsResult> EnsureSeedsAsync(int userId) =>
            Task.FromResult(new EnsureSeedsResult());
    }
}
