namespace TummlyBackend.DTOs.Signup
{
    public class SignupResumeResponse
    {
        public Guid SessionToken { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Client step hint: verify | create-password | restaurant |
        /// payment | provisioning | complete | restart.
        /// </summary>
        public string LastStepHint { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? AccountType { get; set; }

        public string? GroupName { get; set; }

        public string? BusinessCategory { get; set; }

        public string? PrimaryPhone { get; set; }

        public string? BusinessLink { get; set; }

        /// <summary>Serialized profile for resume forms; never includes password.</summary>
        public string? OnboardingJson { get; set; }

        /// <summary>Always null — password hash is never returned.</summary>
        public string? PasswordHash { get; set; }
    }
}
