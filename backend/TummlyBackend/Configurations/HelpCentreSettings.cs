namespace TummlyBackend.Configurations
{
    public class HelpCentreSettings
    {
        public string AdminNotificationEmail { get; set; } = "admin@tummly.com";

        public string SupportNotificationEmail { get; set; } = "support@tummly.com";

        public int ContactFormRateLimitPerWindow { get; set; } = 5;

        public int RateLimitWindowMinutes { get; set; } = 60;
    }
}
