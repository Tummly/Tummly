using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only recovery-completion fact: success-screen Mark resolved
    /// (one-click → Resolved, no close-out reasons). Links the status-change
    /// that reached Resolved. AuthorDisplayName is snapshotted at create.
    /// </summary>
    public class FeedbackRecoveryCompletion
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public FeedbackRecoveryIntent Intent { get; set; }

        public int WorkflowStatusChangeId { get; set; }

        public FeedbackWorkflowStatusChange? WorkflowStatusChange { get; set; }

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
