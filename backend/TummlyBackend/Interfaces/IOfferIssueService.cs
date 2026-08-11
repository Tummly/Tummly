using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Persist Offer issue + MVP Claim for catalog attach paths (ticket 28).
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
            CancellationToken cancellationToken = default
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
    }
}
