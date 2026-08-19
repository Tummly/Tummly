using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignEligibilityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignEligibilityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetEligibility_ReturnsLiveCounts_ForOwnedLocation()
        {
            var seeded = await SeedOwnerWithGuestsAsync(
                "campaign-eligibility-live"
            );

            using var request = AuthorizedGet(
                EligibilityUrl(seeded.LocationId, "all-eligible-guests"),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var eligibility = body.GetProperty("eligibility");
            Assert.Equal(
                "all-eligible-guests",
                eligibility.GetProperty("audienceKey").GetString()
            );
            Assert.True(eligibility.GetProperty("evaluable").GetBoolean());
            Assert.Equal(3, eligibility.GetProperty("matched").GetInt32());
            Assert.Equal(2, eligibility.GetProperty("currentlyEligible").GetInt32());
            Assert.Equal(1, eligibility.GetProperty("excluded").GetInt32());
            Assert.Equal(2, eligibility.GetProperty("emailEligible").GetInt32());
            Assert.Equal(1, eligibility.GetProperty("smsEligible").GetInt32());
            Assert.Equal(
                CampaignEligibilityService.CheckSetVersion,
                eligibility.GetProperty("checkSetVersion").GetString()
            );

            var reasons = eligibility.GetProperty("excludedReasons");
            Assert.Equal(1, reasons.GetArrayLength());
            Assert.Equal(
                "opt-out",
                reasons[0].GetProperty("reason").GetString()
            );
            Assert.Equal(1, reasons[0].GetProperty("count").GetInt32());
        }

        [Fact]
        public async Task GetEligibility_UnevaluableAudience_ReturnsNullCounts()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-eligibility-unevaluable"
            );

            using var request = AuthorizedGet(
                EligibilityUrl(seeded.LocationId, "no-recent-tummly-activity"),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var eligibility = body.GetProperty("eligibility");
            Assert.False(eligibility.GetProperty("evaluable").GetBoolean());
            Assert.Equal(JsonValueKind.Null, eligibility.GetProperty("matched").ValueKind);
            Assert.Equal(
                JsonValueKind.Null,
                eligibility.GetProperty("currentlyEligible").ValueKind
            );
            Assert.Equal(0, eligibility.GetProperty("excludedReasons").GetArrayLength());
        }

        [Fact]
        public async Task GetEligibility_ReturnsForbidden_ForForeignLocation()
        {
            var owner = await SeedOwnerWithLocationAsync(
                "campaign-eligibility-owner"
            );
            var other = await SeedOwnerWithLocationAsync(
                "campaign-eligibility-other"
            );

            using var request = AuthorizedGet(
                EligibilityUrl(other.LocationId, "all-eligible-guests"),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEligibility_ReturnsBadRequest_ForUnknownAudience()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-eligibility-bad-key"
            );

            using var request = AuthorizedGet(
                EligibilityUrl(seeded.LocationId, "not-real"),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static string EligibilityUrl(int locationId, string audienceKey)
        {
            return $"/api/campaigns/eligibility?locationId={locationId}&audienceKey={Uri.EscapeDataString(audienceKey)}";
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId
        )> SeedOwnerWithLocationAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Eligibility Owner",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Eligibility Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithGuestsAsync(string emailLocalPart)
        {
            var seeded = await SeedOwnerWithLocationAsync(emailLocalPart);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            async Task AddGuestAsync(
                string name,
                string? email,
                string? mobile,
                bool offersOptOut
            )
            {
                var master = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = email,
                    NormalizedEmail = email?.Trim().ToLowerInvariant(),
                    Mobile = mobile,
                    NormalizedPhone = mobile,
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                context.LocationGuests.Add(
                    new LocationGuest
                    {
                        MasterGuestId = master.Id,
                        RestaurantLocationId = seeded.LocationId,
                        Name = name,
                        MarketingPreference = LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(offersOptOut),

                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            await AddGuestAsync(
                "Eligible Both",
                "both@example.com",
                "07700900111",
                offersOptOut: false
            );
            await AddGuestAsync(
                "Eligible Email",
                "email@example.com",
                null,
                offersOptOut: false
            );
            await AddGuestAsync(
                "Opted Out",
                "out@example.com",
                "07700900222",
                offersOptOut: true
            );

            return (seeded.Jwt, seeded.LocationId);
        }
    }
}
