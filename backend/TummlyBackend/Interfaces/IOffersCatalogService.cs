using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    public interface IOffersCatalogService
    {
        /// <summary>
        /// Persist a finished Offer create. Same as <see cref="CreateDraftAsync"/> —
        /// stored Draft until the first live attach. Name kept for
        /// <c>POST /offers</c> wire compatibility.
        /// </summary>
        Task<CatalogOfferDto> CreateActiveAsync(
            CreateCatalogOfferRequest request,
            int? createdByUserId = null,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Persist a stored Offers catalog Draft. Same stored outcome as
        /// <see cref="CreateActiveAsync"/> until a live attach promotes status.
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
        /// True when the offer exists at the location and may receive a live attach
        /// (stored Draft or Active, not past fixed expiry / paused / archived).
        /// </summary>
        Task<bool> IsAttachableForLocationAsync(
            int offerId,
            int locationId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Promote Draft → Active when ≥1 live attach exists; demote Active → Draft
        /// when the last attach is cleared. No-op for paused / archived /
        /// expired-effective rows.
        /// </summary>
        Task<CatalogOfferInFlightSyncResult> SyncInFlightStoredStatusAsync(
            int offerId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// After an attach FK change: sync <paramref name="nextOfferId"/> then
        /// <paramref name="previousOfferId"/> when they differ.
        /// </summary>
        Task<CatalogOfferInFlightSyncResult> SyncInFlightStoredStatusForAttachChangeAsync(
            int? previousOfferId,
            int? nextOfferId,
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
