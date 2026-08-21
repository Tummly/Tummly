namespace TummlyBackend.Models
{
    /// <summary>
    /// Input for Home-native recommendation copy (Azure or Fake).
    /// SelectedType is already chosen by the domain router — provider fills copy only.
    /// No guest PII or feedback body text.
    /// </summary>
    public sealed record HomeRecommendationProviderInput(
        string SelectedType,
        string LocationName,
        string OverviewDatePreset,
        DateTime FromUtc,
        DateTime ToUtc,
        HomeRecommendationMetrics Metrics
    );

    public sealed record HomeRecommendationDomainActionOutput(
        string Kind,
        int? FeedbackId,
        int? LocationGuestId,
        int? OfferId
    );

    public sealed record HomeRecommendationModelOutput(
        string Type,
        string? Title,
        string? Opportunity,
        IReadOnlyList<string>? WhyBullets,
        HomeRecommendationDomainActionOutput? Action
    );

    /// <summary>
    /// Result of Home recommendation provider (Azure or Fake).
    /// </summary>
    public abstract record HomeRecommendationProviderResult
    {
        private HomeRecommendationProviderResult()
        {
        }

        public sealed record Succeeded(HomeRecommendationModelOutput Output)
            : HomeRecommendationProviderResult;

        public sealed record Failed(bool Retryable = true)
            : HomeRecommendationProviderResult;
    }
}
