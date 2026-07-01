using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class SmartGuestLinkEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SmartGuestLinkEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetScan_ReturnsLocationMetadata_ForValidToken()
        {
            const string token = "guest-token-123456789012345678";

            await SeedGuestLocationAsync(
                token,
                restaurantName: "The Golden Fork",
                locationName: "Main"
            );

            var response = await _client.GetAsync($"/api/scan/{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "The Golden Fork",
                body.GetProperty("restaurantName").GetString()
            );
            Assert.Equal(
                "Main",
                body.GetProperty("locationName").GetString()
            );
        }

        [Fact]
        public async Task GetScan_Returns404_ForUnknownToken()
        {
            var response = await _client.GetAsync(
                "/api/scan/missing-token"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Link not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetLocations_ReturnsGuestUrl_ForAuthenticatedOwner()
        {
            const string linkToken = "owner-location-token1234567890";

            var jwt = await SeedOwnerLocationAsync(linkToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var locations = body.GetProperty("locations");
            Assert.Equal(JsonValueKind.Array, locations.ValueKind);
            Assert.Equal(1, locations.GetArrayLength());

            var location = locations[0];
            Assert.Equal(
                $"https://tummly.example/scan/{linkToken}",
                location.GetProperty("guestUrl").GetString()
            );
            Assert.False(
                location.TryGetProperty("linkToken", out _)
            );
        }

        [Fact]
        public async Task GetLocations_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/restaurant/locations"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task SeedGuestLocationAsync(
            string linkToken,
            string restaurantName,
            string locationName
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 999_999,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LinkToken = linkToken,
                    LocationName = locationName,
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            await context.SaveChangesAsync();
        }

        private async Task<string> SeedOwnerLocationAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Alex Owner",
                Email = "owner@example.com",
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
                Name = "The Golden Fork",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LinkToken = linkToken,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
