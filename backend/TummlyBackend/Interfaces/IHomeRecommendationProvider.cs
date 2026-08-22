using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Pluggable Home-native recommendation copy provider.
    /// Production: Azure OpenAI Structured Outputs. Tests/CI: Fake.
    /// Campaign allow-list types are completed by the orchestrator handoff (ticket 06),
    /// not by this provider.
    /// </summary>
    public interface IHomeRecommendationProvider
    {
        Task<HomeRecommendationProviderResult> RecommendAsync(
            HomeRecommendationProviderInput input,
            CancellationToken cancellationToken = default
        );
    }
}
