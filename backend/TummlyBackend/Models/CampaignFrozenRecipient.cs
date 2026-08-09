using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Recipient freeze row captured at Campaign schedule commit (ticket 26).
    /// Fire / resume / retry may drop only — never silent add.
    /// </summary>
    public class CampaignFrozenRecipient
    {
        public int Id { get; set; }

        public int CampaignId { get; set; }

        public Campaign? Campaign { get; set; }

        public int LocationGuestId { get; set; }

        public LocationGuest? LocationGuest { get; set; }

        public DateTime FrozenAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Set when the provider accepts the outbound message (ticket 31 fire/settle).
        /// Cancel remaining uses this to choose Partially sent vs Cancelled (ticket 30).
        /// </summary>
        public DateTime? AcceptedAtUtc { get; set; }
    }
}
