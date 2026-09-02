using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    public interface IShopLocationRecommendationsService
    {
        Task<ShopLocationDetailsBasisDto?> SaveDetailsAsync(
            int locationId,
            SaveShopLocationDetailsRequest request,
            CancellationToken cancellationToken = default
        );

        Task<ShopLocationRecommendationsDto> GetRecommendationsAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );
    }
}
