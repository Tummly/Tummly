using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    public interface IOfferRecommendationService
    {
        Task<OfferRecommendationServiceResult> RecommendAsync(
            int operatorUserId,
            int offerId,
            OfferRecommendationRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
