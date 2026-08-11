using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    public interface IOffersMetricsService
    {
        Task<OffersPerformanceDto> GetPerformanceAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Null when the catalog offer does not exist.
        /// </summary>
        Task<OfferMetricsDto?> GetOfferMetricsAsync(
            int offerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        );
    }
}
