using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RevolutMerchantCreateGateTests
    {
        private const string StarterMonthly =
            RevolutPlanVariationKeys.StarterMonthly;

        [Fact]
        public void Evaluate_Passes_WhenVatRevolutAndTargetVariationPresent()
        {
            var gate = CreateGate(
                vat: FullVat(),
                revolut: FullLiveRevolut(withStarterMonthly: true)
            );

            Assert.Null(gate.Evaluate(StarterMonthly));
        }

        [Fact]
        public void Evaluate_ReturnsVatNotReady_WhenAnyVatLegalKeyMissing()
        {
            var incomplete = FullVat();
            incomplete.LegalName = " ";
            var gate = CreateGate(incomplete, FullLiveRevolut(withStarterMonthly: true));

            Assert.Equal(
                RevolutMerchantCreateGate.VatNotReady,
                gate.Evaluate(StarterMonthly)
            );
        }

        [Fact]
        public void Evaluate_ReturnsRevolutNotReady_WhenMerchantSecretMissing()
        {
            var revolut = FullLiveRevolut(withStarterMonthly: true);
            revolut.SecretKey = "";
            var gate = CreateGate(FullVat(), revolut);

            Assert.Equal(
                RevolutMerchantCreateGate.RevolutNotReady,
                gate.Evaluate(StarterMonthly)
            );
        }

        [Fact]
        public void Evaluate_ReturnsRevolutNotReady_WhenWebhookSigningSecretMissing()
        {
            var revolut = FullLiveRevolut(withStarterMonthly: true);
            revolut.WebhookSigningSecret = "  ";
            var gate = CreateGate(FullVat(), revolut);

            Assert.Equal(
                RevolutMerchantCreateGate.RevolutNotReady,
                gate.Evaluate(StarterMonthly)
            );
        }

        [Fact]
        public void Evaluate_ReturnsPlanVariationMissing_WhenTargetSkuAbsentFromMap()
        {
            var revolut = FullLiveRevolut(withStarterMonthly: false);
            var gate = CreateGate(FullVat(), revolut);

            Assert.Equal(
                RevolutMerchantCreateGate.PlanVariationMissing,
                gate.Evaluate(StarterMonthly)
            );
        }

        [Fact]
        public void Evaluate_WithoutLookupKey_SkipsVariationMap()
        {
            var revolut = FullLiveRevolut(withStarterMonthly: false);
            var gate = CreateGate(FullVat(), revolut);

            Assert.Null(gate.Evaluate(planVariationLookupKey: null));
        }

        [Fact]
        public void SandboxAndLiveHosts_AreDistinctConfig()
        {
            var live = FullLiveRevolut(withStarterMonthly: true);
            var sandbox = FullLiveRevolut(withStarterMonthly: true);
            sandbox.ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl;

            Assert.Equal(RevolutSettings.LiveApiBaseUrl, live.ApiBaseUrl);
            Assert.Equal(RevolutSettings.SandboxApiBaseUrl, sandbox.ApiBaseUrl);
            Assert.NotEqual(live.ApiBaseUrl, sandbox.ApiBaseUrl);
            Assert.True(live.IsLiveHost);
            Assert.True(sandbox.IsSandboxHost);
        }

        [Fact]
        public void Evaluate_UsesBoundHost_DoesNotCrossReadMaps()
        {
            var sandbox = FullLiveRevolut(withStarterMonthly: true);
            sandbox.ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl;
            sandbox.PlanVariations = new Dictionary<string, string>
            {
                [StarterMonthly] = "sandbox-variation-uuid",
            };
            var gate = CreateGate(FullVat(), sandbox);

            Assert.Null(gate.Evaluate(StarterMonthly));
            Assert.Equal(
                "sandbox-variation-uuid",
                sandbox.PlanVariations[StarterMonthly]
            );
            Assert.True(sandbox.IsSandboxHost);
        }

        private static RevolutMerchantCreateGate CreateGate(
            TummlySellerVatSettings vat,
            RevolutSettings revolut
        )
        {
            return new RevolutMerchantCreateGate(
                Options.Create(vat),
                Options.Create(revolut)
            );
        }

        private static TummlySellerVatSettings FullVat()
        {
            return new TummlySellerVatSettings
            {
                RegistrationNumber = "GB123456789",
                EffectiveDate = "2024-01-01",
                LegalName = "Tummly Ltd",
                RegisteredAddress = "1 Example Street, London",
            };
        }

        private static RevolutSettings FullLiveRevolut(bool withStarterMonthly)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (withStarterMonthly)
            {
                map[StarterMonthly] = "11111111-1111-1111-1111-111111111111";
            }

            return new RevolutSettings
            {
                SecretKey = "sk_test_placeholder",
                WebhookSigningSecret = "whsec_placeholder",
                ApiBaseUrl = RevolutSettings.LiveApiBaseUrl,
                ApiVersion = RevolutSettings.DefaultApiVersion,
                PlanVariations = map,
            };
        }
    }
}
