using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only internal-action fact recorded on successful Record confirm.
    /// Does not change workflow status. AuthorDisplayName and CategoryLabel are
    /// snapshotted at create.
    /// </summary>
    public class FeedbackInternalAction
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public FeedbackInternalActionCategory Category { get; set; }

        [Required]
        [MaxLength(120)]
        public string CategoryLabel { get; set; }
            = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Note { get; set; }
            = string.Empty;

        public FeedbackRecoveryIntent Intent { get; set; }

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
