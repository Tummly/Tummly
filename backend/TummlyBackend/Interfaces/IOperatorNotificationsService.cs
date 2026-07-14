using TummlyBackend.DTOs.Notifications;

namespace TummlyBackend.Interfaces
{
    public interface IOperatorNotificationsService
    {
        Task<ProduceNotificationResult> ProduceAsync(
            ProduceNotificationRequest request
        );

        Task<IReadOnlyList<NotificationDto>> ListAsync(int userId);

        Task<int> GetUnreadCountAsync(int userId);

        Task<bool> MarkOneReadAsync(int userId, int notificationId);

        Task<int> MarkInboxReadAsync(int userId);

        Task<int> MarkVisibleReadAsync(
            int userId,
            NotificationListFilter filter
        );

        Task<NotificationPreferencesDto> GetPreferencesAsync(int userId);

        Task<NotificationPreferencesDto> SetPreferencesAsync(
            int userId,
            NotificationPreferencesDto preferences
        );

        Task<EnsureSeedsResult> EnsureSeedsAsync(int userId);
    }
}
