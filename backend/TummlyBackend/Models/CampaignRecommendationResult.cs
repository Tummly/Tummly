namespace TummlyBackend.Models
{
    /// <summary>
    /// Live metrics fed into Campaign recommendation AI (no guest PII).
    /// </summary>
    public sealed record CampaignRecommendationMetrics(
        int MarketingEligible,
        int AllGuests,
        int NewGuests,
        int NeedsRecovery,
        int PositiveFeedback,
        int DormantGuests
    );

    public sealed record CampaignRecommendationProviderInput(
        string LocationName,
        string OverviewDatePreset,
        DateTime? FromUtc,
        DateTime? ToUtc,
        CampaignRecommendationMetrics Metrics
    );

    public sealed record CampaignRecommendationDraftPrefillOutput(
        string GoalId,
        string AudienceKey,
        string Channel,
        string OfferStance,
        string CampaignName,
        string? MessageSubject,
        string MessageBody
    );

    public sealed record CampaignRecommendationModelOutput(
        string Type,
        string? Title,
        string? Opportunity,
        string? EligibleAudience,
        IReadOnlyList<string>? WhyBullets,
        string? SuggestedChannel,
        string? EstimatedUsage,
        CampaignRecommendationDraftPrefillOutput? DraftPrefill
    );

    /// <summary>
    /// Result of Campaign recommendation provider (Azure or Fake).
    /// </summary>
    public abstract record CampaignRecommendationProviderResult
    {
        private CampaignRecommendationProviderResult()
        {
        }

        public sealed record Succeeded(
            CampaignRecommendationModelOutput Output
        ) : CampaignRecommendationProviderResult;

        public sealed record Failed(bool Retryable = true)
            : CampaignRecommendationProviderResult;
    }
}
