using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class TwilioRecoveryGuestSmsDeliveryTests
    {
        [Fact]
        public async Task SendAsync_DemoSuppressedDestination_ReturnsAcceptedWithoutProviderId()
        {
            var sut = new TwilioRecoveryGuestSmsDelivery(
                Options.Create(
                    new TwilioSettings
                    {
                        AccountSid = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                        AuthToken = "twilio_test_auth_token_unit",
                        RecoveryFromNumber = "+447700900888",
                    }
                ),
                NullLogger<TwilioRecoveryGuestSmsDelivery>.Instance,
                new DemoSideEffectGuard(
                    Options.Create(
                        new DemoSessionOptions
                        {
                            Enabled = true,
                            SuppressSideEffects = true,
                        }
                    )
                ),
                new ConfigurationBuilder().Build()
            );

            var result = await sut.SendAsync("07700900400", "Thanks for your message.");

            var accepted = Assert.IsType<RecoveryGuestSmsDeliveryResult.Accepted>(result);
            Assert.Null(accepted.ProviderMessageId);
            Assert.False(accepted.StatusCallbackSet);
        }
    }
}
