using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Pluggable Offer recommendation copy provider.
    /// Production: Azure OpenAI Structured Outputs. Tests/CI: Fake.
    /// Rules pick the type; this provider writes copy only.
    /// </summary>
    public interface IOfferRecommendationProvider
    {
        Task<OfferRecommendationProviderResult> RecommendAsync(
            OfferRecommendationProviderInput input,
            CancellationToken cancellationToken = default
        );
    }
}
