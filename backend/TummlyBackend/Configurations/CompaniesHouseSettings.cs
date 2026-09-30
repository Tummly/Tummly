namespace TummlyBackend.Configurations
{
    public class CompaniesHouseSettings
    {
        public string? ApiKey { get; set; }

        public string BaseUrl { get; set; } =
            "https://api.company-information.service.gov.uk/";

        public int SuggestRateLimitPerWindow { get; set; } = 60;

        public int RateLimitWindowMinutes { get; set; } = 5;

        public int SuggestCacheMinutes { get; set; } = 60;

        public int SuggestLimit { get; set; } = 10;
    }
}
