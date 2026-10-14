using TummlyBackend.Helpers;
using TummlyBackend.Models;
// AssistantDraftPreviewFactory + AssistantMessagePresentationJson in Helpers

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAnswerPresentationShapeTests
    {
        [Fact]
        public void StripActionProse_RemovesViewFeedbackLines()
        {
            var body = AssistantAnswerPresentationShape.StripActionProse(
                "Slow service is the main issue.\n"
                + "- You can view feedback for more detail.\n"
                + "- Wait times rise after 7 PM."
            );

            Assert.DoesNotContain("view feedback", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Wait times rise after 7 PM.", body, StringComparison.Ordinal);
        }

        [Fact]
        public void ShapeLiveAnswer_FeedbackSummarise_AddsMetricsChartAndCta()
        {
            var feedback = new AssistantFeedbackEvidence(
                TotalCount: 13,
                SampleCount: 5,
                SucceededPositive: 2,
                SucceededNeutral: 4,
                SucceededNegative: 7,
                NeedsAttention: 3,
                TagCounts: [],
                Rows:
                [
                    new AssistantFeedbackEvidenceRow(
                        1,
                        new DateTime(2026, 10, 13, 20, 0, 0, DateTimeKind.Utc),
                        "Guest",
                        "negative",
                        "succeeded",
                        [],
                        "open",
                        true,
                        null,
                        "none",
                        "slow",
                        "FB-1",
                        null,
                        [],
                        null,
                        false
                    ),
                ],
                GuestRows: [],
                Placeholder4GuestRows: [],
                ContactRedactionTokens: []
            );
            var evidence = AssistantRetrievedEvidence.FromFeedback(feedback);

            var shaped = AssistantAnswerPresentationShape.ShapeLiveAnswer(
                "Summarise Feedback from the last 7 days",
                "Slow service is the main issue this week.",
                "I found 13 Feedback submissions.\n- You can view feedback now.",
                [],
                null,
                null,
                evidence
            );

            Assert.NotNull(shaped.Presentation);
            Assert.NotNull(shaped.Presentation!.Metrics);
            Assert.Contains(
                shaped.Presentation.Metrics!,
                metric => metric.Label == "Feedback" && metric.Value == "13"
            );
            Assert.NotNull(shaped.Presentation.Chart);
            Assert.NotNull(shaped.Presentation.ShortNote);
            Assert.Contains(
                shaped.Actions,
                action => action.Type == "view-feedback-set"
            );
            Assert.Contains(
                shaped.Actions,
                action => action.Type == "prepare-recovery"
            );
            Assert.NotNull(shaped.Presentation.RecommendedNextStep);
            Assert.Equal(
                "view-feedback-set",
                shaped.Presentation.RecommendedNextStep!.PrimaryAction?.Type
            );
            Assert.Equal(
                "prepare-recovery",
                shaped.Presentation.RecommendedNextStep.SecondaryAction?.Type
            );
            Assert.DoesNotContain(
                "view feedback",
                shaped.Body,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public void ForDraftPreview_SerializesCampaignCard()
        {
            var preview = AssistantDraftPreviewFactory.Campaign(
                campaignId: 42,
                name: "Thank recent guests",
                audienceLabel: "New (30d)",
                channel: "email",
                subject: "Thanks for visiting",
                messageBody: "We hope to see you again soon."
            );
            var presentation = AssistantAnswerPresentationShape.ForDraftPreview(preview);
            var json = AssistantMessagePresentationJson.Serialize(presentation);
            var dto = AssistantMessagePresentationJson.ParseDto(json);

            Assert.NotNull(dto?.DraftPreview);
            Assert.Equal("campaign", dto!.DraftPreview!.Kind);
            Assert.Equal(42, dto.DraftPreview.CampaignId);
            Assert.Equal("Draft · Not sent", dto.DraftPreview.StatusLabel);
            Assert.Equal("Thanks for visiting", dto.DraftPreview.Subject);
        }

        [Fact]
        public void CapBodyLength_TruncatesLongBodies()
        {
            var longBody = new string('a', AssistantAnswerPresentationShape.MaxBodyChars + 80);
            var capped = AssistantAnswerPresentationShape.CapBodyLength(longBody);
            Assert.True(capped.Length <= AssistantAnswerPresentationShape.MaxBodyChars);
            Assert.DoesNotContain("…", capped, StringComparison.Ordinal);
        }

        [Fact]
        public void ScrubTrailingEllipsis_RemovesDanglingDots()
        {
            var scrubbed = AssistantAnswerPresentationShape.ScrubTrailingEllipsis(
                "Four items need attention (examples below)...."
            );

            Assert.Equal(
                "Four items need attention (examples below)",
                scrubbed
            );
        }

        [Fact]
        public void ScrubFeedbackReferences_RemovesFdbCodes()
        {
            var scrubbed = AssistantAnswerPresentationShape.ScrubFeedbackReferences(
                "1. FDB-000003 — Priya Nguyen (Email) — “Bill had an unexpected charge.”\n"
                + "See also FDB-000033 in the set."
            );

            Assert.DoesNotContain("FDB-", scrubbed, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Priya Nguyen", scrubbed, StringComparison.Ordinal);
            Assert.Contains("Bill had an unexpected charge", scrubbed, StringComparison.Ordinal);
        }
    }
}
