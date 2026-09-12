namespace TummlyBackend.DTOs.Search
{
    public sealed class GlobalSearchQuery
    {
        public required string Q { get; init; }

        public required int LocationId { get; init; }

        public required string LocationName { get; init; }

        public required int Limit { get; init; }

        public required bool IncludeGuests { get; init; }

        public required bool IncludeFeedback { get; init; }

        public required bool IncludeCampaigns { get; init; }

        public required bool IncludeOffers { get; init; }

        public required bool IncludeQrCodes { get; init; }

        /// <summary>Venue offset for Offer effective status (Expired vs Active).</summary>
        public int UtcOffsetMinutes { get; init; }
    }

    public sealed class GlobalSearchResponse
    {
        public bool Success { get; init; } = true;

        public required string Q { get; init; }

        public required int LocationId { get; init; }

        public required IReadOnlyList<GlobalSearchGroupDto> Groups { get; init; }
    }

    public sealed class GlobalSearchGroupDto
    {
        public required string Type { get; init; }

        public required IReadOnlyList<GlobalSearchHitDto> Hits { get; init; }
    }

    public sealed class GlobalSearchHitDto
    {
        public required string Id { get; init; }

        public required string EntityType { get; init; }

        public required string Title { get; init; }

        public string? Subtitle { get; init; }

        public required int LocationId { get; init; }

        public required string LocationName { get; init; }

        public string? Status { get; init; }
    }
}
