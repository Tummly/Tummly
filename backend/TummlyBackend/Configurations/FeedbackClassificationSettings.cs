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
        /// <c>Fake</c> (tests/local demos) or <c>AzureOpenAI</c> (production default path).
        /// </summary>
        public string Provider { get; set; } = "Fake";

        public string Endpoint { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Mini-tier deployment / model id (e.g. gpt-4o-mini).</summary>
        public string DeploymentName { get; set; } = "gpt-4o-mini";

        public string ApiVersion { get; set; } = "2024-08-01-preview";

        public string Region { get; set; } = string.Empty;

        public string PromptSchemaVersion { get; set; } = "2026-07-16";

        /// <summary>Bounded attempts for transient / invalid-output retries.</summary>
        public int MaxAttempts { get; set; } = 3;

        public int InitialBackoffMilliseconds { get; set; } = 250;
    }
}
