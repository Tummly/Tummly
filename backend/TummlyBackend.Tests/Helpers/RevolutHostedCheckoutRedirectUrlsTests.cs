using Microsoft.Extensions.Configuration;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class RevolutHostedCheckoutRedirectUrlsTests
    {
        [Fact]
        public void BuildBillingCreditsTabUrl_IncludesTopUpOutcomeForCreditTopUp()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://qa.tummly.com",
                    }
                )
                .Build();

            var url = RevolutHostedCheckoutRedirectUrls.BuildBillingCreditsTabUrl(
                configuration,
                "Single",
                42,
                "credits-usage",
                new Dictionary<string, string> { ["topUpOutcome"] = "success" }
            );

            Assert.Equal(
                "https://qa.tummly.com/single-dashboard/settings/billing-credits?location=42&tab=credits-usage&topUpOutcome=success",
                url
            );
        }

        [Fact]
        public void BuildBillingCreditsTabUrl_UsesRevolutRedirectOverride()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "http://localhost:5173",
                        ["Frontend:RevolutRedirectBaseUrl"] = "https://qa.tummly.com",
                    }
                )
                .Build();

            var url = RevolutHostedCheckoutRedirectUrls.BuildBillingCreditsTabUrl(
                configuration,
                "Multi",
                7,
                "credits-usage"
            );

            Assert.StartsWith(
                "https://qa.tummly.com/multi-dashboard/settings/billing-credits?",
                url
            );
        }

        [Fact]
        public void BuildBillingCreditsTabUrl_RejectsLocalhostBaseUrl()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "http://localhost:5173",
                    }
                )
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                RevolutHostedCheckoutRedirectUrls.BuildBillingCreditsTabUrl(
                    configuration,
                    "Single",
                    1,
                    "credits-usage"
                )
            );

            Assert.Equal(
                RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode,
                ex.Message
            );
        }
    }
}
