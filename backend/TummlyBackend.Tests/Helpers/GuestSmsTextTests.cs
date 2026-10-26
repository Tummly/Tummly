using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestSmsTextTests
    {
        [Fact]
        public void ForReply_AddsPrefix()
        {
            Assert.Equal(
                "The Golden Fork: Thanks for your message.",
                GuestSmsText.ForReply("The Golden Fork", "Thanks for your message.")
            );
        }

        [Fact]
        public void ForReply_BlankName_NoPrefix()
        {
            Assert.Equal("Thanks.", GuestSmsText.ForReply("   ", "Thanks."));
            Assert.Equal("Thanks.", GuestSmsText.ForReply(string.Empty, "Thanks."));
        }

        [Fact]
        public void ForCampaign_AddsPrefixAndFooter()
        {
            Assert.Equal(
                "The Golden Fork: Weekend specials.\nReply STOP to stop texts.",
                GuestSmsText.ForCampaign("The Golden Fork", "Weekend specials.")
            );
        }
    }
}
