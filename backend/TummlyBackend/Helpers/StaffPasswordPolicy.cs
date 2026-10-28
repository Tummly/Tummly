namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Password rule for Admin and Support staff logins. Shared by self-service
    /// change, staff create and admin reset so the three paths cannot drift.
    /// </summary>
    public static class StaffPasswordPolicy
    {
        public const int MinLength = 12;
        public const int MaxLength = 128;

        public const string TooShort = "password_too_short";
        public const string TooLong = "password_too_long";
        public const string DevelopmentDefault = "password_development_default";
        public const string MatchesEmail = "password_matches_email";

        /// <summary>
        /// Returns the first failed rule code, or null when the password is acceptable.
        /// The development default and email checks run before length so the
        /// reason shown is the most specific one.
        /// </summary>
        public static string? Validate(string? password, string? email)
        {
            if (string.IsNullOrEmpty(password))
            {
                return TooShort;
            }

            if (IsDevelopmentDefault(password))
            {
                return DevelopmentDefault;
            }

            var trimmedEmail = email?.Trim();
            if (!string.IsNullOrEmpty(trimmedEmail)
                && string.Equals(password, trimmedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return MatchesEmail;
            }

            if (password.Length < MinLength)
            {
                return TooShort;
            }

            if (password.Length > MaxLength)
            {
                return TooLong;
            }

            return null;
        }

        public static string Message(string code) => code switch
        {
            TooShort => $"Use at least {MinLength} characters.",
            TooLong => $"Use no more than {MaxLength} characters.",
            DevelopmentDefault => "Choose a password other than the Tummly development default.",
            MatchesEmail => "The password cannot be the same as the email address.",
            _ => "The password does not meet the staff password rule.",
        };

        private static bool IsDevelopmentDefault(string password) =>
            string.Equals(password, StaffSeedPolicy.DevelopmentAdminPassword, StringComparison.OrdinalIgnoreCase)
            || string.Equals(password, StaffSeedPolicy.DevelopmentSupportPassword, StringComparison.OrdinalIgnoreCase);
    }
}
