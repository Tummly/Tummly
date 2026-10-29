using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Public product-demo visitors share one seeded owner. An Assistant thread
    /// is visible only to the demo session that created it (ENG07 07.1).
    /// </summary>
    public class DemoSessionAssistantIsolationTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string DemoOwnerEmail = "product-demo@example.com";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DemoSessionAssistantIsolationTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DemoThread_IsNotListedOrReadByAnotherDemoSession()
        {
            var sessionA = await CreateDemoTokenAsync();
            var sessionB = await CreateDemoTokenAsync();
            var conversationId = await SendTourTurnAsync(sessionA, conversationId: null);

            // Positive control: the creating session still sees its own thread.
            var ownGet = await GetAsync(sessionA, $"/api/assistant/conversations/{conversationId}");
            Assert.Equal(HttpStatusCode.OK, ownGet.StatusCode);
            var ownList = await ListIdsAsync(sessionA);
            Assert.Contains(conversationId, ownList);

            var otherGet = await GetAsync(sessionB, $"/api/assistant/conversations/{conversationId}");
            Assert.Equal(HttpStatusCode.NotFound, otherGet.StatusCode);

            var otherList = await ListIdsAsync(sessionB);
            Assert.DoesNotContain(conversationId, otherList);
        }

        [Fact]
        public async Task DemoSession_CannotContinueAnotherSessionsThread()
        {
            var sessionA = await CreateDemoTokenAsync();
            var sessionB = await CreateDemoTokenAsync();
            var conversationId = await SendTourTurnAsync(sessionA, conversationId: null);

            using var reply = await PostTourTurnAsync(sessionB, conversationId);

            Assert.Equal(HttpStatusCode.NotFound, reply.StatusCode);
            var ownGet = await GetAsync(sessionA, $"/api/assistant/conversations/{conversationId}");
            var body = await ownGet.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                2,
                body.GetProperty("conversation").GetProperty("messages").GetArrayLength()
            );
        }

        private async Task<int> SendTourTurnAsync(string token, int? conversationId)
        {
            using var response = await PostTourTurnAsync(token, conversationId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("conversation").GetProperty("id").GetInt32();
        }

        private Task<HttpResponseMessage> PostTourTurnAsync(string token, int? conversationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns")
            {
                Content = JsonContent.Create(new
                {
                    conversationId,
                    message = DemoSessionClaims.TourAssistantPrompt,
                    analysisScope = new
                    {
                        scopeKind = "all",
                        reportingPeriod = new { kind = "preset", presetId = "last30" },
                    },
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _client.SendAsync(request);
        }

        private Task<HttpResponseMessage> GetAsync(string token, string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _client.SendAsync(request);
        }

        private async Task<List<int>> ListIdsAsync(string token)
        {
            using var response = await GetAsync(token, "/api/assistant/conversations?archived=false");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("conversations")
                .EnumerateArray()
                .Select(row => row.GetProperty("id").GetInt32())
                .ToList();
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
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (context.Users.Any(u => u.Email == DemoOwnerEmail))
            {
                return;
            }

            var user = new User
            {
                Email = DemoOwnerEmail,
                FullName = "Product Demo Owner",
                PhoneNumber = "5551234503",
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
