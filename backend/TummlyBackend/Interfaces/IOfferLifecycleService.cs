using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Offer Details lifecycle list reads — Claims + Redemptions tabs (ticket 40).
    /// </summary>
    public interface IOfferLifecycleService
    {
        /// <summary>
        /// Null when the catalog offer does not exist.
        /// </summary>
        Task<OfferDetailsClaimsListDto?> ListClaimsAsync(
            int offerId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Null when the catalog offer does not exist.
        /// </summary>
        Task<OfferDetailsRedemptionsListDto?> ListRedemptionsAsync(
            int offerId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );
    }
}
