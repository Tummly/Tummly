using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only operator change of Feedback workflow status.
    /// AuthorDisplayName is snapshotted at create; AuthorUserId uses NoAction on
    /// user delete (SQL Server rejects dual SET NULL paths with Feedback cascade).
    /// </summary>
    public class FeedbackWorkflowStatusChange
    {
        public int Id { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public FeedbackWorkflowStatus FromStatus { get; set; }

        public FeedbackWorkflowStatus ToStatus { get; set; }

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
