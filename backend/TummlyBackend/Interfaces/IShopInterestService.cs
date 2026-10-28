using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    public interface IShopInterestService
    {
        Task<IReadOnlyList<ShopInterestProductDto>> ListActiveProductsAsync(
            int? userId = null,
            int? locationId = null,
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

        Task<(bool Ok, string? Error)> UnregisterAsync(
            int userId,
            string productKey,
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task<AdminShopInterestListDto> ListForAdminAsync(
            bool includeWithdrawn = false,
            CancellationToken cancellationToken = default
        );
    }
}
