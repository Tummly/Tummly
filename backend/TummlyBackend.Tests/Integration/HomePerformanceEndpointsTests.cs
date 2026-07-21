using System.Globalization;
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
    public class HomePerformanceEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomePerformanceEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetHomePerformance_ReturnsFeedbackSubmitted_InHalfOpenWindow()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithFeedbackAsync(
                "home-perf-count-token-1234567890",
                feedbackCreatedAt: new DateTime(
                    2026,
                    7,
                    14,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                extraFeedbackCreatedAt: new DateTime(
                    2026,
                    7,
                    17,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                )
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                1,
                body.GetProperty("feedbackSubmitted").GetInt32()
            );
        }

        [Fact]
        public async Task GetHomePerformance_DoesNotChangeFeedbackListTotal()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithFeedbackAsync(
                "home-perf-list-total-token-123456",
                feedbackCreatedAt: new DateTime(
                    2026,
                    7,
                    14,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                extraFeedbackCreatedAt: new DateTime(
                    2026,
                    7,
                    17,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                )
            );

            using var performanceRequest = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            performanceRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var performanceResponse = await _client.SendAsync(performanceRequest);
            Assert.Equal(HttpStatusCode.OK, performanceResponse.StatusCode);

            var performanceBody = await ReadJsonAsync(performanceResponse);
            Assert.Equal(
                1,
                performanceBody.GetProperty("feedbackSubmitted").GetInt32()
            );

            using var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback?locationId={seeded.LocationId}"
            );
            listRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var listResponse = await _client.SendAsync(listRequest);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            var listBody = await ReadJsonAsync(listResponse);
            Assert.True(listBody.GetProperty("success").GetBoolean());
            Assert.Equal(2, listBody.GetProperty("total").GetInt32());
        }

        [Fact]
        public async Task GetHomePerformance_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(
                PerformanceUrl(1, from, to)
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetHomePerformance_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerWithFeedbackAsync(
                "home-perf-owner-a-token-1234567"
            );
            var other = await SeedOwnerWithFeedbackAsync(
                "home-perf-owner-b-token-1234567",
                email: "home-perf-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(other.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetHomePerformance_Returns400_WhenFromMissing()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "home-perf-missing-from-token-12"
            );
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/home/performance?locationId={seeded.LocationId}&to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetHomePerformance_Returns400_WhenFromNotBeforeTo()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "home-perf-from-gte-to-token-123"
            );
            var instant = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, instant, instant)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetHomePerformance_Returns400_WhenSpanExceeds180Days()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "home-perf-span-max-token-12345"
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(181);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetHomePerformance_AllowsExact180DaySpan()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "home-perf-span-ok-token-123456"
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(180);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                0,
                body.GetProperty("feedbackSubmitted").GetInt32()
            );
        }

        private static string PerformanceUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/home/performance?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string FormatUtc(DateTime value)
        {
            return value
                .ToUniversalTime()
                .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            string email = "home-perf-owner@example.com",
            DateTime? feedbackCreatedAt = null,
            DateTime? extraFeedbackCreatedAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Home Perf Owner",
                Email = email,
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
                Name = "Home Perf Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = linkToken,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Alex Guest",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = feedbackCreatedAt ?? DateTime.UtcNow,
            };

            context.Feedbacks.Add(feedback);

            if (extraFeedbackCreatedAt != null)
            {
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = location.Id,
                        GuestName = "Outside Window",
                        GuestContact = "out@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Too late",
                        CreatedAt = extraFeedbackCreatedAt.Value,
                    }
                );
            }

            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, feedback.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
