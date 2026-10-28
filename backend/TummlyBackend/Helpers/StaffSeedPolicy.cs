namespace TummlyBackend.Helpers
{
    public enum StaffSeedPasswordSource
    {
        None,
        Configuration,
        DevelopmentDefault,
    }

    public sealed record StaffSeedDecision(
        bool ShouldSeed,
        string Email,
        string Password,
        StaffSeedPasswordSource PasswordSource
    );

    /// <summary>
    /// Decides whether a first Admin / Support staff row may be created at startup.
    /// A password must come from configuration. Only the Development environment
    /// falls back to the hard-coded development default.
    /// </summary>
    public static class StaffSeedPolicy
    {
        public const string DevelopmentAdminEmail = "admin@tummly.com";
        public const string DevelopmentAdminPassword = "Admin@123";
        public const string DevelopmentSupportEmail = "support@tummly.com";
        public const string DevelopmentSupportPassword = "Support@123";

        public static StaffSeedDecision Decide(
            bool isDevelopment,
            string? configuredEmail,
            string? configuredPassword,
            string developmentEmail,
            string developmentPassword
        )
        {
            var email = string.IsNullOrWhiteSpace(configuredEmail)
                ? developmentEmail
                : configuredEmail.Trim();

            if (!string.IsNullOrWhiteSpace(configuredPassword))
            {
                return new StaffSeedDecision(
                    ShouldSeed: true,
                    Email: email,
                    Password: configuredPassword,
                    PasswordSource: StaffSeedPasswordSource.Configuration
                );
            }

            if (isDevelopment)
            {
                return new StaffSeedDecision(
                    ShouldSeed: true,
                    Email: email,
                    Password: developmentPassword,
                    PasswordSource: StaffSeedPasswordSource.DevelopmentDefault
                );
            }

            return new StaffSeedDecision(
                ShouldSeed: false,
                Email: email,
                Password: string.Empty,
                PasswordSource: StaffSeedPasswordSource.None
            );
        }
    }
}
