namespace TummlyBackend.Configurations
{
    public class IdealPostcodesSettings
    {
        public string? ApiKey { get; set; }

        public string BaseUrl { get; set; } = "https://api.ideal-postcodes.co.uk/v1/";

        public int SuggestRateLimitPerWindow { get; set; } = 60;

        public int ResolveRateLimitPerWindow { get; set; } = 30;

        public int RateLimitWindowMinutes { get; set; } = 5;

        public int SuggestCacheMinutes { get; set; } = 60;

        public int ResolveCacheHours { get; set; } = 24;

        public int AutocompleteLimit { get; set; } = 5;
    }
}
