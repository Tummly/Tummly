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
    public class FeedbackSummaryEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackSummaryEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetFeedbackSummary_CountsTotalAndSentimentBuckets_InRange()
        {
            // Current [Jul 10, Jul 17); previous equal span [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-summary-kpi-tok"
            );

            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Neutral
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Pending,
                sentiment: null
            );
            // Outside current window (previous period) — must not inflate Total.
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive
            );
            // At `to` boundary — half-open, excluded.
            await AddFeedbackAsync(
                seeded.LocationId,
                to,
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SummaryUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(4, body.GetProperty("total").GetInt32());
            Assert.Equal(1, body.GetProperty("positive").GetInt32());
            Assert.Equal(1, body.GetProperty("neutral").GetInt32());
            Assert.Equal(1, body.GetProperty("negative").GetInt32());
            Assert.Equal(1, body.GetProperty("totalPrevious").GetInt32());
            Assert.Equal(1, body.GetProperty("positivePrevious").GetInt32());
            Assert.Equal(0, body.GetProperty("neutralPrevious").GetInt32());
            Assert.Equal(0, body.GetProperty("negativePrevious").GetInt32());
        }

        [Fact]
        public async Task GetFeedbackSummary_UnclassifiedOnly_ShowsZerosNotEmptyPayload()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-summary-unclassified"
            );

            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Pending,
                sentiment: null
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Failed,
                sentiment: null
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SummaryUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, body.GetProperty("total").GetInt32());
            Assert.Equal(0, body.GetProperty("positive").GetInt32());
            Assert.Equal(0, body.GetProperty("neutral").GetInt32());
            Assert.Equal(0, body.GetProperty("negative").GetInt32());
            Assert.Equal(0, body.GetProperty("needsAttentionTotal").GetInt32());
        }

        [Fact]
        public async Task GetFeedbackSummary_EmptyRange_ReturnsZeroTotals()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-summary-empty"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SummaryUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, body.GetProperty("total").GetInt32());
            Assert.Equal(0, body.GetProperty("positive").GetInt32());
            Assert.Equal(0, body.GetProperty("neutral").GetInt32());
            Assert.Equal(0, body.GetProperty("negative").GetInt32());
            Assert.Equal(0, body.GetProperty("totalPrevious").GetInt32());
            Assert.Equal(0, body.GetProperty("needsAttentionTotal").GetInt32());
        }

        [Fact]
        public async Task GetFeedbackSummary_NeedsAttention_CountsNegativeUnresolvedInRange()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-summary-needs"
            );

            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.InProgress
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.Resolved
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                FeedbackWorkflowStatus.New
            );
            // Outside range — must not count.
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc),
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SummaryUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, body.GetProperty("needsAttentionTotal").GetInt32());
            Assert.Equal(3, body.GetProperty("negative").GetInt32());
        }

        [Fact]
        public async Task GetFeedbackSummary_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SummaryUrl(1, from, to)
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackSummary_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerWithLocationAsync(
                "feedback-summary-owner-a"
            );
            var other = await SeedOwnerWithLocationAsync(
                "feedback-summary-owner-b"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SummaryUrl(other.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackSummary_Returns400_WhenFromMissing()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-summary-bad-from"
            );
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var toIso = to.ToString("o", CultureInfo.InvariantCulture);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/summary?locationId={seeded.LocationId}&to={Uri.EscapeDataString(toIso)}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private static string SummaryUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            var fromIso = from.ToString(
                "o",
                CultureInfo.InvariantCulture
            );
            var toIso = to.ToString("o", CultureInfo.InvariantCulture);
            return $"/api/feedback/summary?locationId={locationId}&from={Uri.EscapeDataString(fromIso)}&to={Uri.EscapeDataString(toIso)}";
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
                FullName = "Summary Owner",
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
                Name = "Summary Venue",
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

        private async Task AddFeedbackAsync(
            int locationId,
            DateTime createdAt,
            ClassificationStatus classificationStatus,
            FeedbackSentiment? sentiment,
            FeedbackWorkflowStatus workflowStatus = FeedbackWorkflowStatus.New
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.Feedbacks.Add(new Feedback
            {
                RestaurantLocationId = locationId,
                GuestName = "Alex Guest",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Comment",
                CreatedAt = createdAt,
                ClassificationStatus = classificationStatus,
                Sentiment = sentiment,
                DetectedTagsJson =
                    classificationStatus == ClassificationStatus.Succeeded
                        ? "[]"
                        : null,
                WorkflowStatus = workflowStatus,
            });
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
