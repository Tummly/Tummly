using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class CampaignNegativeFeedbackCopyTests
    {
        private const string GenericInvite =
            """
            You are invited back to The Golden Fork
            Hello,

            We hope you are well. We wanted to invite you back to The Golden Fork to enjoy our latest dishes, relaxed atmosphere, and friendly service.
            """;

        [Fact]
        public void GenericInvite_IsReplacedWithMakeItRightCopy()
        {
            Assert.True(
                CampaignNegativeFeedbackCopy.NeedsGovernedCopy(
                    "You are invited back to The Golden Fork",
                    GenericInvite
                )
            );

            var copy = CampaignNegativeFeedbackCopy.Governed(
                "The Golden Fork",
                "email",
                offerTitle: null
            );

            Assert.Equal("We are sorry your visit was not right", copy.Subject);
            Assert.Contains("make it right", copy.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("The Golden Fork", copy.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("latest dishes", copy.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("website", copy.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SincereDraft_IsKept()
        {
            Assert.False(
                CampaignNegativeFeedbackCopy.NeedsGovernedCopy(
                    "We are sorry",
                    "We are sorry your last visit was not right. We would like to make it right."
                )
            );
        }
    }
}
