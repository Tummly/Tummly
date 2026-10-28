namespace TummlyBackend.DTOs.Admin
{
    /// <summary>
    /// Non-secret environment registry for the staff console. Provider blocks
    /// carry configured state and mode only; no keys, tokens or full numbers.
    /// </summary>
    public sealed class AdminEnvironmentDto
    {
        public string EnvironmentName { get; set; } = string.Empty;

        public bool IsProduction { get; set; }

        /// <summary>Build:CommitSha, or "unknown" when not configured.</summary>
        public string CommitSha { get; set; } = "unknown";

        public DateTime ApiStartedAtUtc { get; set; }

        /// <summary>Last applied EF migration id; null when it cannot be read.</summary>
        public string? LatestMigrationId { get; set; }

        public AdminRevolutReadinessDto Revolut { get; set; } = new();

        public AdminResendReadinessDto Resend { get; set; } = new();

        public AdminTwilioReadinessDto Twilio { get; set; } = new();

        public bool VatModeActive { get; set; }
    }

    public sealed class AdminRevolutReadinessDto
    {
        /// <summary>"configured" or "not_configured".</summary>
        public string Status { get; set; } = "not_configured";

        /// <summary>"sandbox", "live", "other" or "unset".</summary>
        public string Mode { get; set; } = "unset";
    }

    public sealed class AdminResendReadinessDto
    {
        /// <summary>"configured" or "not_configured".</summary>
        public string Status { get; set; } = "not_configured";

        public string? SenderDomain { get; set; }
    }

    public sealed class AdminTwilioReadinessDto
    {
        /// <summary>"configured" or "not_configured".</summary>
        public string Status { get; set; } = "not_configured";

        public string? FromNumberMasked { get; set; }
    }
}
