using TummlyBackend.DTOs.Offers;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Persist Offer issue + MVP Claim for catalog attach paths (ticket 28),
    /// plus Staff Redeem Check / Mark as redeemed (ticket 38).
    /// </summary>
    public interface IOfferIssueService
    {
        /// <summary>
        /// Campaign Email/SMS provider Accepted: create issue + Claim proxy.
        /// No-op when offer missing/inactive, guest opted out, or already issued
        /// for this campaign+guest.
        /// </summary>
        Task<OfferIssue?> IssueOnCampaignAcceptedAsync(
            int campaignId,
            int locationGuestId,
            int catalogOfferId,
            string channel,
            DateTime atUtc,
            CancellationToken cancellationToken = default,
            string? preallocatedClaimCode = null
        );

        /// <summary>
        /// Guest form thank-you submit: issue when a live thank-you catalog
        /// attach exists; otherwise no-op. MVP Claim proxy sets ClaimedAt at
        /// issue (paint endpoint not shipped yet).
        /// </summary>
        Task<OfferIssue?> IssueOnThankYouSubmitAsync(
            int locationId,
            int locationGuestId,
            int? feedbackId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Recovery Send: issue from durable Recovery catalog attach.
        /// No-op when offer missing/inactive or guest opted out.
        /// MVP Claim proxy sets ClaimedAt at issue (Accepted-style).
        /// </summary>
        Task<OfferIssue?> IssueOnRecoverySendAsync(
            int catalogOfferId,
            int locationGuestId,
            int feedbackId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Staff Check offer: resolve Offer Claim code at Owned location.
        /// Failed checks that map to a known Offer write Failed attempts.
        /// Claim is not required.
        /// </summary>
        Task<OfferRedeemCheckResult> CheckClaimCodeAsync(
            int locationId,
            string code,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Staff Mark as redeemed: persist RedeemedAt on the Offer issue.
        /// Claim is not required.
        /// </summary>
        Task<OfferRedeemMarkResult> RedeemClaimCodeAsync(
            int locationId,
            string code,
            string issueId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        );
    }
}
