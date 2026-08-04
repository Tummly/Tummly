using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class FeedbackRecoveryDraftStructuredOutputTests
    {
        [Fact]
        public void SanitizeGuestProse_RepairsLowByteTruncatedPunctuation()
        {
            // U+0019 / U+0014 are low bytes of U+2019 (’) / U+2014 (—).
            var input =
                "I\u0019m sorry - tell us \u0014 feedback like this is serious.";

            var sanitized = FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                input
            );

            Assert.Equal(
                "I'm sorry - tell us - feedback like this is serious.",
                sanitized
            );
        }

        [Fact]
        public void SanitizeGuestProse_MapsFancyUnicodeToAscii()
        {
            var input = "We\u2019re sorry\u2014the food didn\u2019t meet expectations\u2026";

            var sanitized = FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                input
            );

            Assert.Equal(
                "We're sorry-the food didn't meet expectations...",
                sanitized
            );
        }

        [Fact]
        public void SanitizeGuestProse_KeepsNewlinesAndDropsOtherControls()
        {
            var input = "Line one\nLine\u0000two\tTabbed";

            var sanitized = FeedbackRecoveryDraftStructuredOutput.SanitizeGuestProse(
                input
            );

            Assert.Equal("Line one\nLinetwo\tTabbed", sanitized);
        }

        [Fact]
        public void BuildSystemPrompt_RequiresPlainAsciiPunctuation()
        {
            var prompt = FeedbackRecoveryDraftStructuredOutput.BuildSystemPrompt(
                "2026-07-18"
            );

            Assert.Contains("plain ASCII", prompt, StringComparison.Ordinal);
            Assert.Contains(
                FeedbackRecoveryDraftStructuredOutput.ProsePunctuationRevision,
                prompt,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain('\u2014', prompt);
            Assert.DoesNotContain('\u2019', prompt);
        }

        [Fact]
        public void TryParseModelContent_SanitizesBodyAndSubject()
        {
            // Build JSON with the same control chars the live model returns.
            var body =
                "Dear Salman,\n\nI\u0019m very sorry to hear that. "
                + "Thank you for taking the time to tell us \u0014 "
                + "feedback like this is taken very seriously.";
            var subject =
                "We\u0019re sorry the food didn\u0019t meet your expectations";
            var content = System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    body,
                    subject,
                    channel = "email"
                }
            );

            var ok = FeedbackRecoveryDraftStructuredOutput.TryParseModelContent(
                content,
                requestedChannel: "email",
                out var result,
                out var invalidOutput
            );

            Assert.True(ok);
            Assert.False(invalidOutput);
            var succeeded = Assert.IsType<FeedbackRecoveryDraftResult.Succeeded>(
                result
            );
            Assert.Equal(
                "Dear Salman,\n\nI'm very sorry to hear that. Thank you for taking the time to tell us - feedback like this is taken very seriously.",
                succeeded.Body
            );
            Assert.Equal(
                "We're sorry the food didn't meet your expectations",
                succeeded.Subject
            );
            Assert.Equal("email", succeeded.Channel);
        }
    }
}
