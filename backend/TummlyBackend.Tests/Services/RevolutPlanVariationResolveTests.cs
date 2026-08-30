using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Ticket 13 seams: resolve hits map; missing key fails closed;
    /// top-up packs need no variation UUID.
    /// </summary>
    public class RevolutPlanVariationResolveTests
    {
        [Fact]
        public void TryGetPlanVariationId_HitsMap_ForCurrentRecurringKey()
        {
            var settings = FullMap();
            var expected = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

            Assert.True(
                settings.TryGetPlanVariationId(
                    RevolutPlanVariationKeys.GrowthAnnual,
                    out var id
                )
            );
            Assert.Equal(expected, id);
        }

        [Fact]
        public void TryGetPlanVariationId_FailsClosed_WhenCurrentKeyMissing()
        {
            var settings = FullMap();
            settings.PlanVariations.Remove(
                RevolutPlanVariationKeys.StarterMonthly
            );

            Assert.False(
                settings.TryGetPlanVariationId(
                    RevolutPlanVariationKeys.StarterMonthly,
                    out var id
                )
            );
            Assert.Equal(string.Empty, id);
        }

        [Fact]
        public void Gate_FailsClosed_WhenCurrentRecurringKeyMissing()
        {
            var settings = FullMap();
            settings.PlanVariations.Remove(
                RevolutPlanVariationKeys.GroupMonthly
            );
            var gate = new RevolutMerchantCreateGate(
                Options.Create(FullVat()),
                Options.Create(settings)
            );

            Assert.Equal(
                RevolutMerchantCreateGate.PlanVariationMissing,
                gate.Evaluate(RevolutPlanVariationKeys.GroupMonthly)
            );
        }

        [Fact]
        public void TopUpCreate_NeedsNoVariationUuid()
        {
            var settings = FullMap();
            settings.PlanVariations.Clear();
            var gate = new RevolutMerchantCreateGate(
                Options.Create(FullVat()),
                Options.Create(settings)
            );

            Assert.Null(gate.Evaluate(planVariationLookupKey: null));
            Assert.True(settings.HasMerchantApiConfig);
            Assert.Empty(settings.PlanVariations);
        }

        [Fact]
        public void CurrentRecurringKeys_AreExactlyEight_AndExcludeTopUps()
        {
            Assert.Equal(8, RevolutPlanVariationKeys.All.Count);
            Assert.DoesNotContain(
                RevolutPlanVariationKeys.All,
                key => key.Contains("_sms_", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(
                RevolutPlanVariationKeys.All,
                key => key.Contains("_ai_", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(
                RevolutPlanVariationKeys.All,
                key => key.Contains("_email_", StringComparison.Ordinal)
            );
        }

        private static RevolutSettings FullMap()
        {
            return new RevolutSettings
            {
                SecretKey = "sk_test",
                WebhookSigningSecret = "whsec",
                ApiBaseUrl = RevolutSettings.SandboxApiBaseUrl,
                ApiVersion = RevolutSettings.DefaultApiVersion,
                PlanVariations = new Dictionary<string, string>(
                    StringComparer.Ordinal
                )
                {
                    [RevolutPlanVariationKeys.StarterMonthly] =
                        "11111111-1111-1111-1111-111111111111",
                    [RevolutPlanVariationKeys.StarterAnnual] =
                        "22222222-2222-2222-2222-222222222222",
                    [RevolutPlanVariationKeys.GrowthMonthly] =
                        "33333333-3333-3333-3333-333333333333",
                    [RevolutPlanVariationKeys.GrowthAnnual] =
                        "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                    [RevolutPlanVariationKeys.GroupMonthly] =
                        "55555555-5555-5555-5555-555555555555",
                    [RevolutPlanVariationKeys.GroupAnnual] =
                        "66666666-6666-6666-6666-666666666666",
                    [RevolutPlanVariationKeys.GroupLocationMonthly] =
                        "77777777-7777-7777-7777-777777777777",
                    [RevolutPlanVariationKeys.GroupLocationAnnual] =
                        "88888888-8888-8888-8888-888888888888",
                },
            };
        }

        private static TummlySellerVatSettings FullVat()
        {
            return new TummlySellerVatSettings
            {
                RegistrationNumber = "GB123",
                EffectiveDate = "2024-01-01",
                LegalName = "Tummly Ltd",
                RegisteredAddress = "1 High Street",
            };
        }
    }
}
