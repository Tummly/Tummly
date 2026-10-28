using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Server Campaign eligibility service — Matched / Currently eligible /
    /// Excluded (+ Email / SMS eligible) for a Campaign audience.
    /// </summary>
    public interface ICampaignEligibilityService
    {
        /// <summary>
        /// Stage-1 estimate for one audience key at a location.
        /// Soft-lock / account / Billing suppression activate only when those
        /// stores exist — never invent pass/fail for missing checks.
        /// </summary>
        Task<CampaignEligibilityDto> EvaluateAsync(
            int locationId,
            string audienceKey,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Same as <see cref="EvaluateAsync(int, string, CancellationToken)"/> with the
        /// Campaign Offer and recent-redeemers period. Use for Campaign-owned audiences.
        /// </summary>
        Task<CampaignEligibilityDto> EvaluateAsync(
            int locationId,
            string audienceKey,
            CampaignAudienceScope scope,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// LocationGuest ids currently eligible on the selected channel
        /// (for recipient freeze at schedule commit).
        /// </summary>
        Task<IReadOnlyList<int>> ListChannelEligibleLocationGuestIdsAsync(
            int locationId,
            string audienceKey,
            string channel,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Same as the channel overload above, with the Campaign Offer and period.
        /// The fire-time recheck and schedule freeze use this overload.
        /// </summary>
        Task<IReadOnlyList<int>> ListChannelEligibleLocationGuestIdsAsync(
            int locationId,
            string audienceKey,
            string channel,
            CampaignAudienceScope scope,
            CancellationToken cancellationToken = default
        );
    }
}
