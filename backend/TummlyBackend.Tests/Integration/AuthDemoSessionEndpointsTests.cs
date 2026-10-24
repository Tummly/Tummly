using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AuthDemoSessionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string DemoOwnerEmail = "product-demo@example.com";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthDemoSessionEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DemoSession_ReturnsToken_AndMeSucceeds()
        {
            await SeedDemoOwnerAsync();

            var response = await _client.PostAsync(
                "/api/auth/demo-session",
                content: null
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var data = body.GetProperty("data");
            var accessToken = data.GetProperty("token").GetString();
            var accountType = data.GetProperty("accountType").GetString();
            // Demo sessions get no refresh token (a refreshed JWT would lose
            // the demo claims and escape the sandbox).
            var refreshToken = data.TryGetProperty("refreshToken", out var refresh)
                ? refresh.GetString()
                : null;

            Assert.False(string.IsNullOrWhiteSpace(accessToken));
            Assert.Equal("Single", accountType);
            Assert.True(string.IsNullOrWhiteSpace(refreshToken));

            using var meRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/me"
            );
            meRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var meResponse = await _client.SendAsync(meRequest);

            Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        }

        [Fact]
        public async Task DemoSession_Returns404_WhenOwnerMissing()
        {
            await using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["DemoSession:Enabled"] = "true",
                            ["DemoSession:OwnerEmail"] =
                                "missing-demo-owner@example.com",
                        }
                    );
                });
            });
            var client = factory.CreateClient();

            var response = await client.PostAsync(
                "/api/auth/demo-session",
                content: null
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DemoSession_Returns503_WhenDisabled()
        {
            await using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["DemoSession:Enabled"] = "false",
                        }
                    );
                });
            });
            var client = factory.CreateClient();

            var response = await client.PostAsync(
                "/api/auth/demo-session",
                content: null
            );

            Assert.Equal(
                HttpStatusCode.ServiceUnavailable,
                response.StatusCode
            );
        }

        private async Task SeedDemoOwnerAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new User
            {
                Email = DemoOwnerEmail,
                FullName = "Product Demo Owner",
                PhoneNumber = "5551234500",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Demo Kitchen",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            user.SelectedRestaurantId = restaurant.Id;

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Camden",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            await context.SaveChangesAsync();
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
