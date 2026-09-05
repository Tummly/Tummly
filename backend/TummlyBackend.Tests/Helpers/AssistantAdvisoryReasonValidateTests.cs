using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAdvisoryReasonValidateTests
    {
        private static RestaurantContextSnapshot SnapshotWithCovers(
            decimal covers = 12m,
            Flag? accountFlag = null
        )
            => new(
                "2026-09-05",
                new SingleLocation("10"),
                new PeriodWindow(new DateOnly(2026, 8, 7), new DateOnly(2026, 9, 5)),
                new PeriodWindow(new DateOnly(2026, 7, 8), new DateOnly(2026, 8, 6)),
                new AccountSection(
                    new MetricPoint(covers, 10m, 20m),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    accountFlag is null ? [] : [accountFlag]
                ),
                new CampaignsSection([], [], []),
                new OffersSection([], [], []),
                new FeedbackSection(
                    new MetricPoint(70m, 65m, 5m),
                    [],
                    [],
                    0,
                    []
                ),
                new GuestsSection(
                    new MetricPoint(10m, 8m, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    []
                ),
                new CaptureSection(
                    new MetricPoint(20m, 18m, null),
                    new MetricPoint(10m, 12m, null),
                    new MetricPoint(50m, 40m, null),
                    null,
                    []
                ),
                new RecentActionsSection([]),
                new SnapshotMeta(false, 60, [])
            );

        [Fact]
        public void EvidenceRefResolves_AcceptsExactPrefixAndFlagPaths()
        {
            var snapshot = SnapshotWithCovers(
                accountFlag: new Flag(
                    "COVERS_UP",
                    "Covers rose",
                    FlagSeverity.Notable,
                    ["Covers"]
                )
            );
            var allowed = AssistantAdvisoryReasonValidate.BuildAllowedEvidencePaths(
                snapshot
            );

            Assert.True(
                AssistantAdvisoryReasonValidate.EvidenceRefResolves(
                    "Account",
                    allowed
                )
            );
            Assert.True(
                AssistantAdvisoryReasonValidate.EvidenceRefResolves(
                    "Account.Covers",
                    allowed
                )
            );
            Assert.True(
                AssistantAdvisoryReasonValidate.EvidenceRefResolves(
                    "Account.Flags.COVERS_UP",
                    allowed
                )
            );
            Assert.False(
                AssistantAdvisoryReasonValidate.EvidenceRefResolves(
                    "Account.UnknownMetric",
                    allowed
                )
            );
        }

        [Fact]
        public void Validate_Direct_DropsRecommendations()
        {
            var snapshot = SnapshotWithCovers();
            var output = new AssistantAdvisoryReasonOutput(
                "direct",
                "Summary only.",
                null,
                [
                    new AssistantAdvisoryReasonRecommendation(
                        AssistantAdvisoryReasonStructuredOutput.AdviceOnlyAction,
                        "Headline",
                        "Reason",
                        ["Account.Covers"],
                        "high"
                    ),
                ],
                ["Account.Covers"]
            );

            var result = AssistantAdvisoryReasonValidate.Validate(
                output,
                snapshot,
                NullLogger.Instance
            );

            var valid = Assert.IsType<AdvisoryReasonValidateResult.Valid>(result);
            Assert.Empty(valid.Output.Recommendations);
            Assert.Equal("Summary only.", valid.Output.Summary);
        }

        [Fact]
        public void Validate_Clarify_MissingQuestion_IsFallback()
        {
            var snapshot = SnapshotWithCovers();
            var output = new AssistantAdvisoryReasonOutput(
                "clarify",
                "Need more.",
                null,
                [],
                ["Account"]
            );

            var result = AssistantAdvisoryReasonValidate.Validate(
                output,
                snapshot,
                NullLogger.Instance
            );

            Assert.IsType<AdvisoryReasonValidateResult.FallbackNoClearDriver>(result);
        }

        [Fact]
        public void Validate_Clarify_Valid_ReturnsModelRequestedGap()
        {
            var snapshot = SnapshotWithCovers();
            var output = new AssistantAdvisoryReasonOutput(
                "clarify",
                "Need more.",
                "Which metric should I use?",
                [],
                ["Account"]
            );

            var result = AssistantAdvisoryReasonValidate.Validate(
                output,
                snapshot,
                NullLogger.Instance,
                conversationTurnId: "turn-clarify"
            );

            var clarify = Assert.IsType<AdvisoryReasonValidateResult.Clarify>(result);
            Assert.Equal(AdvisoryGapReason.ModelRequested, clarify.Gap.Reason);
            Assert.Equal("Which metric should I use?", clarify.Gap.PartialDiagnosisNote);
            Assert.Equal("turn-clarify", clarify.Gap.ConversationTurnId);
        }

        [Fact]
        public void Validate_Advisory_DropsBadEvidenceRef_KeepsGood()
        {
            var snapshot = SnapshotWithCovers();
            var output = new AssistantAdvisoryReasonOutput(
                "advisory",
                "Summary.",
                null,
                [
                    new AssistantAdvisoryReasonRecommendation(
                        AssistantAdvisoryReasonStructuredOutput.AdviceOnlyAction,
                        "Keep me",
                        "Good ref",
                        ["Account.Covers"],
                        "high"
                    ),
                    new AssistantAdvisoryReasonRecommendation(
                        AssistantTask.CreateCampaignDraft,
                        "Drop me",
                        "Bad ref",
                        ["Account.NotARealField"],
                        "low"
                    ),
                ],
                ["Account.Covers"]
            );

            var result = AssistantAdvisoryReasonValidate.Validate(
                output,
                snapshot,
                NullLogger.Instance
            );

            var valid = Assert.IsType<AdvisoryReasonValidateResult.Valid>(result);
            var kept = Assert.Single(valid.Output.Recommendations);
            Assert.Equal("Keep me", kept.Headline);
        }

        [Fact]
        public void RenderBody_AppendsRouterActionInParentheses()
        {
            var body = AssistantAdvisoryReasonValidate.RenderBody(
                new AssistantAdvisoryReasonOutput(
                    "advisory",
                    "Summary.",
                    null,
                    [
                        new AssistantAdvisoryReasonRecommendation(
                            AssistantTask.CreateCampaignDraft,
                            "Draft a campaign",
                            "Covers are soft",
                            ["Account.Covers"],
                            "medium"
                        ),
                        new AssistantAdvisoryReasonRecommendation(
                            AssistantAdvisoryReasonStructuredOutput.AdviceOnlyAction,
                            "Watch capture",
                            "Funnel is thin",
                            ["Capture.DropOffRate"],
                            "low"
                        ),
                    ],
                    ["Account.Covers", "Capture.DropOffRate"]
                )
            );

            Assert.Equal(
                "Summary.\n\n"
                + "Draft a campaign — Covers are soft (create-campaign-draft)\n"
                + "Watch capture — Funnel is thin",
                body
            );
        }
    }
}
