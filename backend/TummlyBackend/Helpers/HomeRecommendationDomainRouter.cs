using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Deterministic Home recommendation type picker (ticket 02).
    /// Pure rules only — no Azure, no Campaigns recommendation service call.
    /// Campaign allow-list types are candidates only when Campaign Smart Group
    /// / marketing signals support them (same support gates as Campaigns pre-AI).
    /// </summary>
    /// <remarks>
    /// Priority (first match wins):
    /// <list type="number">
    /// <item><description>review-open-feedback — open or Needs attention feedback</description></item>
    /// <item><description>thank-or-follow-guest — guest join or marketing-eligible</description></item>
    /// <item><description>promote-or-fix-offer — no Active offers (confirmed) or offer health attention</description></item>
    /// <item><description>recovery-follow-up — NeedsRecovery Smart Group</description></item>
    /// <item><description>thank-recent-guests — NewGuests or PositiveFeedback Smart Group</description></item>
    /// <item><description>re-engage — DormantGuests Smart Group</description></item>
    /// <item><description>none — pre-AI quiet gate</description></item>
    /// </list>
    /// Metrics bag field list: see <see cref="HomeRecommendationMetrics"/>.
    /// Setup checklist completion is not an input.
    /// </remarks>
    public static class HomeRecommendationDomainRouter
    {
        /// <summary>
        /// Picks exactly one allow-listed recommendation type from aggregate metrics.
        /// </summary>
        public static string SelectType(HomeRecommendationMetrics metrics)
        {
            if (HasReviewOpenFeedbackSignal(metrics))
            {
                return "review-open-feedback";
            }

            if (HasThankOrFollowGuestSignal(metrics))
            {
                return "thank-or-follow-guest";
            }

            if (HasPromoteOrFixOfferSignal(metrics))
            {
                return "promote-or-fix-offer";
            }

            if (HasRecoveryFollowUpSignal(metrics))
            {
                return "recovery-follow-up";
            }

            if (HasThankRecentGuestsSignal(metrics))
            {
                return "thank-recent-guests";
            }

            if (HasReEngageSignal(metrics))
            {
                return "re-engage";
            }

            return "none";
        }

        private static bool HasReviewOpenFeedbackSignal(HomeRecommendationMetrics metrics)
            => metrics.OpenFeedbackCount > 0 || metrics.NeedsAttentionCount > 0;

        private static bool HasThankOrFollowGuestSignal(HomeRecommendationMetrics metrics)
            => metrics.GuestsJoinedInWindow > 0 || metrics.MarketingEligible > 0;

        private static bool HasPromoteOrFixOfferSignal(HomeRecommendationMetrics metrics)
            => metrics.HasNoActiveOffers || metrics.OfferNeedsAttentionCount > 0;

        /// <summary>
        /// Mirrors CampaignRecommendationService thank support (NewGuests | PositiveFeedback).
        /// </summary>
        private static bool HasThankRecentGuestsSignal(HomeRecommendationMetrics metrics)
            => metrics.NewGuests > 0 || metrics.PositiveFeedback > 0;

        /// <summary>
        /// Mirrors CampaignRecommendationService re-engage support (DormantGuests).
        /// </summary>
        private static bool HasReEngageSignal(HomeRecommendationMetrics metrics)
            => metrics.DormantGuests > 0;

        /// <summary>
        /// Mirrors CampaignRecommendationService recovery support (NeedsRecovery).
        /// </summary>
        private static bool HasRecoveryFollowUpSignal(HomeRecommendationMetrics metrics)
            => metrics.NeedsRecovery > 0;
    }
}
