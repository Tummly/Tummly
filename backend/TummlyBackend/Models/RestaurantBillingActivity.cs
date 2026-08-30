using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class RestaurantBillingActivity
    {
        public long Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        [Required]
        [MaxLength(40)]
        public string Kind { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(150)]
        public string? ActorDisplayName { get; set; }

        [MaxLength(16)]
        public string? Channel { get; set; }

        public int? Qty { get; set; }

        [MaxLength(200)]
        public string? CampaignName { get; set; }

        [MaxLength(32)]
        public string? InvoiceNo { get; set; }

        [MaxLength(32)]
        public string? CreditNoteNo { get; set; }

        [MaxLength(32)]
        public string? Plan { get; set; }

        [MaxLength(16)]
        public string? Cadence { get; set; }

        [MaxLength(64)]
        public string? ScheduledDateLabel { get; set; }

        [MaxLength(200)]
        public string? LocationName { get; set; }

        [MaxLength(16)]
        public string? ManualAdjustDirection { get; set; }

        [MaxLength(32)]
        public string? ConsumeSource { get; set; }

        [MaxLength(32)]
        public string? FromPlan { get; set; }

        [MaxLength(16)]
        public string? FromCadence { get; set; }

        [MaxLength(32)]
        public string? ToPlan { get; set; }

        [MaxLength(16)]
        public string? ToCadence { get; set; }
    }
}
