using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public sealed class TummlyWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                // Pay-path stubs in BillingCredits tests need A+B+C ready so the
                // fail-closed gate does not 503; empty-config gate coverage lives
                // in RevolutMerchantGateEndpointsTests.
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [TummlySellerVatSettings.RegistrationNumberKey] =
                            "GB123456789",
                        [TummlySellerVatSettings.EffectiveDateKey] =
                            "2024-01-01",
                        [TummlySellerVatSettings.LegalNameKey] = "Tummly Ltd",
                        [TummlySellerVatSettings.RegisteredAddressKey] =
                            "1 Example Street",
                        ["Revolut:SecretKey"] = "sk_test_placeholder",
                        ["Revolut:WebhookSigningSecret"] = "whsec_placeholder",
                        ["Revolut:ApiBaseUrl"] =
                            RevolutSettings.SandboxApiBaseUrl,
                        ["Revolut:ApiVersion"] =
                            RevolutSettings.DefaultApiVersion,
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.StarterMonthly}"
                        ] = "11111111-1111-1111-1111-111111111111",
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.StarterAnnual}"
                        ] = "22222222-2222-2222-2222-222222222222",
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GrowthMonthly}"
                        ] = "33333333-3333-3333-3333-333333333333",
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GrowthAnnual}"
                        ] = "44444444-4444-4444-4444-444444444444",
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupMonthly}"
                        ] = "55555555-5555-5555-5555-555555555555",
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupAnnual}"
                        ] = "66666666-6666-6666-6666-666666666666",
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupLocationMonthly}"
                        ] = "77777777-7777-7777-7777-777777777777",
                        [
                            $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupLocationAnnual}"
                        ] = "88888888-8888-8888-8888-888888888888",
                    }
                );
            });

            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d =>
                        d.ServiceType ==
                            typeof(DbContextOptions<ApplicationDbContext>)
                        || d.ServiceType == typeof(ApplicationDbContext)
                    )
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(w =>
                        w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                    );
                });
            });
        }
    }
}
