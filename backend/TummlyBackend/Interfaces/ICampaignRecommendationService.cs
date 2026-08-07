using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    public interface ICampaignRecommendationService
    {
        Task<CampaignRecommendationServiceResult> RecommendAsync(
            int operatorUserId,
            CampaignRecommendationRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
