using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>GET /api/home/weekly-brief</c> — auth, ownership, week default / explicit,
    /// ready vs missing envelope. Must not generate on GET.
    /// </summary>
    public class HomeWeeklyBriefEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string ExplicitWeek = "2026-W33";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomeWeeklyBriefEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_ReturnsBodyAndMetrics()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready");
            var metrics = EmptyMetrics() with { GuestsJoined = 4, FeedbackCount = 2 };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-08-18T09:00:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                metrics,
                generatedAt
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.ResetCallCount();

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.LocationId, json.GetProperty("locationId").GetInt32());
            Assert.Equal(ExplicitWeek, json.GetProperty("week").GetString());
            Assert.Equal(
                "succeeded",
                json.GetProperty("status").GetString()
            );
            Assert.Equal(
                "Steady week across capture and feedback.",
                json.GetProperty("body").GetProperty("headline").GetString()
            );
            Assert.Equal(
                4,
                json.GetProperty("metrics").GetProperty("guestsJoined").GetInt32()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task GetWeeklyBrief_MissingRow_ReturnsNotReadyEnvelope()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-missing");

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.LocationId, json.GetProperty("locationId").GetInt32());
            Assert.Equal(ExplicitWeek, json.GetProperty("week").GetString());
            Assert.False(json.TryGetProperty("body", out _));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetWeeklyBrief_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("wb-owner-a");
            var other = await SeedOwnerWithLocationAsync("wb-owner-b");

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={other.LocationId}&week={ExplicitWeek}",
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetWeeklyBrief_OmittingWeek_UsesClosedPriorWeekInLocationTz()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-default-week");
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                DateTime.UtcNow
            );
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics());

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                closed.WeekKey,
                body,
                EmptyMetrics(),
                DateTime.UtcNow
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
        }

        [Fact]
        public async Task GetWeeklyBrief_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                $"/api/home/weekly-brief?locationId=1&week={ExplicitWeek}"
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GenerateWeeklyBrief_MissingRow_CreatesReadyEnvelope()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-gen-create");
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                DateTime.UtcNow
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();
            fake.ResetCallCount();

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.LocationId, json.GetProperty("locationId").GetInt32());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
            Assert.Equal(
                "succeeded",
                json.GetProperty("status").GetString()
            );
            Assert.True(
                json.GetProperty("body").TryGetProperty("headline", out _)
            );
            Assert.Equal(1, fake.CallCount);

            using var getRequest = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var getJson = await ReadJsonAsync(getResponse);
            Assert.True(getJson.GetProperty("ready").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task GenerateWeeklyBrief_SecondCall_IsIdempotentWithoutReProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-gen-idem");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();
            fake.ResetCallCount();

            using var first = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var firstResponse = await _client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(1, fake.CallCount);

            using var second = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var secondResponse = await _client.SendAsync(second);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

            var json = await ReadJsonAsync(secondResponse);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task GenerateWeeklyBrief_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("wb-gen-owner-a");
            var other = await SeedOwnerWithLocationAsync("wb-gen-owner-b");

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={other.LocationId}",
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetWeeklyBrief_StillDoesNotGenerate()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-get-no-gen");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.ResetCallCount();

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(0, fake.CallCount);
        }

        private async Task SeedSucceededBriefAsync(
            int locationId,
            string weekKey,
            WeeklyBriefBody body,
            WeeklyBriefMetrics metrics,
            DateTime generatedAtUtc
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.WeeklyBriefs.Add(
                new WeeklyBrief
                {
                    LocationId = locationId,
                    WeekKey = weekKey,
                    Status = WeeklyBriefStatus.Succeeded,
                    GeneratedAtUtc = generatedAtUtc,
                    BodyJson = JsonSerializer.Serialize(body, WeeklyBriefStoreJson.Options),
                    MetricsJson = JsonSerializer.Serialize(metrics, WeeklyBriefStoreJson.Options),
                    ErrorInfo = null,
                }
            );
            await context.SaveChangesAsync();
        }

        private static WeeklyBriefMetrics EmptyMetrics()
            => new(
                GuestsJoined: 0,
                QrScanEvents: 0,
                FeedbackCount: 0,
                PositiveFeedbackCount: 0,
                NeutralFeedbackCount: 0,
                NegativeFeedbackCount: 0,
                NeedsAttentionCount: 0,
                DetectedTagCounts: new Dictionary<string, int>(),
                ActiveOffers: 0,
                ClaimsInWeek: 0,
                RedemptionsInWeek: 0,
                CampaignsSentInWeek: 0,
                CampaignRecipientsReached: 0
            );

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPost(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
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
                FullName = "Weekly Brief Owner",
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
                Name = "Weekly Brief Venue",
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
