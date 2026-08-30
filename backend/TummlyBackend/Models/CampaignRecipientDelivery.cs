using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Per Campaign + recipient (+ channel) outbound outcome for idempotent fire
    /// and Submitted/accepted counts (ticket 31 / 29).
    /// </summary>
    public class CampaignRecipientDelivery
    {
        public int Id { get; set; }

        public int CampaignId { get; set; }

        public Campaign? Campaign { get; set; }

        public int LocationGuestId { get; set; }

        public LocationGuest? LocationGuest { get; set; }

        [Required]
        [MaxLength(16)]
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// <c>accepted</c>, <c>skipped-ineligible</c>, or <c>rejected</c>.
        /// </summary>
        [Required]
        [MaxLength(32)]
        public string Outcome { get; set; } = string.Empty;

        public DateTime? AcceptedAtUtc { get; set; }

        /// <summary>Billable units on provider accept (Email = 1, SMS = segments).</summary>
        public int? AcceptedUnits { get; set; }

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
