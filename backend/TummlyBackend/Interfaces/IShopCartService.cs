using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    public interface IShopCartService
    {
        Task<ShopCartDto> GetCartAsync(
            int restaurantId,
            int locationId,
            int userId,
            CancellationToken cancellationToken = default
        );

        Task<ShopCartDto?> UpsertLineAsync(
            int restaurantId,
            int locationId,
            int userId,
            string skuId,
            int quantity,
            CancellationToken cancellationToken = default
        );

        Task<ShopCartDto> RemoveLineAsync(
            int restaurantId,
            int locationId,
            int userId,
            string skuId,
            CancellationToken cancellationToken = default
        );

        Task ClearCartAsync(
            int restaurantId,
            int locationId,
            int userId,
            CancellationToken cancellationToken = default
        );
    }
}
