using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only guest-response fact recorded on successful send. Channel
    /// delivery may be stubbed; the fact is the source of truth for activity.
    /// AuthorDisplayName and MaskedDestination are snapshotted at create.
    /// </summary>
    public class FeedbackGuestResponse
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public FeedbackGuestResponseChannel Channel { get; set; }

        public FeedbackRecoveryIntent Intent { get; set; }

        [Required]
        [MaxLength(200)]
        public string MaskedDestination { get; set; }
            = string.Empty;

        [MaxLength(300)]
        public string? Subject { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Body { get; set; }
            = string.Empty;

        [MaxLength(80)]
        public string? Purpose { get; set; }

        [MaxLength(80)]
        public string? Tone { get; set; }

        [MaxLength(1000)]
        public string? IncludeNotes { get; set; }

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
