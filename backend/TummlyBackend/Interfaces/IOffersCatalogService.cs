using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    public interface IOffersCatalogService
    {
        Task<CatalogOfferDto> CreateActiveAsync(
            CreateCatalogOfferRequest request,
            int? createdByUserId = null,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Persist a stored Offers catalog Draft (not Active). Not attachable until Active.
        /// Separate from <see cref="CreateActiveAsync"/> — do not reuse the live Active create path.
        /// </summary>
        Task<CatalogOfferDto> CreateDraftAsync(
            CreateCatalogOfferRequest request,
            int? createdByUserId = null,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Update editable catalog fields for Draft / Active / Paused offers.
        /// Offer type is immutable. Does not rewrite existing OfferIssue rows.
        /// </summary>
        Task<CatalogOfferLifecycleResult> UpdateAsync(
            int offerId,
            CreateCatalogOfferRequest request,
            int utcOffsetMinutes = 0,
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
            int? createdByUserId = null,
            int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
        );
    }
}
