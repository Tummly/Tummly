using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only operator replace of Feedback Detected Tags (Issue tags).
    /// On Failed → Succeeded promote, FromSentiment/ToSentiment are set;
    /// on Succeeded tags-only replace both are null.
    /// </summary>
    public class FeedbackDetectedTagsChange
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        /// <summary>JSON array of DetectedTag keys before the change.</summary>
        [Required]
        [MaxLength(500)]
        public string FromTagsJson { get; set; }
            = "[]";

        /// <summary>JSON array of DetectedTag keys after the change.</summary>
        [Required]
        [MaxLength(500)]
        public string ToTagsJson { get; set; }
            = "[]";

        public FeedbackSentiment? FromSentiment { get; set; }

        public FeedbackSentiment? ToSentiment { get; set; }

        public int? AuthorUserId { get; set; }

        public User? AuthorUser { get; set; }

        [Required]
        [MaxLength(150)]
        public string AuthorDisplayName { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
