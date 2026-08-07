using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Pluggable Campaign recommendation provider.
    /// Production: Azure OpenAI Structured Outputs. Tests: Fake.
    /// </summary>
    public interface ICampaignRecommendationProvider
    {
        Task<CampaignRecommendationProviderResult> RecommendAsync(
            CampaignRecommendationProviderInput input,
            CancellationToken cancellationToken = default
        );
    }
}
