using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    public interface IOfferVoidRequestService
    {
        Task<OfferVoidCreateResult> CreateAsync(
            int userId,
            CreateOfferVoidRequestBody body,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );

        Task<OfferVoidOutcomeResult> ApproveAsync(
            int userId,
            int requestId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );

        Task<OfferVoidOutcomeResult> RejectAsync(
            int userId,
            int requestId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );

        Task<OfferVoidRequestDetailDto?> GetDetailAsync(
            int requestId,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<OpenVoidAttentionOfferDto>> ListOpenAttentionAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Null when the catalog offer does not exist.
        /// </summary>
        Task<OfferDetailsVoidRequestsListDto?> ListForOfferAsync(
            int offerId,
            CancellationToken cancellationToken = default
        );

        Task NotifyApproversAsync(
            int requestId,
            CancellationToken cancellationToken = default
        );

        Task NotifySubmitterAsync(
            int requestId,
            string outcome,
            CancellationToken cancellationToken = default
        );
    }
}
