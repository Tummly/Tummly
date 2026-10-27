namespace TummlyBackend.Models
{
    public class OtpVerification
    {
        public const string ChannelEmail = "email";
        public const string ChannelSms = "sms";

        /// <summary>
        /// Placeholder stored when Twilio Verify owns the OTP lifecycle.
        /// </summary>
        public const string TwilioManagedCode = "TWILIO";

        public int Id { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        public string Email { get; set; } = string.Empty;

        public string OtpCode { get; set; } = string.Empty;

        public string Channel { get; set; } = ChannelEmail;

        public bool IsUsed { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>See <see cref="TummlyBackend.Helpers.OtpPurposes"/>.</summary>
        public string? Purpose { get; set; }

        /// <summary>Id of the subject the code was issued for (purpose-specific).</summary>
        public string? SubjectId { get; set; }

        /// <summary>Incremented each time a new code is issued for the same subject.</summary>
        public int Generation { get; set; }

        /// <summary>Lower-hex HMAC-SHA256 of the code. Never the plain code.</summary>
        public string? CodeHash { get; set; }

        /// <summary>Wrong code submissions against this row.</summary>
        public int FailedAttempts { get; set; }
    }
}
