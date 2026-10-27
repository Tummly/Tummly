namespace TummlyBackend.Configurations
{
    /// <summary>Per-IP request limits for the signup endpoints (1-minute fixed windows).</summary>
    public sealed class SignupRateLimitSettings
    {
        public const string SectionName = "SignupRateLimit";

        /// <summary>Name of the rate limit policy applied with <c>[EnableRateLimiting]</c>.</summary>
        public const string PolicyName = "signup";

        /// <summary>Requests per client IP and endpoint path, per minute.</summary>
        public int PerEndpointPerMinute { get; set; } = 10;

        /// <summary>Requests per client IP across all signup endpoints, per minute.</summary>
        public int CombinedPerMinute { get; set; } = 30;

        /// <summary>Number of trusted proxy hops; 1 means the right-most X-Forwarded-For entry.</summary>
        public int TrustedProxyHops { get; set; } = 1;
    }
}
