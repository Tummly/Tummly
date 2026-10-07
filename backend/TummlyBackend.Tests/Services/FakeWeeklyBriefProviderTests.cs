using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Fake fixture rules for KOL-RPTA-004 / lock 05 (metrics-shaped watchNext).
    /// </summary>
    public class FakeWeeklyBriefProviderTests
    {
        [Fact]
        public void FixtureFor_ThinMetrics_EmptyWatchNextWithoutLegacyFiller()
        {
            var metrics = EmptyMetrics() with { GuestsJoined = 1 };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var enrichment = FakeWeeklyBriefProvider.FixtureEnrichmentFor(
                metrics,
                body
            );

            Assert.Empty(body.WatchNext);
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchNeedsAttention,
                body.WatchNext
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchOfferRate,
                body.WatchNext
            );
            Assert.Empty(enrichment.ActionWording);
        }

        [Fact]
        public void FixtureFor_SoleNoMaterialChangeBag_EmptyWatchNext()
        {
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 5,
                QrScanEvents = 5,
            };
            var bag = WeeklyBriefInsightCandidates.Emit(
                metrics,
                prior: metrics,
                priorPrior: null,
                controlSignals: [],
                dataQualityIssue: false
            );
            Assert.True(
                WeeklyBriefInsightNarrativeValidation.IsSoleNoMaterialChange(bag)
            );

            var body = FakeWeeklyBriefProvider.FixtureFor(metrics, bag);

            Assert.Empty(body.WatchNext);
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchNeedsAttention,
                body.WatchNext
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchOfferRate,
                body.WatchNext
            );
        }

        [Fact]
        public void FixtureFor_NeedsAttention_NonEmptyWatchNextAndActionWording()
        {
            var thin = EmptyMetrics() with { GuestsJoined = 5, QrScanEvents = 5 };
            var thinBody = FakeWeeklyBriefProvider.FixtureFor(thin);

            var needsAttention = thin with { NeedsAttentionCount = 3 };
            var bag = WeeklyBriefInsightCandidates.Emit(
                needsAttention,
                prior: thin,
                priorPrior: null,
                controlSignals:
                [
                    new WeeklyBriefControlSignalInput(
                        WeeklyBriefInsightCandidates.ActionFeedbackNeedsAttention,
                        new Dictionary<string, object>
                        {
                            ["needsAttentionCount"] = 3,
                        }
                    ),
                ],
                dataQualityIssue: false
            );
            var body = FakeWeeklyBriefProvider.FixtureFor(needsAttention, bag);
            var enrichment = FakeWeeklyBriefProvider.FixtureEnrichmentFor(
                needsAttention,
                body,
                bag
            );

            Assert.NotEmpty(body.WatchNext);
            Assert.NotEqual(thinBody.WatchNext, body.WatchNext);
            Assert.Contains(
                body.WatchNext,
                line => line.Contains("Needs attention", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchNeedsAttention,
                body.WatchNext
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchOfferRate,
                body.WatchNext
            );
            Assert.Single(enrichment.ActionWording);
            Assert.Equal(
                WeeklyBriefEnrichmentActionKinds.FeedbackNeedsAttention,
                enrichment.ActionWording[0].Kind
            );
        }

        private static WeeklyBriefMetrics EmptyMetrics()
            => new(
                GuestsJoined: 0,
                QrScanEvents: 0,
                FeedbackCount: 0,
                PositiveFeedbackCount: 0,
                NeutralFeedbackCount: 0,
                NegativeFeedbackCount: 0,
                NeedsAttentionCount: 0,
                DetectedTagCounts: new Dictionary<string, int>(),
                ActiveOffers: 0,
                ClaimsInWeek: 0,
                RedemptionsInWeek: 0,
                CampaignsSentInWeek: 0,
                CampaignRecipientsReached: 0,
                UnsubscribesInWeek: 0
            );
    }
}
