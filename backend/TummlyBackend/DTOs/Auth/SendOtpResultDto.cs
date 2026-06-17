using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.Auth
{
    public class SendOtpResultDto
    {
        public bool Skipped { get; set; }

        // Yahan se 'OtpVerification.ChannelEmail' hata kar default value set karein
        public string OtpChannel { get; set; } = "Email";

        public string Message { get; set; } = string.Empty;

        public string? MaskedPhone { get; set; }
    }
}