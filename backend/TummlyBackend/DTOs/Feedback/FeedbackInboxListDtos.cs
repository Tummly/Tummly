namespace TummlyBackend.DTOs.Feedback
{
    public sealed class FeedbackInboxListItemDto
    {
        public int Id { get; init; }

        public DateTime CreatedAt { get; init; }

        public string Comment { get; init; } = string.Empty;

        public string GuestName { get; init; } = string.Empty;

        public string ContactType { get; init; } = string.Empty;

        public string LocationName { get; init; } = string.Empty;

        public string? QrSource { get; init; }

        public string ClassificationStatus { get; init; } = string.Empty;

        public string? Sentiment { get; init; }

        public IReadOnlyList<string>? DetectedTags { get; init; }

        public string WorkflowStatus { get; init; } = string.Empty;

        public bool NeedsAttention { get; init; }

        public int? LocationGuestId { get; init; }
    }

    public sealed class FeedbackInboxTabCountsDto
    {
        public int All { get; init; }

        public int NeedsAttention { get; init; }

        public int New { get; init; }

        public int InProgress { get; init; }

        public int Resolved { get; init; }
    }

    public sealed class FeedbackInboxDigitalGuestLinkDto
    {
        public int Id { get; init; }

        public string LinkName { get; init; } = string.Empty;
    }

    public sealed class FeedbackInboxListResponse
    {
        public IReadOnlyList<FeedbackInboxListItemDto> Items { get; init; }
            = Array.Empty<FeedbackInboxListItemDto>();

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public FeedbackInboxTabCountsDto TabCounts { get; init; } = new();

        public IReadOnlyList<FeedbackInboxDigitalGuestLinkDto> DigitalGuestLinks
        {
            get;
            init;
        } = Array.Empty<FeedbackInboxDigitalGuestLinkDto>();
    }

    public sealed class FeedbackInboxListQuery
    {
        public int LocationId { get; init; }

        public string LocationName { get; init; } = string.Empty;

        public DateTime FromUtc { get; init; }

        public DateTime ToUtc { get; init; }

        public string Tab { get; init; } = "all";

        public string? Q { get; init; }

        public string[]? Sentiment { get; init; }

        public string[]? DetectedTags { get; init; }

        public string[]? QrSource { get; init; }

        public string[]? Contact { get; init; }

        public string[]? WorkflowStatus { get; init; }

        public string? DatePreset { get; init; }

        public DateTime? DateFrom { get; init; }

        public DateTime? DateTo { get; init; }

        public string Sort { get; init; } = "newest-submitted";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public int UtcOffsetMinutes { get; init; }
    }
}
