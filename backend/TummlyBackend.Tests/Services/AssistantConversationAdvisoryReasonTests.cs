using Microsoft.EntityFrameworkCore;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_AdvisoryClear_UsesReasonProvider_NotLiveAnswer()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 42, "Clear Venue");
            var snapshot = HealthyAdvisorySnapshot(locationId);
            var reason = new FakeAssistantAdvisoryReasonProvider();
            reason.SucceedWith(
                new AssistantAdvisoryReasonOutput(
                    "advisory",
                    "Covers look healthy this month.",
                    null,
                    [
                        new AssistantAdvisoryReasonRecommendation(
                            AssistantAdvisoryReasonStructuredOutput.AdviceOnlyAction,
                            "Keep watching covers",
                            "Account.Covers is up.",
                            ["Account.Covers"],
                            "high"
                        ),
                    ],
                    ["Account.Covers"]
                )
            );
            var liveBefore = _fake.CompleteCount;
            var service = CreateConversationService(
                restaurantContextSnapshot: new FixedRestaurantContextSnapshot(snapshot),
                advisoryReason: reason
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 42,
                    FirstSendRequest(locationId, "How are we doing this month?")
                )
            );

            Assert.Equal(1, reason.CompleteCount);
            Assert.Equal(liveBefore, _fake.CompleteCount);
            var body = outcome.Conversation.Messages[^1].Body;
            Assert.Contains(
                "Covers look healthy this month.",
                body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Keep watching covers — Account.Covers is up.",
                body,
                StringComparison.Ordinal
            );
            Assert.Null(await StoredGapStateOrNullAsync(outcome.Conversation.Id));
        }

        [Fact]
        public async Task SendTurn_AdvisoryClear_Clarify_PersistsModelRequestedGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 43, "Clarify Venue");
            var snapshot = HealthyAdvisorySnapshot(locationId);
            var reason = new FakeAssistantAdvisoryReasonProvider();
            reason.SucceedWith(
                new AssistantAdvisoryReasonOutput(
                    "clarify",
                    "I need one more detail.",
                    "Which metric should I use?",
                    [],
                    ["Account"]
                )
            );
            var service = CreateConversationService(
                restaurantContextSnapshot: new FixedRestaurantContextSnapshot(snapshot),
                advisoryReason: reason
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 43,
                    FirstSendRequest(locationId, "How are we doing this month?")
                )
            );

            Assert.Equal("gap", outcome.Conversation.Messages[^1].Class);
            var gapState = await StoredGapStateOrNullAsync(outcome.Conversation.Id);
            Assert.NotNull(gapState);
            Assert.Equal(AssistantGapTurn.GapKindAdvisory, gapState!.GapKind);
            Assert.Equal(AssistantGapTurn.KindAdvisoryModel, gapState.Kind);
            Assert.Equal(
                nameof(AdvisoryGapReason.ModelRequested),
                gapState.AdvisoryReason
            );
            Assert.Equal(
                AssistantGapTurn.GapSourceModelRequested,
                gapState.GapSource
            );
            Assert.Equal("Which metric should I use?", gapState.PartialDiagnosisNote);
            Assert.Equal(
                "Which metric should I use?",
                outcome.Conversation.Messages[^1].Body
            );
        }

        [Fact]
        public async Task SendTurn_AdvisoryClear_Clarify_FreeFormReply_Resumes()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 46, "Resume Clarify");
            var snapshot = HealthyAdvisorySnapshot(locationId);
            var reason = new FakeAssistantAdvisoryReasonProvider();
            reason.EnqueueSucceedWith(
                new AssistantAdvisoryReasonOutput(
                    "clarify",
                    "I need one more detail.",
                    "Which metric should I use?",
                    [],
                    ["Account"]
                )
            );
            reason.EnqueueSucceedWith(
                new AssistantAdvisoryReasonOutput(
                    "direct",
                    "Focusing on covers.",
                    null,
                    [],
                    ["Account.Covers"]
                )
            );
            var service = CreateConversationService(
                restaurantContextSnapshot: new FixedRestaurantContextSnapshot(snapshot),
                advisoryReason: reason
            );

            var first = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 46,
                    FirstSendRequest(locationId, "How are we doing this month?")
                )
            );
            Assert.Equal("gap", first.Conversation.Messages[^1].Class);

            var second = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 46,
                    FirstSendRequest(
                        locationId,
                        "covers",
                        first.Conversation.Id
                    )
                )
            );

            Assert.Equal("grounded", second.Conversation.Messages[^1].Class);
            Assert.Contains(
                "Focusing on covers.",
                second.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Null(await StoredGapStateOrNullAsync(second.Conversation.Id));
            Assert.Equal(2, reason.CompleteCount);
        }

        [Fact]
        public async Task SendTurn_AdvisoryClear_MalformedClarify_FallsBackNoGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 44, "Fallback Venue");
            var snapshot = HealthyAdvisorySnapshot(locationId);
            var reason = new FakeAssistantAdvisoryReasonProvider();
            reason.SucceedWith(
                new AssistantAdvisoryReasonOutput(
                    "clarify",
                    "I need one more detail.",
                    null,
                    [],
                    ["Account"]
                )
            );
            var service = CreateConversationService(
                restaurantContextSnapshot: new FixedRestaurantContextSnapshot(snapshot),
                advisoryReason: reason
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 44,
                    FirstSendRequest(locationId, "How are we doing this month?")
                )
            );

            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            Assert.Equal(
                AssistantAdvisoryReasonValidate.NoClearDriverBody,
                outcome.Conversation.Messages[^1].Body
            );
            Assert.Null(await StoredGapStateOrNullAsync(outcome.Conversation.Id));
        }

        [Fact]
        public async Task SendTurn_AdvisoryClear_BadEvidenceRef_KeepsSummary()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 45, "BadRef Venue");
            var snapshot = HealthyAdvisorySnapshot(locationId);
            var reason = new FakeAssistantAdvisoryReasonProvider();
            reason.SucceedWith(
                new AssistantAdvisoryReasonOutput(
                    "advisory",
                    "Summary stays.",
                    null,
                    [
                        new AssistantAdvisoryReasonRecommendation(
                            AssistantTask.CreateCampaignDraft,
                            "Bad recommendation",
                            "Invented path",
                            ["Account.NotReal"],
                            "low"
                        ),
                    ],
                    ["Account"]
                )
            );
            var service = CreateConversationService(
                restaurantContextSnapshot: new FixedRestaurantContextSnapshot(snapshot),
                advisoryReason: reason
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 45,
                    FirstSendRequest(locationId, "How are we doing this month?")
                )
            );

            Assert.Equal("Summary stays.", outcome.Conversation.Messages[^1].Body);
            Assert.DoesNotContain(
                "Bad recommendation",
                outcome.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        private static RestaurantContextSnapshot HealthyAdvisorySnapshot(int locationId)
            => new(
                "2026-09-05",
                new SingleLocation(locationId.ToString()),
                new PeriodWindow(new DateOnly(2026, 8, 7), new DateOnly(2026, 9, 5)),
                new PeriodWindow(new DateOnly(2026, 7, 8), new DateOnly(2026, 8, 6)),
                new AccountSection(
                    new MetricPoint(120m, 100m, 20m),
                    new MetricPoint(1000m, 900m, 10m),
                    new MetricPoint(20m, 18m, null),
                    new MetricPoint(10m, 8m, null),
                    []
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
                new SnapshotMeta(IsNewAccount: false, TotalDaysOfHistory: 60, [])
            );
    }
}
