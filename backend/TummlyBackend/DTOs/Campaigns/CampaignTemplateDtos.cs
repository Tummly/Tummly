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
    /// By-id detail — list fields, suggestion defaults, and static Preview seed (S6).
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

        /// <summary>
        /// Static Campaign template Preview payload — seed counts only, not live eligibility.
        /// </summary>
        public CampaignTemplatePreviewDto Preview { get; init; } = new();
    }

    public sealed class CampaignTemplateSuggestionDefaultsDto
    {
        public string GoalId { get; init; } = string.Empty;

        public string AudienceKey { get; init; } = string.Empty;

        public string Channel { get; init; } = string.Empty;

        public string OfferStance { get; init; } = string.Empty;
    }

    /// <summary>
    /// Catalogue by-id Preview fields for Figma 5116:19403 (Campaign template Preview).
    /// </summary>
    public sealed class CampaignTemplatePreviewDto
    {
        public CampaignTemplatePreviewSummaryDto Summary { get; init; } = new();

        /// <summary>Channel tab ids for this template only (`email`, `sms`).</summary>
        public IReadOnlyList<string> SuggestedChannels { get; init; }
            = Array.Empty<string>();

        public IReadOnlyList<CampaignTemplatePreviewMessageDto> Messages { get; init; }
            = Array.Empty<CampaignTemplatePreviewMessageDto>();

        /// <summary>
        /// Null when offer is No / not-recommended — hide Offer logic and message offer block.
        /// </summary>
        public IReadOnlyList<CampaignTemplatePreviewOfferLogicRowDto>? OfferLogic
        {
            get;
            init;
        }

        public CampaignTemplatePreviewEligibilityDto Eligibility { get; init; }
            = new();

        public string SuggestedTiming { get; init; } = string.Empty;

        public string FooterDisclaimer { get; init; } = string.Empty;
    }

    public sealed class CampaignTemplatePreviewSummaryDto
    {
        public string Goal { get; init; } = string.Empty;

        public string BestFor { get; init; } = string.Empty;

        public string SuggestedAudience { get; init; } = string.Empty;

        public string SuggestedChannel { get; init; } = string.Empty;

        public string Offer { get; init; } = string.Empty;
    }

    public sealed class CampaignTemplatePreviewMessageDto
    {
        public string Channel { get; init; } = string.Empty;

        public string EstimatedUsageLabel { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public string? Subject { get; init; }

        public CampaignTemplatePreviewOfferBlockDto? OfferBlock { get; init; }
    }

    public sealed class CampaignTemplatePreviewOfferBlockDto
    {
        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string RedemptionCode { get; init; } = string.Empty;

        public string ExpiryLabel { get; init; } = string.Empty;
    }

    public sealed class CampaignTemplatePreviewOfferLogicRowDto
    {
        public string Label { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;
    }

    public sealed class CampaignTemplatePreviewEligibilityDto
    {
        public int EmailCount { get; init; }

        public int SmsCount { get; init; }

        public int TotalUniqueGuests { get; init; }
    }

    public sealed class CampaignTemplateListResponse
    {
        public IReadOnlyList<CampaignTemplateListItemDto> Items { get; init; }
            = Array.Empty<CampaignTemplateListItemDto>();
    }
}
