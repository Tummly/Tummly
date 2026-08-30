using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Campaign row — Draft through lifecycle statuses (ticket 26 schedule commit).
    /// </summary>
    public class Campaign
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        /// <summary>Stored lifecycle status (draft, scheduled, sending, …).</summary>
        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = "draft";

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? GoalId { get; set; }

        [MaxLength(64)]
        public string? TemplateId { get; set; }

        public int? TemplateVersion { get; set; }

        [MaxLength(64)]
        public string? AudienceKey { get; set; }

        [MaxLength(32)]
        public string? Channel { get; set; }

        [MaxLength(64)]
        public string? OfferStance { get; set; }

        /// <summary>
        /// Attached Offers catalog definition (Campaign offer attach). Null when No offer.
        /// </summary>
        public int? OfferId { get; set; }

        public CatalogOffer? Offer { get; set; }

        [MaxLength(500)]
        public string? MessageSubject { get; set; }

        [MaxLength(8000)]
        public string? MessageBody { get; set; }

        /// <summary><c>send-now</c> or <c>schedule-later</c> after commit.</summary>
        [MaxLength(32)]
        public string? ScheduleMode { get; set; }

        /// <summary>UTC fire time when schedule-later; null for send-now.</summary>
        public DateTime? ScheduledAtUtc { get; set; }

        /// <summary>Account / restaurant IANA timezone at commit.</summary>
        [MaxLength(64)]
        public string? ScheduleTimeZone { get; set; }

        /// <summary>Billing reservation reference from live Reserve.</summary>
        [MaxLength(128)]
        public string? BillingReservationRef { get; set; }

        /// <summary>Full credit estimate reserved at commit.</summary>
        public int? ReservedEstimate { get; set; }

        /// <summary>Settled credits on the open Billing reservation.</summary>
        public int SettledUnits { get; set; }

        /// <summary>SQL Server rowversion concurrency token (DB-managed).</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        /// <summary>Operator who created the draft (null for legacy rows).</summary>
        public int? CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
