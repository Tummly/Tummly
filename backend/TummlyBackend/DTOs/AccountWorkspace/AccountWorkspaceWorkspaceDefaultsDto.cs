namespace TummlyBackend.DTOs.AccountWorkspace
{
    public sealed class AccountWorkspaceWorkspaceDefaultsDto
    {
        public string WeekStartsOn { get; set; } = "monday";

        public string DefaultReportingPeriod { get; set; } = "7days";

        public string? DefaultCampaignSenderName { get; set; }

        /// <summary>Read-only UK placeholder — not writable in v1.</summary>
        public string DefaultTimezone { get; set; } = "Europe/London";

        /// <summary>Read-only UK placeholder — not writable in v1.</summary>
        public string DefaultCurrency { get; set; } = "GBP";

        /// <summary>Read-only UK placeholder — not writable in v1.</summary>
        public string DefaultLanguage { get; set; } = "English";

        /// <summary>Read-only UK placeholder — not writable in v1.</summary>
        public string DateFormat { get; set; } = "DD/MM/YYYY";
    }

    public sealed class UpdateWorkspaceDefaultsRequest
    {
        public string? WeekStartsOn { get; set; }

        public string? DefaultReportingPeriod { get; set; }

        public string? DefaultCampaignSenderName { get; set; }
    }
}
