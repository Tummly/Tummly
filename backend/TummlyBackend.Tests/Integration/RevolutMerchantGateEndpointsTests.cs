using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class RevolutMerchantGateEndpointsTests
    {
        [Fact]
        public async Task PostPlanChange_Pay_Returns503_VatNotReady_WhenConfigEmpty()
        {
            await using var factory = new RevolutGateWebApplicationFactory();
            var client = factory.CreateClient();
            var seeded = await SeedPilotAsync(factory);

            using var request = AuthorizedPlanChange(
                seeded.OwnerJwt,
                "Starter",
                "monthly",
                withIdempotency: true
            );
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                RevolutMerchantCreateGate.VatNotReady,
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task PostPlanChange_Pay_Returns503_RevolutNotReady_WhenVatOnly()
        {
            await using var factory = new RevolutGateWebApplicationFactory(
                RevolutGateConfigMode.VatOnly
            );
            var client = factory.CreateClient();
            var seeded = await SeedPilotAsync(factory);

            using var request = AuthorizedPlanChange(
                seeded.OwnerJwt,
                "Starter",
                "monthly",
                withIdempotency: true
            );
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                RevolutMerchantCreateGate.RevolutNotReady,
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task PostPlanChange_Pay_Returns503_PlanVariationMissing_WhenMapLacksTarget()
        {
            await using var factory = new RevolutGateWebApplicationFactory(
                RevolutGateConfigMode.VatAndRevolutNoVariations
            );
            var client = factory.CreateClient();
            var seeded = await SeedPilotAsync(factory);

            using var request = AuthorizedPlanChange(
                seeded.OwnerJwt,
                "Starter",
                "monthly",
                withIdempotency: true
            );
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                RevolutMerchantCreateGate.PlanVariationMissing,
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task GetBillingCredits_PilotPath_NotBlocked_WhenRevolutConfigEmpty()
        {
            await using var factory = new RevolutGateWebApplicationFactory();
            var client = factory.CreateClient();
            var seeded = await SeedPilotAsync(factory);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/billing-credits"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(
                body.GetProperty("planSubscription").GetProperty("isPilot").GetBoolean()
            );
        }

        [Fact]
        public async Task PostPlanChange_Pay_ReturnsRedirect_WhenAPlusBPlusCReady()
        {
            await using var factory = new RevolutGateWebApplicationFactory(
                RevolutGateConfigMode.FullReady
            );
            var client = factory.CreateClient();
            var seeded = await SeedPilotAsync(factory);

            using var request = AuthorizedPlanChange(
                seeded.OwnerJwt,
                "Starter",
                "monthly",
                withIdempotency: true
            );
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("pay", body.GetProperty("outcome").GetString());
            Assert.Equal(
                FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
                body.GetProperty("redirectUrl").GetString()
            );
        }

        private static HttpRequestMessage AuthorizedPlanChange(
            string jwt,
            string targetPlan,
            string targetCadence,
            bool withIdempotency
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/billing-credits/plan-change"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            if (withIdempotency)
            {
                request.Headers.Add(
                    "Idempotency-Key",
                    Guid.NewGuid().ToString("D")
                );
            }

            request.Content = JsonContent.Create(new
            {
                targetPlan,
                targetCadence,
            });
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private static async Task<(string OwnerJwt, int RestaurantId)> SeedPilotAsync(
            RevolutGateWebApplicationFactory factory
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Gate Owner",
                Email = $"gate-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Gate Cafe",
                OwnerUserId = owner.Id,
                AccountType = "Single",
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            owner.SelectedRestaurantId = restaurant.Id;

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    Status = MembershipStatus.Active,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                }
            );
            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            await context.SaveChangesAsync();

            var token = jwt.GenerateToken(
                owner.Id.ToString(),
                owner.Email,
                owner.Role
            );
            return (token, restaurant.Id);
        }
    }

    internal enum RevolutGateConfigMode
    {
        Empty,
        VatOnly,
        VatAndRevolutNoVariations,
        FullReady,
    }

    internal sealed class RevolutGateWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly RevolutGateConfigMode _mode;

        public RevolutGateWebApplicationFactory(
            RevolutGateConfigMode mode = RevolutGateConfigMode.Empty
        )
        {
            _mode = mode;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(BuildConfig(_mode));
            });

            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d =>
                        d.ServiceType
                            == typeof(DbContextOptions<ApplicationDbContext>)
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

                if (_mode == RevolutGateConfigMode.FullReady)
                {
                    services.RemoveAll<IRevolutMerchantClient>();
                    services.AddSingleton<IRevolutMerchantClient>(
                        new FakeFirstPaidRevolutMerchantClient()
                    );
                }
            });
        }

        private static Dictionary<string, string?> BuildConfig(
            RevolutGateConfigMode mode
        )
        {
            var values = new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "https://tummly.example",
            };
            if (mode == RevolutGateConfigMode.Empty)
            {
                return values;
            }

            values[TummlySellerVatSettings.RegistrationNumberKey] = "GB123456789";
            values[TummlySellerVatSettings.EffectiveDateKey] = "2024-01-01";
            values[TummlySellerVatSettings.LegalNameKey] = "Tummly Ltd";
            values[TummlySellerVatSettings.RegisteredAddressKey] =
                "1 Example Street";

            if (mode == RevolutGateConfigMode.VatOnly)
            {
                return values;
            }

            values["Revolut:SecretKey"] = "sk_test_placeholder";
            values["Revolut:WebhookSigningSecret"] = "whsec_placeholder";
            values["Revolut:ApiBaseUrl"] = RevolutSettings.SandboxApiBaseUrl;
            values["Revolut:ApiVersion"] = RevolutSettings.DefaultApiVersion;

            if (mode == RevolutGateConfigMode.VatAndRevolutNoVariations)
            {
                return values;
            }

            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.StarterMonthly}"
            ] = "11111111-1111-1111-1111-111111111111";
            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.StarterAnnual}"
            ] = "22222222-2222-2222-2222-222222222222";
            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GrowthMonthly}"
            ] = "33333333-3333-3333-3333-333333333333";
            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GrowthAnnual}"
            ] = "44444444-4444-4444-4444-444444444444";
            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupMonthly}"
            ] = "55555555-5555-5555-5555-555555555555";
            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupAnnual}"
            ] = "66666666-6666-6666-6666-666666666666";
            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupLocationMonthly}"
            ] = "77777777-7777-7777-7777-777777777777";
            values[
                $"Revolut:PlanVariations:{RevolutPlanVariationKeys.GroupLocationAnnual}"
            ] = "88888888-8888-8888-8888-888888888888";

            return values;
        }
    }
}
