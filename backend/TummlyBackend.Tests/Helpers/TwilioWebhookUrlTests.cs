using Microsoft.Extensions.Configuration;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class TwilioWebhookUrlTests
    {
        private static IConfiguration Config(string? baseUrl) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["PublicApi:BaseUrl"] = baseUrl }
                )
                .Build();

        [Fact]
        public void TwilioWebhookUrl_NoScheme_ReturnsNull()
        {
            Assert.Null(
                TwilioWebhookUrl.For(Config("api.tummly.test"), TwilioWebhookUrl.SmsStatusPath)
            );
        }

        [Fact]
        public void TwilioWebhookUrl_NotHttpScheme_ReturnsNull()
        {
            Assert.Null(
                TwilioWebhookUrl.For(Config("ftp://api.tummly.test"), TwilioWebhookUrl.SmsStatusPath)
            );
        }

        [Fact]
        public void TwilioWebhookUrl_AbsoluteHttps_ReturnsUrl()
        {
            Assert.Equal(
                "https://api.tummly.test/api/webhooks/twilio/sms-status",
                TwilioWebhookUrl.For(Config("https://api.tummly.test/"), TwilioWebhookUrl.SmsStatusPath)
            );
        }
    }
}
