using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class CampaignRecommendationWhyFactsTests
    {
        private static CampaignRecommendationMetrics Empty()
            => new(
                MarketingEligible: 0,
                AllGuests: 0,
                NewGuests: 0,
                NeedsRecovery: 0,
                PositiveFeedback: 0,
                DormantGuests: 0,
                OpenFeedbackCount: 0,
                NeedsAttentionCount: 0,
                ActiveOffers: 0,
                OfferNeedsAttentionCount: 0,
                CampaignsSentInWindow: 0,
                UniqueEmailOpensInWindow: 0,
                CampaignAttributedRedemptionsInWindow: 0
            );

        [Fact]
        public void ForType_Thank_CitesNewGuestsAndMarketingEligible()
        {
            var metrics = Empty() with
            {
                NewGuests = 4,
                MarketingEligible = 12,
            };

            var bullets = CampaignRecommendationWhyFacts.ForType(
                "thank-recent-guests",
                metrics
            );

            Assert.Contains(
                bullets,
                b => b.Contains("4 new guests", StringComparison.Ordinal)
            );
            Assert.Contains(
                bullets,
                b => b.Contains("12 guests are marketing-eligible", StringComparison.Ordinal)
            );
        }

        [Fact]
        public void ForType_ReEngage_CitesDormantAndSentWithNoOpens()
        {
            var metrics = Empty() with
            {
                DormantGuests = 7,
                CampaignsSentInWindow = 2,
                UniqueEmailOpensInWindow = 0,
                ActiveOffers = 1,
            };

            var bullets = CampaignRecommendationWhyFacts.ForType("re-engage", metrics);

            Assert.Contains(
                bullets,
                b => b.Contains("7 dormant guests", StringComparison.Ordinal)
            );
            Assert.Contains(
                bullets,
                b =>
                    b.Contains("2 Campaigns sent", StringComparison.Ordinal)
                    && b.Contains("no recorded email opens", StringComparison.Ordinal)
            );
            Assert.Contains(
                bullets,
                b => b.Contains("1 Active Offers", StringComparison.Ordinal)
            );
        }

        [Fact]
        public void ForType_Recovery_CitesNeedsRecoveryAndFeedback()
        {
            var metrics = Empty() with
            {
                NeedsRecovery = 3,
                NeedsAttentionCount = 5,
                OpenFeedbackCount = 2,
            };

            var bullets = CampaignRecommendationWhyFacts.ForType(
                "recovery-follow-up",
                metrics
            );

            Assert.Contains(
                bullets,
                b => b.Contains("3 guests still need recovery", StringComparison.Ordinal)
            );
            Assert.Contains(
                bullets,
                b => b.Contains("5 Feedback items need attention", StringComparison.Ordinal)
            );
            Assert.Contains(
                bullets,
                b => b.Contains("2 open Feedback threads", StringComparison.Ordinal)
            );
        }

        [Fact]
        public void ForType_Unknown_ReturnsEmpty()
        {
            Assert.Empty(CampaignRecommendationWhyFacts.ForType("none", Empty()));
        }
    }
}
