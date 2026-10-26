using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Models;

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
        }

        [Fact]
        public async Task SaveOnboarding_InvalidLocations_ReturnsCode()
        {
            var sessionToken = await SeedVerifiedPendingAsync();

            var response = await _client.PostAsJsonAsync(
                $"/api/signup/onboarding?sessionToken={sessionToken}",
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
