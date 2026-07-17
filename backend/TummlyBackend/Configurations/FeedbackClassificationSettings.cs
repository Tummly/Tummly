namespace TummlyBackend.Configurations
{
    /// <summary>
    /// Azure OpenAI Structured Outputs settings for Feedback AI classification.
    /// Credentials stay backend-only.
    /// </summary>
    public class FeedbackClassificationSettings
    {
        public const string SectionName = "FeedbackClassification";

        /// <summary>
        /// <c>AzureOpenAI</c> (production default) or <c>Fake</c> (tests/local demos).
        /// </summary>
        public string Provider { get; set; } = "AzureOpenAI";

        public string Endpoint { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Mini-tier deployment / model id (e.g. gpt-4o-mini).</summary>
        public string DeploymentName { get; set; } = "gpt-4o-mini";

        public string ApiVersion { get; set; } = "2024-08-01-preview";

        public string Region { get; set; } = string.Empty;

        public string PromptSchemaVersion { get; set; } = "2026-07-16";

        /// <summary>
        /// Implementation-default attempt budget for transient / invalid-output retries
        /// inside one provider call (not a product-facing knob).
        /// Distinct from <see cref="MaxClaimAttempts"/>.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>Implementation-default backoff base in milliseconds.</summary>
        public int InitialBackoffMilliseconds { get; set; } = 250;

        /// <summary>
        /// Per-row soft-claim budget for durable Pending work (ADR-0010).
        /// Exhaustion marks the same generic Failed as other terminal failures.
        /// </summary>
        public int MaxClaimAttempts { get; set; } = 3;

        /// <summary>Soft-claim lease length before another worker may reclaim.</summary>
        public int ClaimLeaseMinutes { get; set; } = 10;

        /// <summary>
        /// How often the worker sweeps unclaimed / lease-expired Pending rows
        /// (in addition to Channel wake-ups and a startup sweep).
        /// </summary>
        public int SweepIntervalSeconds { get; set; } = 30;

        /// <summary>In-flight classifications per process. Default 1.</summary>
        public int MaxDegreeOfParallelism { get; set; } = 1;

        /// <summary>
        /// Max Failed→Pending delayed reopen cycles (ADR-0012). Default 5.
        /// </summary>
        public int MaxDelayedReopens { get; set; } = 5;

        /// <summary>Initial delay before first delayed reopen. Default 5 minutes.</summary>
        public int DelayedRequeueInitialDelayMinutes { get; set; } = 5;

        /// <summary>Cap between delayed reopens. Default 60 minutes.</summary>
        public int DelayedRequeueMaxDelayMinutes { get; set; } = 60;
    }
}
