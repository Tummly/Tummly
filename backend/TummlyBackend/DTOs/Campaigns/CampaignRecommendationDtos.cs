namespace TummlyBackend.DTOs.Campaigns
{
    public sealed class CampaignRecommendationRequest
    {
        public int LocationId { get; init; }

        /// <summary>
        /// Campaigns overview preset wire value (e.g. last30, all-time).
        /// </summary>
        public string OverviewDatePreset { get; init; } = "last30";

        public DateTime? From { get; init; }

        public DateTime? To { get; init; }

        /// <summary>
        /// When true, bypass and replace the 30-minute cache entry.
        /// </summary>
        public bool Refresh { get; init; }
    }

    public sealed class CampaignRecommendationEchoedCountsDto
    {
        public int MarketingEligible { get; init; }

        public int AllGuests { get; init; }

        public int NewGuests { get; init; }

        public int NeedsRecovery { get; init; }

        public int PositiveFeedback { get; init; }

        public int DormantGuests { get; init; }
    }

    public sealed class CampaignRecommendationDraftPrefillDto
    {
        public string GoalId { get; init; } = string.Empty;

        public string AudienceKey { get; init; } = string.Empty;

        public string Channel { get; init; } = string.Empty;

        public string OfferStance { get; init; } = string.Empty;

        public string CampaignName { get; init; } = string.Empty;

        public string? MessageSubject { get; init; }

        public string MessageBody { get; init; } = string.Empty;
    }

    public sealed class CampaignRecommendationDto
    {
        public string Type { get; init; } = "none";

        public string? Title { get; init; }

        public string? Opportunity { get; init; }

        public string? EligibleAudience { get; init; }

        public IReadOnlyList<string>? WhyBullets { get; init; }

        public string? SuggestedChannel { get; init; }

        public string? EstimatedUsage { get; init; }

        public CampaignRecommendationEchoedCountsDto? EchoedCounts { get; init; }

        public CampaignRecommendationDraftPrefillDto? DraftPrefill { get; init; }

        public string? LocationName { get; init; }
    }

    public abstract record CampaignRecommendationServiceResult
    {
        private CampaignRecommendationServiceResult()
        {
        }

        public sealed record Ok(CampaignRecommendationDto Recommendation)
            : CampaignRecommendationServiceResult;

        public sealed record Failed(string Message, bool Retryable)
            : CampaignRecommendationServiceResult;
    }
}
