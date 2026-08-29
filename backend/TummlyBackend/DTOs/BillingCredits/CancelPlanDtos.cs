namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class CancelPlanResultDto
    {
        public bool Success { get; set; } = true;

        public string Outcome { get; set; } = string.Empty;

        public string? ScheduledChangeLine { get; set; }
    }
}
