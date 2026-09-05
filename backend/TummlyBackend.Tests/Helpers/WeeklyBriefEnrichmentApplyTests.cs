using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefEnrichmentApplyTests
    {
        [Fact]
        public void ResolveExecutiveSummary_PrefersEnrichmentWhenPresent()
        {
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Richer AI executive summary.",
                FeedbackSummary: null,
                ActionWording: Array.Empty<WeeklyBriefEnrichmentActionWording>()
            );

            Assert.Equal(
                "Richer AI executive summary.",
                WeeklyBriefEnrichmentApply.ResolveExecutiveSummary(
                    "Phase1 summary.",
                    enrichment
                )
            );
        }

        [Fact]
        public void ResolveExecutiveSummary_FallsBackWhenEnrichmentMissing()
        {
            Assert.Equal(
                "Phase1 summary.",
                WeeklyBriefEnrichmentApply.ResolveExecutiveSummary(
                    "Phase1 summary.",
                    enrichment: null
                )
            );
        }

        [Fact]
        public void ResolveFeedbackSummary_PrefersEnrichmentText()
        {
            var phase1 = new WeeklyBriefPhase1Sections.FeedbackSummaryDto(
                "Phase1 text.",
                "Phase1 subtitle.",
                NeedsAttentionCount: 3
            );
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: null,
                FeedbackSummary: new WeeklyBriefEnrichmentFeedbackSummary(
                    "Guests mentioned packaging and wait time.",
                    "Based on private feedback submitted between 6–12 July."
                ),
                ActionWording: Array.Empty<WeeklyBriefEnrichmentActionWording>()
            );

            var resolved = WeeklyBriefEnrichmentApply.ResolveFeedbackSummary(
                phase1,
                EmptyMetrics() with { NeedsAttentionCount = 3 },
                enrichment
            );

            Assert.NotNull(resolved);
            Assert.Equal(
                "Guests mentioned packaging and wait time.",
                resolved!.Text
            );
            Assert.Equal(
                "Based on private feedback submitted between 6–12 July.",
                resolved.Subtitle
            );
            Assert.Equal(3, resolved.NeedsAttentionCount);
        }

        [Fact]
        public void ApplyActionWording_OnlyWhenFactFires()
        {
            var facts = new List<object>
            {
                new WeeklyBriefRecommendedActions.FeedbackNeedsAttentionFactDto(
                    "feedback-needs-attention",
                    6,
                    "feedback-needs-attention"
                ),
            };
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: null,
                FeedbackSummary: null,
                ActionWording:
                [
                    new WeeklyBriefEnrichmentActionWording(
                        "feedback-needs-attention",
                        "Follow up with six guests",
                        "AI subtitle"
                    ),
                    new WeeklyBriefEnrichmentActionWording(
                        "repeated-invalid",
                        "Should not appear",
                        "No matching fact"
                    ),
                ]
            );

            var applied = WeeklyBriefEnrichmentApply.ApplyActionWording(
                facts,
                enrichment
            );

            Assert.Single(applied);
            var feedback =
                Assert.IsType<WeeklyBriefRecommendedActions.FeedbackNeedsAttentionFactDto>(
                    applied[0]
                );
            Assert.Equal("Follow up with six guests", feedback.Title);
            Assert.Equal("AI subtitle", feedback.Subtitle);
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
