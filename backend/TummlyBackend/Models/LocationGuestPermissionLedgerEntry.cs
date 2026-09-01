using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only Location Guest permission ledger event. Current permission
    /// state is the latest row per (guest, permission kind, location).
    /// </summary>
    public class LocationGuestPermissionLedgerEntry
    {
        public int Id { get; set; }

        public int LocationGuestId { get; set; }

        public LocationGuest LocationGuest { get; set; } = null!;

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation RestaurantLocation { get; set; } = null!;

        public LocationGuestPermissionKind PermissionKind { get; set; }

        [Required]
        [MaxLength(16)]
        public string EventKind { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string Source { get; set; } = string.Empty;

        public int? ActorUserId { get; set; }

        public User? ActorUser { get; set; }

        public DateTime OccurredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class LocationGuestPermissionLedgerEventKinds
    {
        public const string Grant = "grant";

        public const string Withdraw = "withdraw";
    }

    public static class LocationGuestPermissionLedgerSources
    {
        public const string LegacyMarketingPreference =
            "legacy-marketing-preference";

        public const string GuestForm = "guest-form";

        public const string Operator = "operator";
    }
}
