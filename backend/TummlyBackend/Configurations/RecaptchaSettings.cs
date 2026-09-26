namespace TummlyBackend.Configurations
{
    public class RecaptchaSettings
    {
        public const string SectionName = "Recaptcha";

        /// <summary>Google reCAPTCHA v3 secret. Empty → verification skipped.</summary>
        public string? SecretKey { get; set; }

        /// <summary>Minimum score (0–1) for a passing v3 token.</summary>
        public double MinScore { get; set; } = 0.5;

        public string VerifyUrl { get; set; } =
            "https://www.google.com/recaptcha/api/siteverify";

        /// <summary>Expected grecaptcha.execute action for guest feedback.</summary>
        public string ExpectedAction { get; set; } = "guest_feedback";
    }
}
