using TummlyBackend.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantAskIntentTests
    {
        [Theory]
        [InlineData("How many guests have we got across all our location?")]
        [InlineData("How many guests are on all Locations")]
        [InlineData("total guests across all owned locations")]
        [InlineData("guest count for this location")]
        [InlineData("how many guests do we have?")]
        public void HasRetrieveAsk_GuestCountPhrasing_IsTrue(string message)
        {
            Assert.True(AssistantAskIntent.HasRetrieveAsk(message));
        }

        [Theory]
        [InlineData("How many guests have we got across all our location?")]
        [InlineData("How many guests are on all Locations")]
        [InlineData("total guests across owned locations")]
        [InlineData("list guests at Camden")]
        public void ClassifyGrounded_GuestCountPhrasing_IsListGuests(string message)
        {
            Assert.Equal(
                AssistantGroundedAsk.ListGuests,
                AssistantAskIntent.ClassifyGrounded(message)
            );
        }

        [Theory]
        [InlineData("How many campaigns do we have ?")]
        [InlineData("xplain performance")]
        public void HasRetrieveAsk_CampaignsAndPerformance_IsTrue(string message)
        {
            Assert.True(AssistantAskIntent.HasRetrieveAsk(message));
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("what is up?")]
        [InlineData("random gibberish xyz")]
        public void HasRetrieveAsk_Chitchat_IsFalse(string message)
        {
            Assert.False(AssistantAskIntent.HasRetrieveAsk(message));
        }

        [Theory]
        [InlineData("Any Campaigns live?")]
        [InlineData("Any Campaigns sending?")]
        [InlineData("Have we had any QR scans today?")]
        [InlineData("Did anyone redeem an Offer today?")]
        [InlineData("Feedback this week")]
        public void HasRetrieveAsk_SynonymPhrases_IsTrue(string message)
        {
            Assert.True(AssistantAskIntent.HasRetrieveAsk(message));
        }

        [Theory]
        [InlineData("Can you create a Campaign?")]
        [InlineData("create campaign")]
        [InlineData("create a campaign")]
        [InlineData("start a campaign")]
        [InlineData("help me create a campaign")]
        [InlineData("Create a Campaign for recent guests.")]
        public void Classify_LegalCreateCampaign_IsNotMutate(string message)
        {
            Assert.NotEqual(
                AssistantAskKind.Mutate,
                AssistantAskIntent.Classify(message)
            );
            Assert.False(
                AssistantAskIntent.IsFullRefusal(
                    AssistantAskIntent.Classify(message)
                )
            );
            Assert.False(AssistantAskIntent.LooksLikeMutateAsk(message));
        }

        [Theory]
        [InlineData("schedule a campaign")]
        [InlineData("send it now")]
        [InlineData("change the status")]
        [InlineData("mark as resolved")]
        public void Classify_RealMutateAsks_StayMutate(string message)
        {
            Assert.Equal(
                AssistantAskKind.Mutate,
                AssistantAskIntent.Classify(message)
            );
            Assert.True(AssistantAskIntent.LooksLikeMutateAsk(message));
        }
    }
}
