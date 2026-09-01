using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class LocationActivityKinds
    {
        public const string LocationCreated = "location-created";
        public const string LifecycleChanged = "lifecycle-changed";
        public const string ManagerChanged = "manager-changed";
        public const string LocationEdited = "location-edited";
        public const string ConsentCopyChanged = "consent-copy-changed";
        public const string PrivacyReviewCompleted = "privacy-review-completed";
        public const string GuestMarketingUnsubscribed =
            "guest-marketing-unsubscribed";
        public const string GuestPermissionToggleChanged =
            "guest-permission-toggle-changed";
    }

    /// <summary>
    /// Operator Settings Locations activity (Activity tab + write audit).
    /// Distinct from RestaurantAccessActivity and LocationGuestActivity.
    /// </summary>
    public class LocationActivity
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        public int? LocationId { get; set; }

        public RestaurantLocation? Location { get; set; }

        public int ActorUserId { get; set; }

        [MaxLength(150)]
        public string? ActorDisplayName { get; set; }

        [Required]
        [MaxLength(40)]
        public string Kind { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Description { get; set; }

        [MaxLength(400)]
        public string? FromValue { get; set; }

        [MaxLength(400)]
        public string? ToValue { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
