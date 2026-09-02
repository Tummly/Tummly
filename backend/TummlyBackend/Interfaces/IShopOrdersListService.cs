using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    public interface IShopOrdersListService
    {
        Task<ShopOrderListResponseDto> GetListAsync(
            ShopOrdersListQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
