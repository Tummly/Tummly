using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class CreditTopUpLineCopyTests
    {
        [Theory]
        [InlineData("ai", 500, "AI Credits 500 Topup")]
        [InlineData("ai", 100, "AI Credits 100 Topup")]
        [InlineData("sms", 1000, "SMS Credits 1000 Topup")]
        [InlineData("email", 5000, "Email Credits 5000 Topup")]
        public void FormatLineDescription_UsesFriendlyCheckoutLabel(
            string channel,
            int quantity,
            string expected
        )
        {
            Assert.Equal(
                expected,
                CreditTopUpLineCopy.FormatLineDescription(channel, quantity)
            );
        }
    }
}
