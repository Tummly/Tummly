namespace TummlyBackend.DTOs.Campaigns
{
    /// <summary>
    /// Figma Campaign table row projection — Draft rows use null performance columns (ticket 30).
    /// </summary>
    public sealed class CampaignsListItemDto
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Status { get; init; } = "draft";

        public string? GoalId { get; init; }

        public int LocationId { get; init; }

        public string LocationName { get; init; } = string.Empty;

        public string? Channel { get; init; }

        public string? AudienceKey { get; init; }

        public string? OfferStance { get; init; }

        public DateTime UpdatedAt { get; init; }

        /// <summary>Null for Draft — no schedule/send yet.</summary>
        public string? SendDate { get; init; }

        /// <summary>Null for Draft — no delivery metrics.</summary>
        public string? Delivery { get; init; }

        /// <summary>Null for Draft — no engagement metrics.</summary>
        public string? Engagement { get; init; }

        /// <summary>Null for Draft — no redemptions.</summary>
        public string? Redemptions { get; init; }
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
