using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Platform-level identity of a person who uses the guest preference centre.
    /// Distinct from <see cref="MasterGuest"/> (restaurant-scoped). Restaurants
    /// never see this row.
    /// </summary>
    public class TummlyGuest
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<TummlyGuestContact> Contacts { get; set; } = [];

        public List<TummlyGuestMarketingEvent> MarketingEvents { get; set; } = [];

        public List<TummlyGuestLocationMute> LocationMutes { get; set; } = [];
    }

    /// <summary>
    /// A verified email or phone owned by one <see cref="TummlyGuest"/>.
    /// One owner per (ContactType, NormalizedValue).
    /// </summary>
    public class TummlyGuestContact
    {
        public int Id { get; set; }

        public int TummlyGuestId { get; set; }

        public TummlyGuest TummlyGuest { get; set; } = null!;

        [Required]
        [MaxLength(16)]
        public string ContactType { get; set; } = string.Empty;

        [Required]
        [MaxLength(320)]
        public string NormalizedValue { get; set; } = string.Empty;

        public DateTime VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Append-only Tummly marketing consent event. Current state is the latest
    /// row per (guest, channel), as with the Location Guest permission ledger.
    /// </summary>
    public class TummlyGuestMarketingEvent
    {
        public int Id { get; set; }

        public int TummlyGuestId { get; set; }

        public TummlyGuest TummlyGuest { get; set; } = null!;

        [Required]
        [MaxLength(16)]
        public string Channel { get; set; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string EventKind { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string Source { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? WordingSnapshot { get; set; }

        public DateTime OccurredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A Tummly Guest's choice that one Owned location must not market to them
    /// and must not feature in Tummly marketing.
    /// </summary>
    public class TummlyGuestLocationMute
    {
        public int Id { get; set; }

        public int TummlyGuestId { get; set; }

        public TummlyGuest TummlyGuest { get; set; } = null!;

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation RestaurantLocation { get; set; } = null!;

        public DateTime MutedAt { get; set; }
    }

    public static class TummlyGuestContactTypes
    {
        public const string Email = "email";

        public const string Phone = "phone";
    }

    public static class TummlyGuestChannels
    {
        public const string Email = "email";

        public const string Sms = "sms";
    }
}
