using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only Feedback close-out fact. Links the status-change that reached
    /// Resolved and, when reason is Other, the Feedback internal note created
    /// in the same transaction. AuthorDisplayName is snapshotted at create.
    /// </summary>
    public class FeedbackCloseOut
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public FeedbackCloseOutIntent Intent { get; set; }

        public FeedbackCloseOutReason Reason { get; set; }

        public int WorkflowStatusChangeId { get; set; }

        public FeedbackWorkflowStatusChange? WorkflowStatusChange { get; set; }

        public int? InternalNoteId { get; set; }

        public FeedbackInternalNote? InternalNote { get; set; }

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
