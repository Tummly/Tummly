namespace TummlyBackend.DTOs.Signup
{
    /// <summary>
    /// Guest Loop onboarding payload for a verified pending signup.
    /// Shape matches CompleteSetupDto fields used by ProvisionFromPendingAsync
    /// (password is hashed on save for email signup; not stored in OnboardingJson).
    /// Password / ConfirmPassword are optional for social (AuthProvider) pending.
    /// </summary>
    public class SaveSignupOnboardingDto
    {
        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;

        public string BusinessCategory { get; set; } = string.Empty;

        public string? PrimaryPhone { get; set; }

        public string? BusinessLink { get; set; }

        public List<LocationItem> Locations { get; set; } = [];

        public class LocationItem
        {
            public string LocationName { get; set; } = "";

            public string Address { get; set; } = "";

            public string? City { get; set; }

            public string? Postcode { get; set; }

            public string? LocationPhone { get; set; }

            public string? LocalContact { get; set; }

            public bool AddressOverridden { get; set; }
        }
    }
}
