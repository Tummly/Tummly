namespace TummlyBackend.Models
{
    /// <summary>
    /// Input for Offer recommendation copy (Azure or Fake).
    /// SelectedType is already chosen by rules — provider fills copy only.
    /// No guest PII.
    /// </summary>
    public sealed record OfferRecommendationProviderInput(
        string SelectedType,
        string LocationName,
        string ReportingPeriod,
        DateTime FromUtc,
        DateTime ToUtc,
        int OfferId,
        string OfferTitle,
        int MarketingEligible,
        int ClaimsInPeriod,
        bool NeedsAttention
    );

    public sealed record OfferRecommendationModelOutput(
        string Type,
        string? Title,
        string? Opportunity,
        IReadOnlyList<string>? WhyBullets,
        string? SuggestedChannel,
        string? CampaignName,
        string? MessageSubject,
        string? MessageBody
    );

    public abstract record OfferRecommendationProviderResult
    {
        private OfferRecommendationProviderResult()
        {
        }

        public sealed record Succeeded(OfferRecommendationModelOutput Output)
            : OfferRecommendationProviderResult;

        public sealed record Failed(bool Retryable = true)
            : OfferRecommendationProviderResult;
    }
}
