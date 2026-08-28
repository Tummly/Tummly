namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class PlanChangeRequestDto
    {
        public string TargetPlan { get; set; } = string.Empty;

        public string TargetCadence { get; set; } = "monthly";
    }

    public sealed class PlanChangeResultDto
    {
        public bool Success { get; set; } = true;

        public string Outcome { get; set; } = string.Empty;

        public string? RedirectUrl { get; set; }

        public string? ScheduledChangeLine { get; set; }
    }
}
