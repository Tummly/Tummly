using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAttentionAskTests
    {
        [Theory]
        [InlineData("Show what needs attention")]
        [InlineData("What needs attention?")]
        [InlineData("What needs attention at Camden?")]
        [InlineData("what needs attention")]
        public void AttentionPhrases_AreNeedsAttention_EvenWhenGroundedClassifierIsSummarise(
            string message
        )
        {
            Assert.Equal(
                AssistantAttentionSurface.NeedsAttention,
                AssistantAttentionAsk.Detect(message)
            );
            Assert.Equal(
                AssistantGroundedAsk.Summarise,
                AssistantAskIntent.ClassifyGrounded(message)
            );
        }

        [Theory]
        [InlineData("What should I do today at Camden?")]
        [InlineData("what should I do today")]
        [InlineData("what should I do next")]
        public void TodayAndNextPhrases_AreRecommendedNextStep(string message)
        {
            Assert.Equal(
                AssistantAttentionSurface.RecommendedNextStep,
                AssistantAttentionAsk.Detect(message)
            );
        }

        [Theory]
        [InlineData("weekly brief")]
        [InlineData("watch next")]
        [InlineData("What happened last week?")]
        public void WeeklyBriefPhrases_AreWeeklyBrief(string message)
        {
            Assert.Equal(
                AssistantAttentionSurface.WeeklyBrief,
                AssistantAttentionAsk.Detect(message)
            );
        }

        [Theory]
        [InlineData("help me today")]
        [InlineData("what should I focus on")]
        [InlineData("what should I focus on today")]
        public void FocusTodayPhrases_AreMix(string message)
        {
            Assert.Equal(
                AssistantAttentionSurface.Mix,
                AssistantAttentionAsk.Detect(message)
            );
        }

        [Theory]
        [InlineData("Summarise recent feedback")]
        [InlineData("what needs recovery")]
        [InlineData("Create a campaign")]
        [InlineData("Compare last week to last month")]
        [InlineData("Summarise last week")]
        public void RestaurantAndCreateAsks_AreNotAttentionRetrieve(string message)
        {
            Assert.Equal(
                AssistantAttentionSurface.None,
                AssistantAttentionAsk.Detect(message)
            );
        }

        [Fact]
        public void PureAttention_DoesNotAlsoMatchTodayOrWeekly()
        {
            Assert.Equal(
                AssistantAttentionSurface.NeedsAttention,
                AssistantAttentionAsk.Detect("Show what needs attention")
            );
            Assert.NotEqual(
                AssistantAttentionSurface.RecommendedNextStep,
                AssistantAttentionAsk.Detect("Show what needs attention")
            );
            Assert.NotEqual(
                AssistantAttentionSurface.WeeklyBrief,
                AssistantAttentionAsk.Detect("Show what needs attention")
            );
        }
    }
}
