namespace TummlyBackend.Models
{
    public sealed record AssistantFeedbackEvidence(
        int TotalCount,
        int SampleCount,
        int SucceededPositive,
        int SucceededNeutral,
        int SucceededNegative,
        int NeedsAttention,
        IReadOnlyList<AssistantFeedbackTagCount> TagCounts,
        IReadOnlyList<AssistantFeedbackEvidenceRow> Rows,
        IReadOnlyList<AssistantGuestEvidenceRow> GuestRows,
        IReadOnlyList<AssistantGuestEvidenceRow> Placeholder4GuestRows,
        IReadOnlyList<string> ContactRedactionTokens
    )
    {
        public static AssistantFeedbackEvidence Empty { get; } =
            new(0, 0, 0, 0, 0, 0, [], [], [], [], []);

        public bool IsEmpty => TotalCount == 0;

        public bool DisclosesSample => TotalCount > SampleCount;
    }

    public sealed record AssistantFeedbackTagCount(string Tag, int Count);

    public sealed record AssistantFeedbackEvidenceRow(
        int Id,
        DateTime CreatedAt,
        string GuestName,
        string? Sentiment,
        string ClassificationStatus,
        IReadOnlyList<string> DetectedTags,
        string WorkflowStatus,
        bool NeedsAttention,
        string? QrSource,
        string ContactType,
        string Excerpt,
        string FeedbackReference,
        string? MarketingStatus,
        IReadOnlyList<string> GuestTags,
        int? LocationGuestId,
        bool IsLinked
    );

    public sealed record AssistantGuestEvidenceRow(
        int LocationGuestId,
        string Name,
        string MarketingStatus,
        IReadOnlyList<string> GuestTags,
        bool IsMarketingEligible
    );
}
