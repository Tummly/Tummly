namespace TummlyBackend.DTOs.Guests
{
    public sealed class GuestActivityListItemDto
    {
        public int Id { get; init; }

        public string Kind { get; init; }
            = string.Empty;

        public DateTime OccurredAt { get; init; }

        public int? FeedbackId { get; init; }

        public string LocationName { get; init; }
            = string.Empty;

        public string? TagName { get; init; }

        public int? GuestTagId { get; init; }

        public string? AuthorDisplayName { get; init; }

        public string? Sentiment { get; init; }

        public IReadOnlyList<string>? ChangedFields { get; init; }
    }

    public sealed class GuestActivityListResponse
    {
        public IReadOnlyList<GuestActivityListItemDto> Items { get; init; }
            = Array.Empty<GuestActivityListItemDto>();

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }
    }
}
