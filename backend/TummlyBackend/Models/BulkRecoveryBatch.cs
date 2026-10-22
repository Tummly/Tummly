using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Shared guest-response draft for many Feedback rows. Send still writes
    /// one <see cref="FeedbackGuestResponse"/> per member.
    /// </summary>
    public class BulkRecoveryBatch
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant? Restaurant { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public int CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public FeedbackGuestResponseChannel Channel { get; set; }

        public FeedbackRecoveryIntent Intent { get; set; }

        [MaxLength(300)]
        public string? Subject { get; set; }

        [Required]
        [MaxLength(5000)]
        public string BodyTemplate { get; set; }
            = string.Empty;

        [MaxLength(80)]
        public string? Purpose { get; set; }

        [MaxLength(80)]
        public string? Tone { get; set; }

        [MaxLength(1000)]
        public string? IncludeNotes { get; set; }

        public BulkRecoveryBatchStatus Status { get; set; }
            = BulkRecoveryBatchStatus.Draft;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public List<BulkRecoveryBatchMember> Members { get; set; } = [];
    }
}
