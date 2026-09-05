using System.Text.Json;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared JSON options for Weekly brief <c>BodyJson</c> / <c>MetricsJson</c> /
    /// <c>EnrichmentJson</c> store and read paths (generate persist + GET deserialize).
    /// </summary>
    public static class WeeklyBriefStoreJson
    {
        public static JsonSerializerOptions Options { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
        };
    }
}
