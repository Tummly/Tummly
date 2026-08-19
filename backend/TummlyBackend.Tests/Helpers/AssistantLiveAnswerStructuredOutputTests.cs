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
                "The server owns Gap turns",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Do not put Markdown in title",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSystemPrompt_DoesNotRefuseLegalCreateCampaignDraftAsMutate()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Legal Create Campaign Draft, Offer path, and Recovery path asks",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "are not Mutate refusals",
                prompt,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "Mutate asks (create, send, or change records) are a refusal",
                prompt,
                StringComparison.Ordinal
            );
        }
    }
}
