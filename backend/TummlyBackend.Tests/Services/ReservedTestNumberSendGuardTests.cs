using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Provider send guard for the Ofcom reserved range 07700 900xxx. The guard
    /// refuses before any Twilio call, so these tests need no network.
    /// </summary>
    public class ReservedTestNumberSendGuardTests
    {
        private const string ReservedNumber = "07700900400";

        private static TwilioSettings ConfiguredSettings() =>
            new()
            {
                AccountSid = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                AuthToken = "twilio_test_auth_token_unit",
                VerifyServiceSid = "VAaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                RecoveryFromNumber = "+447911000001",
            };

        [Fact]
        public async Task RecoverySms_ReservedRange_RefusedWithoutProviderWhenPolicyOff()
        {
            using var policy = ReservedTestNumberPolicy.OverrideFor(false);
            var sut = new TwilioRecoveryGuestSmsDelivery(
                Options.Create(ConfiguredSettings()),
                NullLogger<TwilioRecoveryGuestSmsDelivery>.Instance,
                new DemoSideEffectGuard(Options.Create(new DemoSessionOptions())),
                new ConfigurationBuilder().Build()
            );

            var result = await sut.SendAsync(ReservedNumber, "Thanks for your message.");

            var failed = Assert.IsType<RecoveryGuestSmsDeliveryResult.Failed>(result);
            Assert.Equal("Unable to send Recovery SMS.", failed.Message);
        }

        [Fact]
        public async Task VerifyOtp_ReservedRange_RefusedWithoutProviderWhenPolicyOff()
        {
            using var policy = ReservedTestNumberPolicy.OverrideFor(false);
            var sut = new TwilioVerifySmsService(
                Options.Create(ConfiguredSettings()),
                NullLogger<TwilioVerifySmsService>.Instance,
                new DemoSideEffectGuard(Options.Create(new DemoSessionOptions())),
                new HttpContextAccessor()
            );

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.SendOtpSmsAsync(ReservedNumber)
            );
            Assert.Equal("Unable to send SMS verification code.", ex.Message);
        }

        [Fact]
        public void Policy_OverrideRestoresThePreviousValueWhenDisposed()
        {
            var before = ReservedTestNumberPolicy.AllowReservedTestNumbers;

            using (ReservedTestNumberPolicy.OverrideFor(!before))
            {
                Assert.Equal(!before, ReservedTestNumberPolicy.AllowReservedTestNumbers);
            }

            Assert.Equal(before, ReservedTestNumberPolicy.AllowReservedTestNumbers);
        }
    }
}
