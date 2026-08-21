namespace TummlyBackend.DTOs.OperatorHome
{
    /// <summary>
    /// POST /api/home/recommendation request (ticket 01 contract).
    /// Home performance window as preset + resolved from/to; optional refresh.
    /// </summary>
    public sealed class HomeRecommendationRequest
    {
        public int LocationId { get; init; }

        /// <summary>
        /// Home performance preset wire value (e.g. last7, last30, thisMonth, custom).
        /// </summary>
        public string OverviewDatePreset { get; init; } = "last7";

        public DateTime? From { get; init; }

        public DateTime? To { get; init; }

        /// <summary>
        /// When true, bypass and replace the 30-minute cache entry.
        /// </summary>
        public bool Refresh { get; init; }
    }

    /// <summary>
    /// Domain primary CTA for Home-native recommendation types.
    /// Null entity id means the domain list / create destination.
    /// </summary>
    public sealed class HomeRecommendationActionDto
    {
        /// <summary>
        /// open-feedback | open-guest | open-offer
        /// </summary>
        public string Kind { get; init; } = string.Empty;

        public int? FeedbackId { get; init; }

        public int? LocationGuestId { get; init; }

        public int? OfferId { get; init; }
    }

    /// <summary>
    /// Home Recommended next step payload.
    /// Campaign types may include DraftPrefill + audience fields matching Campaigns.
    /// </summary>
    public sealed class HomeRecommendationDto
    {
        public string Type { get; init; } = "none";

        public string? Title { get; init; }

        public string? Opportunity { get; init; }

        public IReadOnlyList<string>? WhyBullets { get; init; }

        public HomeRecommendationActionDto? Action { get; init; }

        /// <summary>
        /// Server metrics only. Campaign types reuse CampaignRecommendationEchoedCountsDto shape.
        /// </summary>
        public Campaigns.CampaignRecommendationEchoedCountsDto? EchoedCounts { get; init; }

        public string? EligibleAudience { get; init; }

        public string? SuggestedChannel { get; init; }

        public string? EstimatedUsage { get; init; }

        public Campaigns.CampaignRecommendationDraftPrefillDto? DraftPrefill { get; init; }

        public string? LocationName { get; init; }
    }

    public abstract record HomeRecommendationServiceResult
    {
        private HomeRecommendationServiceResult()
        {
        }

        public sealed record Ok(HomeRecommendationDto Recommendation)
            : HomeRecommendationServiceResult;

        public sealed record Failed(string Message, bool Retryable)
            : HomeRecommendationServiceResult;
    }
}
