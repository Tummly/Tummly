using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class SignupOnboardingCustomPlanTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SignupOnboardingCustomPlanTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.CommercialCatalogVersions.Any(v => v.Status == CommercialCatalogStatuses.Published))
            {
                var json = PricebookCatalog.LoadFromDirectory(
                    TestPricebookPaths.PackDirectory()
                );
                var snapshot = json.GetRequired(json.CurrentPricebookId);
                db.CommercialCatalogVersions.Add(
                    CommercialCatalogSeeder.FromSnapshot(snapshot, DateTime.UtcNow)
                );
                db.SaveChanges();
            }
        }

        [Fact]
        public async Task SaveOnboarding_StaleQuoteHash_Returns400QuoteChanged()
        {
            var sessionToken = await SeedVerifiedPendingAsync();

            var response = await PostOnboardingAsync(
                sessionToken,
                new
                {
                    password = "Password1!",
                    confirmPassword = "Password1!",
                    fullName = "Pilot Owner",
                    groupName = "Pilot Kitchen",
                    businessCategory = "takeaway",
                    primaryPhone = "07911123456",
                    chosenPlan = BillingSubscriptionPlans.Custom,
                    chosenCadence = "monthly",
                    locations = new[]
                    {
                        new
                        {
                            locationName = "Main",
                            address = "1 High Street",
                            city = "Leeds",
                            postcode = "LS1 1AA",
                        },
                    },
                    customPlanConfigJson = JsonSerializer.Serialize(
                        new
                        {
                            version = 2,
                            cadence = "monthly",
                            basePlan = "group",
                            locations = 31,
                            packs = new Dictionary<string, int>(),
                            quoteHash = new string('0', 32),
                        }
                    ),
                }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(
                "quote_changed",
                body.RootElement.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task SaveOnboarding_InvalidLocations_ReturnsCode()
        {
            var sessionToken = await SeedVerifiedPendingAsync();

            var response = await PostOnboardingAsync(
                sessionToken,
                new
                {
                    password = "Password1!",
                    confirmPassword = "Password1!",
                    fullName = "Pilot Owner",
                    groupName = "Pilot Kitchen",
                    businessCategory = "takeaway",
                    primaryPhone = "07911123456",
                    chosenPlan = BillingSubscriptionPlans.Custom,
                    chosenCadence = "monthly",
                    locations = new[]
                    {
                        new
                        {
                            locationName = "Main",
                            address = "1 High Street",
                            city = "Leeds",
                            postcode = "LS1 1AA",
                        },
                    },
                    customPlanConfigJson = JsonSerializer.Serialize(
                        new
                        {
                            version = 2,
                            cadence = "monthly",
                            basePlan = "growth",
                            locations = 0,
                            packs = new Dictionary<string, int>(),
                        }
                    ),
                }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(
                "invalid_location_count",
                body.RootElement.GetProperty("code").GetString()
            );
            Assert.Equal(
                "Enter a whole number of Locations.",
                body.RootElement.GetProperty("message").GetString()
            );
        }

        private async Task<HttpResponseMessage> PostOnboardingAsync(
            Guid sessionToken,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/signup/onboarding")
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Add("X-Signup-Session", sessionToken.ToString());
            return await _client.SendAsync(request);
        }

        private async Task<Guid> SeedVerifiedPendingAsync()
        {
            var sessionToken = Guid.NewGuid();
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = $"signup-{Guid.NewGuid():N}@example.com",
                    Status = PendingSignupStatuses.Verified,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await db.SaveChangesAsync();
            return sessionToken;
        }
    }
}
