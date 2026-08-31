using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Operator Settings location activity (Settings Locations Activity tab).
    /// Distinct from <see cref="LocationGuestActivityEvent"/>.
    /// </summary>
    public static class LocationSettingsActivityKinds
    {
        public const string LifecycleChanged = "lifecycle-changed";

        public const string LocationCreated = "location-created";

        public const string ManagerChanged = "manager-changed";

        public const string LocationEdited = "location-edited";

        public const string ConsentCopyChanged = "consent-copy-changed";

        public const string PrivacyReviewCompleted = "privacy-review-completed";
    }

    public class LocationSettingsActivityEvent
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
        [MaxLength(64)]
        public string Kind { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(4000)]
        public string? ParamsJson { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
