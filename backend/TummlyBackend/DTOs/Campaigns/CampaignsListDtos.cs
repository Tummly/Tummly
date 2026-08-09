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

        public int? CreatedByUserId { get; init; }

        public string? CreatedByDisplayName { get; init; }

        public DateTime UpdatedAt { get; init; }

        /// <summary>Null for Draft — no schedule/send yet.</summary>
        public string? SendDate { get; init; }

        /// <summary>Null for Draft — no delivery metrics.</summary>
        public string? Delivery { get; init; }

        /// <summary>Null for Draft — no engagement metrics.</summary>
        public string? Engagement { get; init; }

        /// <summary>Null for Draft — no redemptions.</summary>
        public string? Redemptions { get; init; }

        /// <summary>Base64 SQL rowversion for list lifecycle actions (ticket 30).</summary>
        public byte[] RowVersion { get; init; } = [];
    }

    public sealed class CampaignsTabCountsDto
    {
        public int All { get; init; }

        public int NeedsAttention { get; init; }

        public int Drafts { get; init; }

        public int InFlight { get; init; }

        public int Sent { get; init; }
    }

    public sealed class CampaignsCreatedByOptionDto
    {
        public int Id { get; init; }

        public string Label { get; init; } = string.Empty;
    }

    public sealed class CampaignsListFilterCatalogDto
    {
        public IReadOnlyList<CampaignsCreatedByOptionDto> CreatedBy { get; init; }
            = Array.Empty<CampaignsCreatedByOptionDto>();
    }

    public sealed class CampaignsListResponse
    {
        public IReadOnlyList<CampaignsListItemDto> Items { get; init; }
            = Array.Empty<CampaignsListItemDto>();

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public CampaignsTabCountsDto TabCounts { get; init; } = new();

        public CampaignsListFilterCatalogDto FilterCatalog { get; init; } = new();
    }

    public sealed class CampaignsListQuery
    {
        public int LocationId { get; init; }

        /// <summary>
        /// Effective locations for the item query (shell location when omitted).
        /// Tab counts always use <see cref="LocationId"/> only.
        /// </summary>
        public IReadOnlyList<int> LocationIds { get; init; } = Array.Empty<int>();

        public IReadOnlyDictionary<int, string> LocationNamesById { get; init; }
            = new Dictionary<int, string>();

        public string? View { get; init; }

        public string? Q { get; init; }

        public string Sort { get; init; } = "recent-activity";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public IReadOnlyList<string> Status { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> Channel { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> GoalId { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> OfferStance { get; init; } = Array.Empty<string>();

        public IReadOnlyList<int> CreatedBy { get; init; } = Array.Empty<int>();

        public IReadOnlyList<string> DeliveryIssue { get; init; } = Array.Empty<string>();

        public string? DateAxis { get; init; }

        public string? DatePreset { get; init; }

        public DateTime? DateFrom { get; init; }

        public DateTime? DateTo { get; init; }

        public int UtcOffsetMinutes { get; init; }
    }
}
