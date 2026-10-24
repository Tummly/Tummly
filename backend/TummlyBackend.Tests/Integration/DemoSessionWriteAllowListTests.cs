using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Public product-demo JWT: writes are default-deny on the server (ENG07 07.1).
    /// Only the explicit allow-list runs; overlay writes are simulated.
    /// </summary>
    public class DemoSessionWriteAllowListTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string DemoOwnerEmail = "product-demo@example.com";
        private const string DemoDisabledMessage =
            "This action is disabled in the public product demo.";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DemoSessionWriteAllowListTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public static TheoryData<string, string> DeniedWrites =>
            new()
            {
                { "DELETE", "/api/guests/1" },
                { "POST", "/api/account/change-password" },
                { "PATCH", "/api/team-permissions/members/1/role" },
                { "POST", "/api/account-workspace/pause" },
                { "POST", "/api/campaigns/1/pause" },
                { "POST", "/api/billing-credits/top-up" },
                { "POST", "/api/shop/orders" },
                { "POST", "/api/assistant/stt" },
            };

        [Theory]
        [MemberData(nameof(DeniedWrites))]
        public async Task DemoJwt_WriteOutsideAllowList_Returns403(
            string method,
            string path
        )
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(new HttpMethod(method), path)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(DemoDisabledMessage, body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task DemoJwt_NotificationReadAll_IsSimulated()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/notifications/read-all"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("demoSessionIsolated").GetBoolean());
        }

        [Fact]
        public async Task DemoJwt_Logout_IsAllowed()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DemoJwt_AssistantTurn_WithOtherPrompt_Returns403()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns")
            {
                Content = JsonContent.Create(
                    new { message = "Write me a poem about the other visitors." }
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DemoJwt_AssistantTurn_WithTourPrompt_IsNotBlocked()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns")
            {
                Content = JsonContent.Create(
                    new { message = DemoSessionClaims.TourAssistantPrompt }
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<string> CreateDemoTokenAsync()
        {
            await EnsureDemoOwnerAsync();

            var response = await _client.PostAsync("/api/auth/demo-session", content: null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("data").GetProperty("token").GetString()!;
        }

        private async Task EnsureDemoOwnerAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (context.Users.Any(u => u.Email == DemoOwnerEmail))
            {
                return;
            }

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
    }
}
