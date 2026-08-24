namespace TummlyBackend.DTOs.Offers
{
    /// <summary>
    /// POST /api/offers/{offerId}/recommendation (ticket 02).
    /// Window is Default reporting period — not Overview KPI range.
    /// </summary>
    public sealed class OfferRecommendationRequest
    {
        public int LocationId { get; init; }

        /// <summary>
        /// When true, bypass and replace the 30-minute cache entry.
        /// </summary>
        public bool Refresh { get; init; }
    }

    public sealed class OfferRecommendationDraftPrefillDto
    {
        public int OfferId { get; init; }

        public string OfferStance { get; init; } = "existing-offer";

        public string GoalId { get; init; } = "promote-something-new";

        public string AudienceKey { get; init; } = "all-eligible-guests";

        public string Channel { get; init; } = "email";

        public string CampaignName { get; init; } = string.Empty;

        public string? MessageSubject { get; init; }

        public string? MessageBody { get; init; }
    }

    public sealed class OfferRecommendationDto
    {
        public string Type { get; init; } = "none";

        public string? Title { get; init; }

        public string? Opportunity { get; init; }

        public IReadOnlyList<string>? WhyBullets { get; init; }

        public string? SuggestedChannel { get; init; }

        public OfferRecommendationDraftPrefillDto? DraftPrefill { get; init; }

        public string? LocationName { get; init; }
    }

    public abstract record OfferRecommendationServiceResult
    {
        private OfferRecommendationServiceResult()
        {
        }

        public sealed record Ok(OfferRecommendationDto Recommendation)
            : OfferRecommendationServiceResult;

        public sealed record Failed(string Message, bool Retryable)
            : OfferRecommendationServiceResult;

        public sealed record NotFound() : OfferRecommendationServiceResult;

        public sealed record WrongLocation() : OfferRecommendationServiceResult;
    }
}
