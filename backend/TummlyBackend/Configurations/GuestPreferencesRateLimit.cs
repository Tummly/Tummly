namespace TummlyBackend.Configurations
{
    /// <summary>
    /// Rate limit for the anonymous guest preference centre endpoints. Partitioned by
    /// client IP, in a one-hour fixed window. Wired in Program.cs next to the signup policies.
    /// </summary>
    public static class GuestPreferencesRateLimit
    {
        public const string PolicyName = "guest-preferences";

        public const int PermitLimitPerIpPerHour = 20;

        public static readonly TimeSpan Window = TimeSpan.FromHours(1);
    }
}
