namespace TummlyBackend.DTOs.Campaigns
{
    /// <summary>
    /// Slice-1 list projection placeholder — items stay empty until Draft stub (ticket 30).
    /// </summary>
    public sealed class CampaignsListItemDto
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Status { get; init; } = "draft";
    }

    public sealed class CampaignsTabCountsDto
    {
        public int All { get; init; }

        public int NeedsAttention { get; init; }

        public int Drafts { get; init; }

        public int InFlight { get; init; }

        public int Sent { get; init; }
    }

    public sealed class CampaignsListResponse
    {
        public IReadOnlyList<CampaignsListItemDto> Items { get; init; }
            = Array.Empty<CampaignsListItemDto>();

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public CampaignsTabCountsDto TabCounts { get; init; } = new();
    }

    public sealed class CampaignsListQuery
    {
        public int LocationId { get; init; }

        public string? View { get; init; }

        public string? Q { get; init; }

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;
    }
}
