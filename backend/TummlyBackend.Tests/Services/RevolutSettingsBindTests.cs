using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class RevolutSettingsBindTests
    {
        [Fact]
        public void Binds_RevolutSection_AndFlatVatKeys()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Revolut:SecretKey"] = "sk_bound",
                        ["Revolut:WebhookSigningSecret"] = "whsec_bound",
                        ["Revolut:ApiBaseUrl"] =
                            RevolutSettings.LiveApiBaseUrl,
                        ["Revolut:ApiVersion"] =
                            RevolutSettings.DefaultApiVersion,
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.StarterMonthly}"
                        ] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                        [TummlySellerVatSettings.RegistrationNumberKey] =
                            "GB999",
                        [TummlySellerVatSettings.EffectiveDateKey] =
                            "2025-06-01",
                        [TummlySellerVatSettings.LegalNameKey] = "Bound Ltd",
                        [TummlySellerVatSettings.RegisteredAddressKey] =
                            "Bound Address",
                    }
                )
                .Build();

            var services = new ServiceCollection();
            services.Configure<RevolutSettings>(
                configuration.GetSection(RevolutSettings.SectionName)
            );
            services.Configure<TummlySellerVatSettings>(options =>
            {
                options.RegistrationNumber =
                    configuration[TummlySellerVatSettings.RegistrationNumberKey]
                    ?? string.Empty;
                options.EffectiveDate =
                    configuration[TummlySellerVatSettings.EffectiveDateKey]
                    ?? string.Empty;
                options.LegalName =
                    configuration[TummlySellerVatSettings.LegalNameKey]
                    ?? string.Empty;
                options.RegisteredAddress =
                    configuration[TummlySellerVatSettings.RegisteredAddressKey]
                    ?? string.Empty;
            });

            using var provider = services.BuildServiceProvider();
            var revolut = provider.GetRequiredService<IOptions<RevolutSettings>>()
                .Value;
            var vat = provider
                .GetRequiredService<IOptions<TummlySellerVatSettings>>()
                .Value;

            Assert.Equal("sk_bound", revolut.SecretKey);
            Assert.Equal("whsec_bound", revolut.WebhookSigningSecret);
            Assert.Equal(RevolutSettings.LiveApiBaseUrl, revolut.ApiBaseUrl);
            Assert.Equal(RevolutSettings.DefaultApiVersion, revolut.ApiVersion);
            Assert.Equal(
                "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                revolut.PlanVariations[
                    RevolutPlanVariationKeys.StarterMonthly
                ]
            );
            Assert.True(revolut.IsLiveHost);
            Assert.False(revolut.IsSandboxHost);

            Assert.Equal("GB999", vat.RegistrationNumber);
            Assert.Equal("2025-06-01", vat.EffectiveDate);
            Assert.Equal("Bound Ltd", vat.LegalName);
            Assert.Equal("Bound Address", vat.RegisteredAddress);
            Assert.True(vat.IsComplete);
        }
    }
}
