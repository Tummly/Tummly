using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignsListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignsListEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("all")]
        [InlineData("needs-attention")]
        [InlineData("drafts")]
        [InlineData("in-flight")]
        [InlineData("sent")]
        public async Task GetCampaigns_EmptyViews_ReturnSuccessEmptyEnvelope(
            string view
        )
        {
            var seeded = await SeedOwnerWithLocationAsync(
                $"campaigns-list-{view}"
            );

            using var request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, view),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());

            var tabCounts = body.GetProperty("tabCounts");
            Assert.Equal(0, tabCounts.GetProperty("all").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("drafts").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("needsAttention").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("inFlight").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("sent").GetInt32());
        }

        [Fact]
        public async Task GetCampaigns_RejectsAwaitingApprovalView()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaigns-list-awaiting"
            );

            using var request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "awaiting-approval"),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaigns_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/campaigns?locationId=1&view=all"
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaigns_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("campaigns-list-a");
            var other = await SeedOwnerWithLocationAsync("campaigns-list-b");

            using var request = AuthorizedGet(
                CampaignsUrl(other.LocationId, "all"),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private static HttpRequestMessage AuthorizedGet(
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static string CampaignsUrl(int locationId, string view)
        {
            return $"/api/campaigns?locationId={locationId}&view={view}";
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
                FullName = "Campaigns Owner",
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
                Name = "Campaigns Venue",
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
