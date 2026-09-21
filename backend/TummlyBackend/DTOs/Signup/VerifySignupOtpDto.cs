namespace TummlyBackend.DTOs.Signup
{
    public class VerifySignupOtpDto
    {
        public string Email { get; set; } = string.Empty;

        public string OtpCode { get; set; } = string.Empty;
    }
}
