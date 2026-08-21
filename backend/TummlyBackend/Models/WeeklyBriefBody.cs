namespace TummlyBackend.Models
{
    /// <summary>
    /// One domain section in a Weekly brief body (capture / feedback / offers / campaigns).
    /// </summary>
    public sealed record WeeklyBriefSection(
        bool HasData,
        string Summary,
        IReadOnlyDictionary<string, int>? EchoedCounts
    );

    /// <summary>
    /// Structured Weekly brief body (schema v1). Shared by store JSON, API, and Azure
    /// Structured Outputs (<c>WeeklyBriefStructuredOutput</c>).
    /// </summary>
    public sealed record WeeklyBriefBody(
        string Headline,
        WeeklyBriefSection Capture,
        WeeklyBriefSection Feedback,
        WeeklyBriefSection Offers,
        WeeklyBriefSection Campaigns,
        IReadOnlyList<string> WatchNext
    );
}
