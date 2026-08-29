namespace TummlyBackend.Configurations
{
    /// <summary>
    /// Revolut Merchant API settings (<c>Revolut__*</c>). Sandbox and live use
    /// separate hosts, secrets, and plan-variation maps.
    /// </summary>
    public class RevolutSettings
    {
        public const string SectionName = "Revolut";

        public const string LiveApiBaseUrl = "https://merchant.revolut.com";

        public const string SandboxApiBaseUrl =
            "https://sandbox-merchant.revolut.com";

        /// <summary>Research default <c>Revolut-Api-Version</c>; pin per deploy.</summary>
        public const string DefaultApiVersion = "2026-04-20";

        public string SecretKey { get; set; } = string.Empty;

        public string WebhookSigningSecret { get; set; } = string.Empty;

        public string ApiBaseUrl { get; set; } = string.Empty;

        public string ApiVersion { get; set; } = string.Empty;

        /// <summary>
        /// Pricebook recurring lookup key → Revolut <c>plan_variation_id</c>.
        /// Bound from <c>Revolut__PlanVariations__{lookup_key}</c>.
        /// </summary>
        public Dictionary<string, string> PlanVariations { get; set; } =
            new(StringComparer.Ordinal);

        public bool IsLiveHost =>
            HostMatches(LiveApiBaseUrl);

        public bool IsSandboxHost =>
            HostMatches(SandboxApiBaseUrl);

        public bool HasMerchantApiConfig =>
            !string.IsNullOrWhiteSpace(SecretKey)
            && !string.IsNullOrWhiteSpace(ApiBaseUrl)
            && !string.IsNullOrWhiteSpace(ApiVersion);

        public bool TryGetPlanVariationId(
            string lookupKey,
            out string planVariationId
        )
        {
            planVariationId = string.Empty;
            if (
                string.IsNullOrWhiteSpace(lookupKey)
                || PlanVariations.Count == 0
            )
            {
                return false;
            }

            if (
                !PlanVariations.TryGetValue(lookupKey, out var mapped)
                || string.IsNullOrWhiteSpace(mapped)
            )
            {
                return false;
            }

            planVariationId = mapped.Trim();
            return true;
        }

        private bool HostMatches(string expectedBase)
        {
            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                return false;
            }

            var configured = ApiBaseUrl.Trim().TrimEnd('/');
            var expected = expectedBase.TrimEnd('/');
            return string.Equals(
                configured,
                expected,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
