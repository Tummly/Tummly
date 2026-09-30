using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class AssistantEmptyChannelAudienceTests
    {
        [Theory]
        [InlineData("4", AssistantEmptyChannelAudienceChoice.CreateAnyway)]
        [InlineData("Create SMS draft anyway", AssistantEmptyChannelAudienceChoice.CreateAnyway)]
        [InlineData("1", AssistantEmptyChannelAudienceChoice.SwitchChannel)]
        [InlineData("Use Email instead", AssistantEmptyChannelAudienceChoice.SwitchChannel)]
        [InlineData("2", AssistantEmptyChannelAudienceChoice.BroadenToEmail)]
        [InlineData("3", AssistantEmptyChannelAudienceChoice.Wait)]
        public void ResolveChoice_EmptySms_OrdinalAndLabels(
            string message,
            AssistantEmptyChannelAudienceChoice expected
        )
        {
            var chosen = AssistantCampaignDraftBind.ResolveNamedChoice(
                AssistantEmptyChannelAudience.EmptySmsOptions,
                message
            );
            Assert.NotNull(chosen);
            Assert.Equal(
                expected,
                AssistantEmptyChannelAudience.ResolveChoice("sms", chosen!)
            );
        }
    }
}
