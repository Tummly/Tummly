namespace TummlyBackend.Configurations
{
    /// <summary>Per-IP request limits for the signup endpoints (1-minute fixed windows).</summary>
    public sealed class SignupRateLimitSettings
    {
        public const string SectionName = "SignupRateLimit";

        /// <summary>
        /// Name of the strict rate limit policy. It covers the endpoints that send or check
        /// emailed codes: <c>start</c>, <c>verify-otp</c> and <c>resend-otp</c>.
        /// </summary>
        public const string PolicyName = "signup";

        /// <summary>
        /// Name of the token endpoint policy. It covers the endpoints that carry a signup
        /// session token and are polled by the pages (<c>onboarding</c>, <c>session</c>,
        /// <c>retry-provision</c>, <c>provisioning-status</c>, <c>session-login</c>).
        /// </summary>
        public const string TokenPolicyName = "signup-token";

        /// <summary>Requests per client IP and strict endpoint, per minute.</summary>
        public int PerEndpointPerMinute { get; set; } = 10;

        /// <summary>Requests per client IP across the strict endpoints, per minute.</summary>
        public int CombinedPerMinute { get; set; } = 30;

        /// <summary>
        /// Requests per client IP and token endpoint, per minute. These endpoints are not
        /// counted in <see cref="CombinedPerMinute"/>.
        /// </summary>
        public int TokenEndpointPerMinute { get; set; } = 120;

        /// <summary>Number of trusted proxy hops; 1 means the right-most X-Forwarded-For entry.</summary>
        public int TrustedProxyHops { get; set; } = 1;

        /// <summary>
        /// Fails with a clear message when a limit is below 1. A zero limit would reject every
        /// signup request, so the app must not start with it.
        /// </summary>
        public void Validate()
        {
            RequireAtLeastOne(PerEndpointPerMinute, nameof(PerEndpointPerMinute));
            RequireAtLeastOne(CombinedPerMinute, nameof(CombinedPerMinute));
            RequireAtLeastOne(TokenEndpointPerMinute, nameof(TokenEndpointPerMinute));
            RequireAtLeastOne(TrustedProxyHops, nameof(TrustedProxyHops));
        }

        private static void RequireAtLeastOne(int value, string name)
        {
            if (value < 1)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:{name} must be 1 or more."
                );
            }
        }
    }
}
