using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    public interface IShopOrderPlaceService
    {
        Task<ShopOrderPlaceResult> PlaceAsync(
            int restaurantId,
            int userId,
            string placedByName,
            PlaceShopOrderRequest request,
            CancellationToken cancellationToken = default
        );

        Task<ShopDeliveryDefaultsDto?> GetDeliveryDefaultsAsync(
            int restaurantId,
            int locationId,
            string placingUserDisplayName,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class ShopOrderPlaceResult
    {
        public ShopOrderDto? Order { get; init; }

        public string? ErrorCode { get; init; }

        public string? ErrorMessage { get; init; }

        public static ShopOrderPlaceResult Ok(ShopOrderDto order) =>
            new() { Order = order };

        public static ShopOrderPlaceResult Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };
    }
}
