namespace TummlyBackend.DTOs.Signup
{
    /// <summary>
    /// Profile fields persisted in PendingSignup.OnboardingJson for
    /// ProvisionFromPendingAsync (no password).
    /// </summary>
    public class SignupOnboardingPayload
    {
        public string FullName { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;

        public string BusinessCategory { get; set; } = string.Empty;

        public string? PrimaryPhone { get; set; }

        public string? BusinessLink { get; set; }

        public List<SaveSignupOnboardingDto.LocationItem> Locations { get; set; } =
            [];
    }
}
