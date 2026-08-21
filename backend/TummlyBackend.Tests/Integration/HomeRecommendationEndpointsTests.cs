using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class HomeRecommendationEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomeRecommendationEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostRecommendation_WeakSignals_ReturnsNoneWithoutCallingProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync("home-rec-weak");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("review-open-feedback")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_HomeNativeSuccess_ReturnsAllowListedTypeAndAction()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync(
                "home-rec-success"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("review-open-feedback")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var recommendation = body.GetProperty("recommendation");
            Assert.Equal(
                "review-open-feedback",
                recommendation.GetProperty("type").GetString()
            );
            Assert.Equal(
                "Review open feedback",
                recommendation.GetProperty("title").GetString()
            );
            Assert.Equal("Main", recommendation.GetProperty("locationName").GetString());

            var action = recommendation.GetProperty("action");
            Assert.Equal("open-feedback", action.GetProperty("kind").GetString());

            Assert.Equal(1, fake.CallCount);
            Assert.NotNull(fake.LastInput);
            Assert.Equal("review-open-feedback", fake.LastInput!.SelectedType);
            var serialized = JsonSerializer.Serialize(fake.LastInput);
            Assert.DoesNotContain("Alex Guest", serialized);
            Assert.DoesNotContain("alex@example.com", serialized);
            Assert.DoesNotContain("Great food", serialized);
        }

        [Fact]
        public async Task PostRecommendation_ProviderFail_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync("home-rec-fail");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.Fail(retryable: true);

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_ProviderNoneAfterNativeSelect_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync(
                "home-rec-provider-none"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("none")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_CachesByOperatorLocationWindow_RefreshBypasses()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync("home-rec-cache");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("review-open-feedback")
            );

            using (var first = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            ))
            {
                var firstResponse = await _client.SendAsync(first);
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            }

            Assert.Equal(1, fake.CallCount);

            using (var second = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            ))
            {
                var secondResponse = await _client.SendAsync(second);
                Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            }

            Assert.Equal(1, fake.CallCount);

            var refreshBody = Last7Body(seeded.LocationId);
            refreshBody["refresh"] = true;
            using (var refresh = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                refreshBody
            ))
            {
                var refreshResponse = await _client.SendAsync(refresh);
                Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
            }

            Assert.Equal(2, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsync(
                "/api/home/recommendation",
                new StringContent("{}", Encoding.UTF8, "application/json")
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostRecommendation_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("home-rec-a");
            var other = await SeedOwnerWithLocationAsync("home-rec-b");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                owner.Jwt,
                Last7Body(other.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostRecommendation_FakeRegistered_NoLiveAzureRequired()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync("home-rec-fake");

            using var scope = _factory.Services.CreateScope();
            var provider = scope.ServiceProvider
                .GetRequiredService<IHomeRecommendationProvider>();
            Assert.IsType<FakeHomeRecommendationProvider>(provider);

            var fake = (FakeHomeRecommendationProvider)provider;
            fake.ResetCallCount();
            // Default fixtures per type — no SucceedWith required.
            fake.UseDefaultFixtures();

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "review-open-feedback",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(1, fake.CallCount);
        }

        private static Dictionary<string, object?> Last7Body(
            int locationId,
            string from = "2026-08-15T00:00:00.000Z",
            string to = "2026-08-21T15:30:00.000Z"
        )
        {
            return new Dictionary<string, object?>
            {
                ["locationId"] = locationId,
                ["overviewDatePreset"] = "last7",
                ["from"] = from,
                ["to"] = to,
                ["refresh"] = false,
            };
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
                FullName = "Home Rec Owner",
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
                Name = "Home Rec Venue",
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

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithOpenFeedbackAsync(string emailLocalPart)
        {
            var seeded = await SeedOwnerWithLocationAsync(emailLocalPart);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            // Inside the Last7Body window used by tests.
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    GuestName = "Alex Guest",
                    GuestContact = "alex@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Great food",
                    WorkflowStatus = FeedbackWorkflowStatus.New,
                    CreatedAt = DateTime.Parse("2026-08-18T12:00:00Z").ToUniversalTime(),
                }
            );
            await context.SaveChangesAsync();

            return seeded;
        }
    }
}
