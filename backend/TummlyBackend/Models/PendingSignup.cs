using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class PendingSignupStatuses
    {
        public const string EmailPending = "EmailPending";
        public const string Verified = "Verified";
        public const string OnboardingComplete = "OnboardingComplete";
        public const string AwaitingPayment = "AwaitingPayment";
        public const string Provisioning = "Provisioning";
        public const string Complete = "Complete";
        public const string Abandoned = "Abandoned";
    }

    public class PendingSignup
    {
        public Guid Id { get; set; }

        public Guid SessionToken { get; set; }

        [MaxLength(200)]
        public string Email { get; set; } = "";

        [MaxLength(32)]
        public string Status { get; set; } = PendingSignupStatuses.EmailPending;

        public int OtpResendCount { get; set; }

        public DateTime? LastOtpSentAt { get; set; }

        public bool TermsAccepted { get; set; }

        public DateTime? EmailVerifiedAt { get; set; }

        [MaxLength(200)]
        public string? PasswordHash { get; set; }

        [MaxLength(32)]
        public string? AuthProvider { get; set; }

        [MaxLength(200)]
        public string? ProviderSubject { get; set; }

        [MaxLength(150)]
        public string? FullName { get; set; }

        /// <summary>Single | Multi</summary>
        [MaxLength(16)]
        public string? AccountType { get; set; }

        /// <summary>Pilot|Free|Starter|Growth|Group</summary>
        [MaxLength(32)]
        public string? ChosenPlan { get; set; }

        /// <summary>monthly|annual</summary>
        [MaxLength(16)]
        public string? ChosenCadence { get; set; }

        public string? OnboardingJson { get; set; }

        [MaxLength(128)]
        public string? RevolutOrderId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
