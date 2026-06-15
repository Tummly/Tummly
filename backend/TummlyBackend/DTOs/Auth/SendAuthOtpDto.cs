namespace TummlyBackend.DTOs.Auth
{
    public class SendAuthOtpDto
    {
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// resend — A2 resend on active channel (always issues new OTP).
        /// switch-to-email — A3 email option (skip when active OTP still valid).
        /// </summary>
        public string Purpose { get; set; } = "resend";
    }
}
