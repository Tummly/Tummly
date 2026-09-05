using System.Text.Json.Nodes;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAdvisoryReasonStructuredOutputTests
    {
        [Fact]
        public void BuildSchema_RequiresAnswerType()
        {
            var schema = AssistantAdvisoryReasonStructuredOutput.BuildSchema();
            var required = schema["required"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();

            Assert.Contains("answer_type", required);
            Assert.Contains("summary", required);
            Assert.Contains("clarifying_question", required);
            Assert.Contains("recommendations", required);
            Assert.Contains("evidence_used", required);

            var answerTypeEnum = schema["properties"]!["answer_type"]!["enum"]!
                .AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();
            Assert.Contains("direct", answerTypeEnum);
            Assert.Contains("advisory", answerTypeEnum);
            Assert.Contains("product_expert", answerTypeEnum);
            Assert.Contains("clarify", answerTypeEnum);
        }

        [Fact]
        public void BuildSystemPrompt_IncludesAnswerTypeAndGuardrails()
        {
            var prompt = AssistantAdvisoryReasonStructuredOutput.BuildSystemPrompt(
                "2026-09-05"
            );

            Assert.Contains("answer_type", prompt, StringComparison.Ordinal);
            Assert.Contains("Never invent metrics", prompt, StringComparison.Ordinal);
            Assert.Contains("guest names", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("advice-only", prompt, StringComparison.Ordinal);
        }

        [Fact]
        public void TryParseModelContent_RoundTripsAdvisory()
        {
            const string json = """
                {
                  "answer_type": "advisory",
                  "summary": "Covers are steady.",
                  "clarifying_question": null,
                  "recommendations": [
                    {
                      "action": "advice-only",
                      "headline": "Watch covers",
                      "reason": "Account.Covers is stable.",
                      "evidence_ref": ["Account.Covers"],
                      "confidence": "high"
                    }
                  ],
                  "evidence_used": ["Account.Covers"]
                }
                """;

            Assert.True(
                AssistantAdvisoryReasonStructuredOutput.TryParseModelContent(
                    json,
                    out var output,
                    out var invalid
                )
            );
            Assert.False(invalid);
            Assert.NotNull(output);
            Assert.Equal("advisory", output!.AnswerType);
            Assert.Equal("Covers are steady.", output.Summary);
            Assert.Null(output.ClarifyingQuestion);
            var recommendation = Assert.Single(output.Recommendations);
            Assert.Equal("advice-only", recommendation.Action);
            Assert.Equal(["Account.Covers"], recommendation.EvidenceRef);
            Assert.Equal(["Account.Covers"], output.EvidenceUsed);
        }

        [Fact]
        public void BuildRequestJson_IncludesSnapshotAndHistory()
        {
            var snapshot = new RestaurantContextSnapshot(
                "2026-09-05",
                new SingleLocation("10"),
                new PeriodWindow(new DateOnly(2026, 8, 7), new DateOnly(2026, 9, 5)),
                new PeriodWindow(new DateOnly(2026, 7, 8), new DateOnly(2026, 8, 6)),
                new AccountSection(
                    new MetricPoint(12m, 10m, 20m),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    []
                ),
                new CampaignsSection([], [], []),
                new OffersSection([], [], []),
                new FeedbackSection(
                    new MetricPoint(70m, 65m, null),
                    [],
                    [],
                    0,
                    []
                ),
                new GuestsSection(
                    new MetricPoint(1m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    []
                ),
                new CaptureSection(
                    new MetricPoint(1m, null, null),
                    new MetricPoint(1m, null, null),
                    new MetricPoint(0m, null, null),
                    null,
                    []
                ),
                new RecentActionsSection([]),
                new SnapshotMeta(false, 60, [])
            );

            var request = AssistantAdvisoryReasonStructuredOutput.BuildRequestJson(
                "deploy-1",
                new AssistantAdvisoryReasonInput(
                    "How are we doing?",
                    snapshot,
                    [
                        new AssistantLiveAnswerHistoryTurn(
                            AssistantMessageRole.User,
                            "Earlier ask"
                        ),
                    ]
                ),
                "2026-09-05"
            );

            Assert.Contains("answer_type", request, StringComparison.Ordinal);
            Assert.Contains("How are we doing?", request, StringComparison.Ordinal);
            Assert.Contains("Earlier ask", request, StringComparison.Ordinal);
            Assert.Contains("Account", request, StringComparison.Ordinal);
        }
    }
}
