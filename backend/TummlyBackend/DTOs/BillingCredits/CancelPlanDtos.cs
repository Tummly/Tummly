namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class CancelPlanResultDto
    {
        public bool Success { get; set; } = true;

        public string ScheduledChangeLine { get; set; } = string.Empty;
    }
}
