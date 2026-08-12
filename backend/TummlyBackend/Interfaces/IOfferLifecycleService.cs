using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Offer Details lifecycle list reads — Claims / Redemptions / Campaigns
    /// (tickets 40–41) and location-wide redemption log (ticket 42).
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

        /// <summary>
        /// Location-wide redeemed + failed staff attempts across catalog offers.
        /// </summary>
        Task<OfferDetailsRedemptionsListDto> ListLocationRedemptionsAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Null when the catalog offer does not exist.
        /// </summary>
        Task<OfferDetailsLinkedCampaignsListDto?> ListLinkedCampaignsAsync(
            int offerId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Null when the catalog offer does not exist.
        /// </summary>
        Task<OfferDetailsIssuanceSourcesListDto?> ListIssuanceSourcesAsync(
            int offerId,
            CancellationToken cancellationToken = default
        );
    }
}
