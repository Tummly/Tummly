using TummlyBackend.DTOs.Shop;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Physical Offer Card print catalog attach per Owned location.
    /// </summary>
    public interface IShopOfferCardOfferService
    {
        Task<ShopOfferCardOfferDto> GetAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task<ShopOfferCardOfferSetResult> SetAsync(
            int locationId,
            int? offerId,
            CancellationToken cancellationToken = default
        );

        Task<bool> HasAttachAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );
    }
}
