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
    }
}
