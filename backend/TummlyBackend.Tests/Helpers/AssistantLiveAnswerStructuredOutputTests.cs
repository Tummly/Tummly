using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantLiveAnswerStructuredOutputTests
    {
        [Fact]
        public void BuildSystemPrompt_RequiresGroundedMarkdownAllowList()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Grounded body formatting uses this Markdown allow-list only",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Use headings for distinct sections and bold for short",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "clarify bodies must be plain text",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Do not put Markdown in title",
                prompt,
                StringComparison.Ordinal
            );
        }
    }
}
