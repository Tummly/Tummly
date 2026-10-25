using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Restaurant-scoped contact hash after guest withdrawal / delete / STOP
    /// (WS2a). No plain contact or name on the row.
    /// </summary>
    public class GuestContactSuppression
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant? Restaurant { get; set; }

        public LocationGuestPermissionKind PermissionKind { get; set; }

        [Required]
        [MaxLength(64)]
        public string ContactHash { get; set; } = string.Empty;

        public DateTime WithdrawnAtUtc { get; set; }

        [Required]
        [MaxLength(32)]
        public string Source { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }

    public static class GuestContactSuppressionSources
    {
        public const string GuestDelete = "guest-delete";

        public const string RetentionPurge = "retention-purge";

        public const string EmailUnsubscribe = "email-unsubscribe";

        public const string SmsStop = "sms-stop";
    }
}
