using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned why bullets for Campaign recommendations (CMP-13).
    /// Grounded on live metrics so copy stays aligned with the chosen type.
    /// </summary>
    public static class CampaignRecommendationWhyFacts
    {
        public static IReadOnlyList<string> ForType(
            string type,
            CampaignRecommendationMetrics metrics
        )
        {
            return type switch
            {
                "thank-recent-guests" => ThankBullets(metrics),
                "re-engage" => ReEngageBullets(metrics),
                "recovery-follow-up" => RecoveryBullets(metrics),
                _ => Array.Empty<string>(),
            };
        }

        private static IReadOnlyList<string> ThankBullets(
            CampaignRecommendationMetrics metrics
        )
        {
            var bullets = new List<string>(3);
            if (metrics.NewGuests > 0)
            {
                bullets.Add(
                    $"{metrics.NewGuests} new guests joined recently and can be thanked."
                );
            }

            if (metrics.PositiveFeedback > 0)
            {
                bullets.Add(
                    $"{metrics.PositiveFeedback} guests left positive Feedback in the period."
                );
            }

            if (metrics.MarketingEligible > 0)
            {
                bullets.Add(
                    $"{metrics.MarketingEligible} guests are marketing-eligible for a thank-you send."
                );
            }

            return EnsureFallback(
                bullets,
                "Recent guest activity supports a thank-you Campaign."
            );
        }

        private static IReadOnlyList<string> ReEngageBullets(
            CampaignRecommendationMetrics metrics
        )
        {
            var bullets = new List<string>(3);
            if (metrics.DormantGuests > 0)
            {
                bullets.Add(
                    $"{metrics.DormantGuests} dormant guests have not engaged recently."
                );
            }

            if (metrics.CampaignsSentInWindow > 0
                && metrics.UniqueEmailOpensInWindow == 0)
            {
                bullets.Add(
                    $"{metrics.CampaignsSentInWindow} Campaigns sent in the period with no recorded email opens yet."
                );
            }
            else if (metrics.CampaignsSentInWindow > 0)
            {
                bullets.Add(
                    $"{metrics.CampaignsSentInWindow} Campaigns sent in the period — a re-engage send can reach quieter guests."
                );
            }

            if (metrics.ActiveOffers > 0)
            {
                bullets.Add(
                    $"{metrics.ActiveOffers} Active Offers are ready to attach to a re-engage Campaign."
                );
            }

            return EnsureFallback(
                bullets,
                "Quieter guest activity supports a re-engage Campaign."
            );
        }

        private static IReadOnlyList<string> RecoveryBullets(
            CampaignRecommendationMetrics metrics
        )
        {
            var bullets = new List<string>(3);
            if (metrics.NeedsRecovery > 0)
            {
                bullets.Add(
                    $"{metrics.NeedsRecovery} guests still need recovery follow-up."
                );
            }

            if (metrics.NeedsAttentionCount > 0)
            {
                bullets.Add(
                    $"{metrics.NeedsAttentionCount} Feedback items need attention in the period."
                );
            }

            if (metrics.OpenFeedbackCount > 0)
            {
                bullets.Add(
                    $"{metrics.OpenFeedbackCount} open Feedback threads remain at this location."
                );
            }

            return EnsureFallback(
                bullets,
                "Open recovery work supports a follow-up Campaign."
            );
        }

        private static IReadOnlyList<string> EnsureFallback(
            List<string> bullets,
            string fallback
        )
        {
            if (bullets.Count == 0)
            {
                bullets.Add(fallback);
            }

            return bullets;
        }
    }
}
