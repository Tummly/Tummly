using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class BulkRecoveryBatchMember
    {
        public int Id { get; set; }

        public int BatchId { get; set; }

        public BulkRecoveryBatch? Batch { get; set; }

        public int FeedbackId { get; set; }

        public Feedback? Feedback { get; set; }

        public BulkRecoveryBatchMemberStatus Status { get; set; }
            = BulkRecoveryBatchMemberStatus.Pending;

        public int? FeedbackGuestResponseId { get; set; }

        public FeedbackGuestResponse? FeedbackGuestResponse { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
