using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only Location Guest activity event. Domain writes emit rows at
    /// write time; Guest Profile Activity and Feedback details history read
    /// this store. Home Latest activity does not.
    /// </summary>
    public class LocationGuestActivityEvent
    {
        public int Id { get; set; }

        /// <summary>
        /// Nullable so feedback-keyed events can survive Location Guest delete
        /// via SET NULL while Feedback rows are retained.
        /// </summary>
        public int? LocationGuestId { get; set; }

        public LocationGuest? LocationGuest { get; set; }

        /// <summary>
        /// Optional Feedback link for feedback / classification kinds.
        /// </summary>
        public int? FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        [Required]
        [MaxLength(64)]
        public string Kind { get; set; }
            = string.Empty;

        /// <summary>
        /// Kind-specific JSON snapshot (e.g. changedFields, tagName, sentiment).
        /// </summary>
        [MaxLength(4000)]
        public string? PayloadJson { get; set; }

        public DateTime OccurredAt { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
