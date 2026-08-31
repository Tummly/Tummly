using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class CancelPlanRequestDto
    {
        [Required]
        [MaxLength(64)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AdditionalNotes { get; set; }
    }
}
