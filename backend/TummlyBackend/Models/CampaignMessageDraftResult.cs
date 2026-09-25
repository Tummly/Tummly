namespace TummlyBackend.Models
{
    /// <summary>
    /// Confirmed catalog Offer facts for Campaign message-draft grounding.
    /// Must never include live redemption codes (issued only at send).
    /// </summary>
    public sealed record CampaignMessageDraftConfirmedOffer(
        string OfferType,
        string Title,
        string Description,
        string Validity,
        string? ExpiryDate,
        decimal? DiscountPercentage,
        decimal? DiscountAmount,
        string? FreeItemText,
        string? PurchaseRequirement,
        decimal? MinimumSpend,
        string? AdditionalExclusions,
        string? ReplacementItemText
    );

    /// <summary>
    /// Inputs for Campaign message-draft AI.
    /// Must never include guest email/phone or other guest PII.
    /// </summary>
    public sealed record CampaignMessageDraftInput(
        string LocationName,
        string Channel,
        string GoalId,
        string AudienceKey,
        string OfferStance,
        string? CampaignName,
        string Tone,
        string? IncludeNotes,
        string Mode,
        string? CurrentBody,
        string? CurrentSubject,
        CampaignMessageDraftConfirmedOffer? ConfirmedOffer = null
    );

    public abstract record CampaignMessageDraftProviderResult
    {
        private CampaignMessageDraftProviderResult()
        {
        }

        public sealed record Succeeded(
            string Body,
            string? Subject,
            string Channel
        ) : CampaignMessageDraftProviderResult;

        public sealed record Failed(bool Retryable = true)
            : CampaignMessageDraftProviderResult;
    }
}
