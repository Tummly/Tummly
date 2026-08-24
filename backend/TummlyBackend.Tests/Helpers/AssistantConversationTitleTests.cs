using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantConversationTitleTests
    {
        [Fact]
        public void TryAccept_OverLengthLiveAnswerTitle_IsRejectedAfterEmptyGroundedShortensDisplay()
        {
            const string liveAnswerTitle =
                "No facts at Camden for the last 7 days and extra words after sixty";
            const string emptyGroundedTitle = "No facts at Camden for the last 7 days";

            Assert.Null(
                AssistantConversationTitle.TryAccept(liveAnswerTitle, liveAnswerTitle)
            );
            Assert.Null(
                AssistantConversationTitle.TryAccept(liveAnswerTitle, emptyGroundedTitle)
            );
        }

        [Fact]
        public void TryAccept_DistinctTitle_IsCutNotRejected()
        {
            const string proposed =
                "Bring back Email-eligible guests during the quiet lunch period this week";

            Assert.Equal(
                "Bring back Email-eligible guests during the quiet lunch",
                AssistantConversationTitle.TryAccept(proposed, "No facts at Camden")
            );
        }
    }
}
