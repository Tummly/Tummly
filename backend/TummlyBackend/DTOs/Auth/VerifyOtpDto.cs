namespace TummlyBackend.DTOs.Auth
{
    public class VerifyOtpDto
    {
        public string Email { get; set; } = string.Empty;

        public string OtpCode { get; set; } = string.Empty;
    }
}