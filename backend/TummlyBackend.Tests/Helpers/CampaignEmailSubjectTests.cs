using TummlyBackend.Helpers.EmailTemplates;

namespace TummlyBackend.Tests.Helpers
{
    public class CampaignEmailSubjectTests
    {
        [Fact]
        public void Format_UsesOfferAndRestaurant()
        {
            Assert.Equal(
                "Your 14% OFF YOUR NEXT ORDER from KFC",
                CampaignEmailSubject.Format("14% OFF YOUR NEXT ORDER", "KFC")
            );
        }

        [Fact]
        public void Format_TrimsWhitespace()
        {
            Assert.Equal(
                "Your Free dessert from Burger House",
                CampaignEmailSubject.Format("  Free dessert  ", "  Burger House  ")
            );
        }

        [Fact]
        public void Format_FallsBackWhenBlank()
        {
            Assert.Equal(
                "Your offer from us",
                CampaignEmailSubject.Format("  ", null)
            );
        }
    }
}
