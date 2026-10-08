namespace TummlyBackend.Models
{
    /// <summary>
    /// Phase-2 Reports enrichment from Weekly brief Structured Outputs.
    /// Stored separately from Home <see cref="WeeklyBriefBody"/> schema v1.
    /// </summary>
    public sealed record WeeklyBriefEnrichment(
        string? ExecutiveSummary,
        WeeklyBriefEnrichmentFeedbackSummary? FeedbackSummary,
        IReadOnlyList<WeeklyBriefEnrichmentActionWording> ActionWording,
        IReadOnlyList<WeeklyBriefInsightNarrative>? InsightNarratives = null,
        IReadOnlyList<WeeklyBriefInsightCandidate>? InsightCandidates = null
    );

    public sealed record WeeklyBriefEnrichmentFeedbackSummary(
        string Text,
        string Subtitle
    );

    public sealed record WeeklyBriefEnrichmentActionWording(
        string Kind,
        string Title,
        string Subtitle
    );

    public static class WeeklyBriefEnrichmentActionKinds
    {
        public const string FeedbackNeedsAttention = "feedback-needs-attention";
        public const string RepeatedInvalid = "repeated-invalid";
        public const string LowRedemption = "low-redemption";
        public const string UnderperformQr = "underperform-qr";

        public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(
            StringComparer.Ordinal
        )
        {
            FeedbackNeedsAttention,
            RepeatedInvalid,
            LowRedemption,
            UnderperformQr,
        };

        public static bool IsAllowed(string? kind)
            => kind is not null && Allowed.Contains(kind);
    }
}
