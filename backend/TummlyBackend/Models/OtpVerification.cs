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
    }
}
