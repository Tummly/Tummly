using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.DTOs.Scan
{
    public class FeedbackSubmissionDto
    {
        [MaxLength(150)]
        public string? GuestName { get; set; }

        [MaxLength(100)]
        public string? GuestContact { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public bool OffersOptOut { get; set; }

        /// <summary>Google reCAPTCHA v3 token from grecaptcha.execute.</summary>
        [MaxLength(4000)]
        public string? RecaptchaToken { get; set; }
    }
}
