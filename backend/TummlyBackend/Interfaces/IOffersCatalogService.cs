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
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// True when the offer exists, is effectively Active, and belongs to the location.
        /// Expired-by-fixed-date offers are not attachable.
        /// </summary>
        Task<bool> IsActiveForLocationAsync(
            int offerId,
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task<CatalogOffersListResponse> ListAsync(
            CatalogOffersListQuery query,
            CancellationToken cancellationToken = default
        );

        Task<CatalogOfferLifecycleResult> PauseAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        );

        Task<CatalogOfferLifecycleResult> ResumeAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        );

        Task<CatalogOfferLifecycleResult> ArchiveAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        );

        Task<CatalogOfferLifecycleResult> DuplicateAsync(
            int offerId,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        );
    }
}
