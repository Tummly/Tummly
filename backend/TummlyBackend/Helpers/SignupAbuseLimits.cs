namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Limits for the signup code and session-token flow (lane D phase 1).
    /// </summary>
    public static class SignupAbuseLimits
    {
        /// <summary>Length of the per-email abuse window.</summary>
        public static readonly TimeSpan Window = TimeSpan.FromHours(24);

        /// <summary>Codes that may be sent for one email inside one window.</summary>
        public const int MaxCodesPerWindow = 10;

        /// <summary>Wrong code submissions that may be made for one email inside one window.</summary>
        public const int MaxFailedVerifiesPerWindow = 15;

        /// <summary>Minimum wait between two codes for the same email.</summary>
        public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

        /// <summary>Life of a signup session token after it is issued by Verify.</summary>
        public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    }
}
