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

        /// <summary>
        /// Status-only (not stored): paid checkout did not open; client retries.
        /// </summary>
        public const string PaymentFailed = "PaymentFailed";
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

        /// <summary>Pilot|Free|Starter|Growth|Group|Custom</summary>
        [MaxLength(32)]
        public string? ChosenPlan { get; set; }

        /// <summary>monthly|annual</summary>
        [MaxLength(16)]
        public string? ChosenCadence { get; set; }

        /// <summary>Custom à-la-carte config JSON from marketing custom-plan.</summary>
        public string? CustomPlanConfigJson { get; set; }

        public string? OnboardingJson { get; set; }

        [MaxLength(128)]
        public string? RevolutOrderId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Set when POST /api/Signup/session-login succeeds (one-time use).
        /// </summary>
        public DateTime? SessionLoginConsumedAtUtc { get; set; }

        /// <summary>When the signup session token was last issued.</summary>
        public DateTime? SessionTokenIssuedAtUtc { get; set; }

        /// <summary>Incremented each time a new signup code is issued.</summary>
        public int CodeGeneration { get; set; }

        /// <summary>Start of the current 24 hour abuse window.</summary>
        public DateTime? AbuseWindowStartUtc { get; set; }

        /// <summary>Codes sent inside the current abuse window.</summary>
        public int CodesSentInWindow { get; set; }

        /// <summary>Wrong code submissions inside the current abuse window.</summary>
        public int FailedVerifiesInWindow { get; set; }
    }
}
