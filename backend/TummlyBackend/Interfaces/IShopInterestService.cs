using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    public interface IShopInterestService
    {
        Task<IReadOnlyList<ShopInterestProductDto>> ListActiveProductsAsync(
            CancellationToken cancellationToken = default
        );

        Task<(bool Ok, string? Error, string? Message)> RegisterAsync(
            int userId,
            int restaurantId,
            string restaurantName,
            RegisterShopInterestRequest request,
            string? locationName,
            string userFullName,
            string userEmail,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopInterestListDto> ListForAdminAsync(
            CancellationToken cancellationToken = default
        );
    }
}
