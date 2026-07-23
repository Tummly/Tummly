namespace TummlyBackend.DTOs.Guests
{
    public sealed class GuestFeedbacksListItemDto
    {
        public int Id { get; init; }

        public DateTime CreatedAt { get; init; }

        public string Comment { get; init; } = string.Empty;

        public string LocationName { get; init; } = string.Empty;

        public string ClassificationStatus { get; init; } = string.Empty;

        public string? Sentiment { get; init; }

        public IReadOnlyList<string>? DetectedTags { get; init; }
    }

    public sealed class GuestFeedbacksListResponse
    {
        public IReadOnlyList<GuestFeedbacksListItemDto> Items { get; init; }
            = Array.Empty<GuestFeedbacksListItemDto>();

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }
    }
}
