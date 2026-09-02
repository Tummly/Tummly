using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    public interface IShopOrderCancelReorderService
    {
        Task<ShopOrderCancelResult> CancelAsync(
            int restaurantId,
            int userId,
            Guid orderId,
            int locationId,
            string reasonSlug,
            CancellationToken cancellationToken = default
        );

        Task<ShopOrderReorderResult> BuildReorderPrefillAsync(
            int restaurantId,
            Guid orderId,
            int locationId,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class ShopOrderCancelResult
    {
        public ShopOrderOperatorDetailDto? Order { get; init; }

        public string? ErrorCode { get; init; }

        public string? ErrorMessage { get; init; }

        public static ShopOrderCancelResult Ok(ShopOrderOperatorDetailDto order) =>
            new() { Order = order };

        public static ShopOrderCancelResult Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };
    }

    public sealed class ShopOrderReorderResult
    {
        public ShopReorderPrefillDto? Prefill { get; init; }

        public IReadOnlyList<string>? UnavailableSkuIds { get; init; }

        public string? ErrorCode { get; init; }

        public string? ErrorMessage { get; init; }

        public static ShopOrderReorderResult Ok(ShopReorderPrefillDto prefill) =>
            new() { Prefill = prefill };

        public static ShopOrderReorderResult SkuUnavailable(
            IReadOnlyList<string> skuIds
        ) =>
            new()
            {
                ErrorCode = "catalog_sku_unavailable",
                ErrorMessage = "One or more catalog skus are no longer available.",
                UnavailableSkuIds = skuIds,
            };

        public static ShopOrderReorderResult Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };
    }
}
