using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class HomeRecommendationDomainRouterTests
    {
        private static HomeRecommendationMetrics Empty()
            => new(
                OpenFeedbackCount: 0,
                NeedsAttentionCount: 0,
                GuestsJoinedInWindow: 0,
                MarketingEligible: 0,
                ActiveOffers: 0,
                HasNoActiveOffers: false,
                OfferNeedsAttentionCount: 0,
                NewGuests: 0,
                PositiveFeedback: 0,
                DormantGuests: 0,
                NeedsRecovery: 0
            );

        [Fact]
        public void SelectType_AllZeros_ReturnsNone()
        {
            var type = HomeRecommendationDomainRouter.SelectType(Empty());
            Assert.Equal("none", type);
            Assert.True(HomeRecommendationContract.IsAllowedType(type));
        }

        [Fact]
        public void SelectType_OpenFeedback_WinsReviewOpenFeedback()
        {
            var metrics = Empty() with { OpenFeedbackCount = 3 };
            Assert.Equal(
                "review-open-feedback",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_NeedsAttentionOnly_WinsReviewOpenFeedback()
        {
            var metrics = Empty() with { NeedsAttentionCount = 1 };
            Assert.Equal(
                "review-open-feedback",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_GuestJoinSignal_WinsThankOrFollowGuest()
        {
            var metrics = Empty() with { GuestsJoinedInWindow = 2 };
            Assert.Equal(
                "thank-or-follow-guest",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_MarketingEligibleOnly_WinsThankOrFollowGuest()
        {
            var metrics = Empty() with { MarketingEligible = 5 };
            Assert.Equal(
                "thank-or-follow-guest",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_OfferHealthAttention_WinsPromoteOrFixOffer()
        {
            var metrics = Empty() with
            {
                OfferNeedsAttentionCount = 2,
                ActiveOffers = 1,
            };
            Assert.Equal(
                "promote-or-fix-offer",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_ConfirmedNoActiveOffers_WinsPromoteOrFixOffer()
        {
            var metrics = Empty() with { HasNoActiveOffers = true };
            Assert.Equal(
                "promote-or-fix-offer",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_OnlyCampaignThankSignals_WinsThankRecentGuests()
        {
            var metrics = Empty() with { NewGuests = 4 };
            Assert.Equal(
                "thank-recent-guests",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_PositiveFeedbackCampaignSignal_WinsThankRecentGuests()
        {
            var metrics = Empty() with { PositiveFeedback = 2 };
            Assert.Equal(
                "thank-recent-guests",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_OnlyCampaignDormant_WinsReEngage()
        {
            var metrics = Empty() with { DormantGuests = 3 };
            Assert.Equal(
                "re-engage",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_OnlyCampaignNeedsRecovery_WinsRecoveryFollowUp()
        {
            var metrics = Empty() with { NeedsRecovery = 2 };
            Assert.Equal(
                "recovery-follow-up",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_HomeNativeBeatsCampaignWhenBothPresent()
        {
            var metrics = Empty() with
            {
                NeedsAttentionCount = 1,
                NewGuests = 10,
                DormantGuests = 10,
                NeedsRecovery = 10,
            };
            Assert.Equal(
                "review-open-feedback",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_CampaignRecoveryBeatsThankAndReEngage()
        {
            var metrics = Empty() with
            {
                NeedsRecovery = 1,
                NewGuests = 5,
                DormantGuests = 5,
            };
            Assert.Equal(
                "recovery-follow-up",
                HomeRecommendationDomainRouter.SelectType(metrics)
            );
        }

        [Fact]
        public void SelectType_NeverEmitsOutsideAllowList()
        {
            var samples = new[]
            {
                Empty(),
                Empty() with { OpenFeedbackCount = 1 },
                Empty() with { GuestsJoinedInWindow = 1 },
                Empty() with { HasNoActiveOffers = true },
                Empty() with { OfferNeedsAttentionCount = 1 },
                Empty() with { NewGuests = 1 },
                Empty() with { PositiveFeedback = 1 },
                Empty() with { DormantGuests = 1 },
                Empty() with { NeedsRecovery = 1 },
                Empty() with
                {
                    OpenFeedbackCount = 1,
                    GuestsJoinedInWindow = 1,
                    HasNoActiveOffers = true,
                    NeedsRecovery = 1,
                },
            };

            foreach (var metrics in samples)
            {
                var type = HomeRecommendationDomainRouter.SelectType(metrics);
                Assert.True(
                    HomeRecommendationContract.IsAllowedType(type),
                    $"Unexpected type '{type}'"
                );
            }
        }

        [Fact]
        public void MetricsBag_HasNoSetupChecklistField()
        {
            var names = typeof(HomeRecommendationMetrics)
                .GetProperties()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.DoesNotContain("SetupChecklist", names);
            Assert.DoesNotContain("ChecklistComplete", names);
            Assert.DoesNotContain("SetupComplete", names);
            Assert.DoesNotContain("ChecklistCompletion", names);
        }
    }
}
