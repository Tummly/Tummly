namespace TummlyBackend.DTOs.Auth
{
    public class SendOtpResultDto
    {
        public bool Skipped { get; set; }

        public string OtpChannel { get; set; } = Models.OtpVerification.ChannelEmail;

        public string Message { get; set; } = string.Empty;

        public string? MaskedPhone { get; set; }
    }
}
