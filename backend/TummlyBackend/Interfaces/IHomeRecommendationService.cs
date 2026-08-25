using TummlyBackend.DTOs.OperatorHome;

namespace TummlyBackend.Interfaces
{
    public interface IHomeRecommendationService
    {
        Task<HomeRecommendationServiceResult> RecommendAsync(
            int operatorUserId,
            HomeRecommendationRequest request,
            IReadOnlySet<string>? allowedTypes = null,
            CancellationToken cancellationToken = default
        );
    }
}
