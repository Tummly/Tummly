using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    public interface IOffersCatalogService
    {
        Task<CatalogOfferDto> CreateActiveAsync(
            CreateCatalogOfferRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CatalogOfferDto?> GetByIdAsync(
            int offerId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// True when the offer exists, is Active, and belongs to the location.
        /// </summary>
        Task<bool> IsActiveForLocationAsync(
            int offerId,
            int locationId,
            CancellationToken cancellationToken = default
        );
    }
}
