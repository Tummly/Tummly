using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefStructuredOutputTests
    {
        [Fact]
        public void TryParseModelContent_AcceptsWatchNextWithOneToThreeLines()
        {
            var content = ValidBodyJson(watchNext: ["Watch service wait times."]);

            var ok = WeeklyBriefStructuredOutput.TryParseModelContent(
                content,
                out var body,
                out var invalid
            );

            Assert.True(ok);
            Assert.False(invalid);
            Assert.NotNull(body);
            Assert.Single(body!.WatchNext);
            Assert.Equal("Watch service wait times.", body.WatchNext[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void TryParseModelContent_RejectsWatchNextOutsideAllowListLength(
            int lineCount
        )
        {
            var lines = Enumerable
                .Range(0, lineCount)
                .Select(i => $"Advisory line {i + 1}.")
                .ToArray();
            var content = ValidBodyJson(watchNext: lines);

            var ok = WeeklyBriefStructuredOutput.TryParseModelContent(
                content,
                out var body,
                out var invalid
            );

            Assert.False(ok);
            Assert.True(invalid);
            Assert.Null(body);
        }

        [Fact]
        public void TryParseModelContent_RejectsEmptySectionOutsideAllowList()
        {
            var content = ValidBodyJson(
                watchNext: ["Watch wait times."],
                feedbackSummary: "Guest Jane said the soup was cold."
            );

            var ok = WeeklyBriefStructuredOutput.TryParseModelContent(
                content,
                out var body,
                out var invalid
            );

            Assert.False(ok);
            Assert.True(invalid);
            Assert.Null(body);
        }

        [Fact]
        public void TryParseModelContent_RejectsCrossSectionEmptySummary()
        {
            var content = ValidBodyJson(
                watchNext: ["Watch wait times."],
                feedbackSummary: WeeklyBriefStructuredOutput.EmptyCaptureSummary
            );

            var ok = WeeklyBriefStructuredOutput.TryParseModelContent(
                content,
                out var body,
                out var invalid
            );

            Assert.False(ok);
            Assert.True(invalid);
            Assert.Null(body);
        }

        [Fact]
        public void BuildSystemPrompt_RequiresAggregatesOnlyPrivacyRules()
        {
            var prompt = WeeklyBriefStructuredOutput.BuildSystemPrompt("2026-08-21");

            Assert.Contains(
                WeeklyBriefStructuredOutput.SchemaVersion,
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "aggregate metrics and Detected Tag rollups",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Do not include guest names, emails, phones, or feedback comment bodies",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                $"watchNext must have {WeeklyBriefStructuredOutput.WatchNextMinLength} to {WeeklyBriefStructuredOutput.WatchNextMaxLength}",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                WeeklyBriefStructuredOutput.PromptSchemaRevision,
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildRequestJson_FeedsOnlyDocumentedAggregateMetrics()
        {
            var metrics = new WeeklyBriefMetrics(
                GuestsJoined: 12,
                QrScanEvents: 20,
                FeedbackCount: 5,
                PositiveFeedbackCount: 3,
                NeutralFeedbackCount: 1,
                NegativeFeedbackCount: 1,
                NeedsAttentionCount: 1,
                DetectedTagCounts: new Dictionary<string, int>
                {
                    ["Service"] = 2,
                    ["Wait time"] = 1,
                },
                ActiveOffers: 2,
                ClaimsInWeek: 4,
                RedemptionsInWeek: 1,
                CampaignsSentInWeek: 1,
                CampaignRecipientsReached: 40
            );
            var input = new WeeklyBriefProviderInput(
                LocationName: "Harbour Kitchen",
                WeekKey: "2026-W33",
                CoverageStartUtc: new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                CoverageEndUtcExclusive: new DateTime(
                    2026,
                    8,
                    17,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                Metrics: metrics
            );

            var requestJson = WeeklyBriefStructuredOutput.BuildRequestJson(
                "weekly-brief-deployment",
                input,
                "2026-08-21"
            );
            var root = JsonNode.Parse(requestJson)!.AsObject();
            var userContent = root["messages"]![1]!["content"]!.GetValue<string>();
            var payload = JsonNode.Parse(userContent)!.AsObject();
            var metricsNode = payload["metrics"]!.AsObject();

            Assert.Equal(
                WeeklyBriefStructuredOutput.SchemaVersion,
                payload["schemaVersion"]!.GetValue<string>()
            );
            Assert.Equal(12, metricsNode["guestsJoined"]!.GetValue<int>());
            Assert.Equal(20, metricsNode["qrScanEvents"]!.GetValue<int>());
            Assert.Equal(
                2,
                metricsNode["detectedTagCounts"]!["Service"]!.GetValue<int>()
            );
            Assert.Equal(40, metricsNode["campaignRecipientsReached"]!.GetValue<int>());

            Assert.False(metricsNode.ContainsKey("guestName"));
            Assert.False(metricsNode.ContainsKey("email"));
            Assert.False(metricsNode.ContainsKey("phone"));
            Assert.False(metricsNode.ContainsKey("comment"));
            Assert.False(metricsNode.ContainsKey("feedbackComment"));
            Assert.False(metricsNode.ContainsKey("feedbackThemeCounts"));
            Assert.False(metricsNode.ContainsKey("captureEvents"));
            Assert.DoesNotContain("Jane", userContent, StringComparison.Ordinal);
            Assert.DoesNotContain("@", userContent, StringComparison.Ordinal);
        }

        [Fact]
        public void BuildSchema_RequiresWatchNextLengthBounds()
        {
            var schema = WeeklyBriefStructuredOutput.BuildSchema();
            var watchNext = schema["properties"]!["watchNext"]!.AsObject();

            Assert.Equal(
                WeeklyBriefStructuredOutput.WatchNextMinLength,
                watchNext["minItems"]!.GetValue<int>()
            );
            Assert.Equal(
                WeeklyBriefStructuredOutput.WatchNextMaxLength,
                watchNext["maxItems"]!.GetValue<int>()
            );
        }

        private static string ValidBodyJson(
            string[] watchNext,
            string? feedbackSummary = null
        )
        {
            return JsonSerializer.Serialize(
                new
                {
                    headline = "Quiet week with steady capture.",
                    capture = new
                    {
                        hasData = true,
                        summary = "Twelve guests joined.",
                        echoedCounts = new Dictionary<string, int>
                        {
                            ["guestsJoined"] = 12,
                        },
                    },
                    feedback = new
                    {
                        hasData = false,
                        summary = feedbackSummary
                            ?? WeeklyBriefStructuredOutput.EmptyFeedbackSummary,
                    },
                    offers = new
                    {
                        hasData = false,
                        summary = WeeklyBriefStructuredOutput.EmptyOffersSummary,
                    },
                    campaigns = new
                    {
                        hasData = false,
                        summary = WeeklyBriefStructuredOutput.EmptyCampaignsSummary,
                    },
                    watchNext,
                }
            );
        }
    }
}
