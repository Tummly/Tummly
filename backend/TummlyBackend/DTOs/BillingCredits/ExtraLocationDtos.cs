namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class ExtraLocationResultDto
    {
        public bool Success { get; set; } = true;

        public string Outcome { get; set; } = string.Empty;

        public string? RedirectUrl { get; set; }

        public string? ScheduledChangeLine { get; set; }
    }
}
