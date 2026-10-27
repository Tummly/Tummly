namespace TummlyBackend.DTOs.Signup
{
    /// <summary>
    /// Returned by Start and Resend. It never carries a session token.
    /// </summary>
    public sealed class SignupCodeSentResponse
    {
        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = "code_sent";
    }
}
