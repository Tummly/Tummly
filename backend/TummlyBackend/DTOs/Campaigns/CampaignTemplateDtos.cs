namespace TummlyBackend.DTOs.Campaigns
{
    /// <summary>
    /// List projection for the product-global campaign-template catalogue.
    /// </summary>
    public sealed class CampaignTemplateListItemDto
    {
        public string Id { get; init; } = string.Empty;

        public int Version { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string GoalLabel { get; init; } = string.Empty;

        public string AudienceLabel { get; init; } = string.Empty;

        public string ChannelLabel { get; init; } = string.Empty;

        public string OfferLabel { get; init; } = string.Empty;

        public bool SuggestsGoal { get; init; }

        public bool SuggestsAudience { get; init; }

        public bool SuggestsChannel { get; init; }

        public bool SuggestsOffer { get; init; }
    }

    /// <summary>
    /// By-id detail — list fields plus structured suggestion defaults for wizard open.
    /// </summary>
    public sealed class CampaignTemplateDetailDto
    {
        public string Id { get; init; } = string.Empty;

        public int Version { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string GoalLabel { get; init; } = string.Empty;

        public string AudienceLabel { get; init; } = string.Empty;

        public string ChannelLabel { get; init; } = string.Empty;

        public string OfferLabel { get; init; } = string.Empty;

        public bool SuggestsGoal { get; init; }

        public bool SuggestsAudience { get; init; }

        public bool SuggestsChannel { get; init; }

        public bool SuggestsOffer { get; init; }

        public CampaignTemplateSuggestionDefaultsDto Suggestions { get; init; }
            = new();
    }

    public sealed class CampaignTemplateSuggestionDefaultsDto
    {
        public string GoalId { get; init; } = string.Empty;

        public string AudienceKey { get; init; } = string.Empty;

        public string Channel { get; init; } = string.Empty;

        public string OfferStance { get; init; } = string.Empty;
    }

    public sealed class CampaignTemplateListResponse
    {
        public IReadOnlyList<CampaignTemplateListItemDto> Items { get; init; }
            = Array.Empty<CampaignTemplateListItemDto>();
    }
}
