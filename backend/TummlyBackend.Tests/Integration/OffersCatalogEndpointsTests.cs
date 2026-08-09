using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class OffersCatalogEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersCatalogEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostOffer_CreatesActiveCatalogDefinition()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-catalog-create");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "percentage_discount",
                    title = "10% off next visit",
                    description = "Enjoy 10% off your next meal with us.",
                    validity = "30_days_after_issue",
                    discountPercentage = 10m,
                    staffInstructions = "Ask for the unique code.",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var offer = body.GetProperty("offer");
            Assert.True(offer.GetProperty("id").GetInt32() > 0);
            Assert.Equal(seeded.LocationId, offer.GetProperty("locationId").GetInt32());
            Assert.Equal("active", offer.GetProperty("status").GetString());
            Assert.Equal("percentage_discount", offer.GetProperty("offerType").GetString());
            Assert.Equal("10% off next visit", offer.GetProperty("title").GetString());
            Assert.Equal(10m, offer.GetProperty("discountPercentage").GetDecimal());
            Assert.False(offer.TryGetProperty("redemptionCode", out _));
        }

        [Fact]
        public async Task PostCampaign_AttachesOfferId_WithCreateNewOfferStance()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-attach-offer");
            var offerId = await CreateOfferAsync(seeded, "Attach me");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Thank with offer",
                    goalId = "thank-recent-guests",
                    audienceKey = "all-eligible-guests",
                    channel = "email",
                    offerStance = "create-new-offer",
                    offerId,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var campaign = (await ReadJsonAsync(response)).GetProperty("campaign");
            Assert.Equal("create-new-offer", campaign.GetProperty("offerStance").GetString());
            Assert.Equal(offerId, campaign.GetProperty("offerId").GetInt32());
        }

        [Fact]
        public async Task PatchCampaign_NoOffer_ClearsOfferId()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-clear-offer");
            var offerId = await CreateOfferAsync(seeded, "Clear me");

            using var createRequest = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Clear offer draft",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId,
                }
            );
            var createResponse = await _client.SendAsync(createRequest);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            var created = (await ReadJsonAsync(createResponse)).GetProperty("campaign");
            var id = created.GetProperty("id").GetInt32();
            var rowVersion = created.GetProperty("rowVersion").GetString();

            using var patchRequest = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new { rowVersion, offerStance = "no-offer" }
            );
            var patchResponse = await _client.SendAsync(patchRequest);
            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            var campaign = (await ReadJsonAsync(patchResponse)).GetProperty("campaign");
            Assert.Equal("no-offer", campaign.GetProperty("offerStance").GetString());
            Assert.Equal(JsonValueKind.Null, campaign.GetProperty("offerId").ValueKind);
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenOfferIdIsOrphan()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-orphan-offer");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Orphan offer",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId = 999_999,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Contains(
                "offerId",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private async Task<int> CreateOfferAsync(
            (string Jwt, int LocationId) seeded,
            string title
        )
        {
            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "fixed_discount",
                    title,
                    description = "A reusable campaign offer definition.",
                    validity = "14_days_after_issue",
                    discountAmount = 5m,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await ReadJsonAsync(response))
                .GetProperty("offer")
                .GetProperty("id")
                .GetInt32();
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
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
            int LocationId
        )> SeedOwnerWithLocationAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Offers Catalog Owner",
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
                Name = "Offers Catalog Venue",
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
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }
    }
}
