using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantRecoveryIntentTests
    {
        [Theory]
        [InlineData("Help with guest recovery")]
        [InlineData("help with guest recovery")]
        [InlineData("Prepare a recovery response")]
        public void LooksLikeRecoveryAsk_ChipAndPreparePhrases(string message)
        {
            Assert.True(AssistantRecoveryIntent.LooksLikeRecoveryAsk(message));
        }

        [Fact]
        public void LooksLikeRecoveryAsk_UnrelatedAsk_False()
        {
            Assert.False(
                AssistantRecoveryIntent.LooksLikeRecoveryAsk("Summarise feedback this week")
            );
        }
    }
}
