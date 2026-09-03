using System.Globalization;
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
    /// Seam: <c>GET /api/reports/feedback</c> — auth, aggregates, lifetime empty,
    /// Soft-lock read-allowed.
    /// </summary>
    public class ReportsFeedbackEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReportsFeedbackEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetFeedback_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(FeedbackUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedback_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerAsync("reports-fb-owner-a");
            var other = await SeedOwnerAsync("reports-fb-owner-b");

            using var request = AuthorizedGet(
                FeedbackUrl(other.LocationId, from, to),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedback_ReturnsLifetimeEmpty_WhenNeverHadFeedback()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-fb-empty");

            using var request = AuthorizedGet(
                FeedbackUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.False(body.TryGetProperty("kpis", out _));
        }

        [Fact]
        public async Task GetFeedback_ReturnsAggregates_WithNeedsAttentionCapAndBySource()
        {
            // Current [Jul 10, Jul 17); previous [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithFeedbackReportFactsAsync(
                "reports-fb-agg"
            );

            using var request = AuthorizedGet(
                FeedbackUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());

            var feedbackReceived = body
                .GetProperty("kpis")
                .GetProperty("feedbackReceived");
            // Happy + Resolved + 5 NA + 1 extra NA = 8
            Assert.Equal(8, feedbackReceived.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                feedbackReceived.GetProperty("valuePrevious").GetInt32()
            );

            // Happy + Needs1-4 + Extra (Needs0 + Resolved opt-out)
            Assert.Equal(
                6,
                body
                    .GetProperty("kpis")
                    .GetProperty("marketingOptIns")
                    .GetProperty("value")
                    .GetInt32()
            );
            Assert.Equal(
                6,
                body
                    .GetProperty("kpis")
                    .GetProperty("followUpNeeded")
                    .GetProperty("value")
                    .GetInt32()
            );
            Assert.Equal(
                1,
                body
                    .GetProperty("kpis")
                    .GetProperty("resolved")
                    .GetProperty("value")
                    .GetInt32()
            );

            Assert.Equal(
                1,
                body
                    .GetProperty("status")
                    .GetProperty("new")
                    .GetProperty("value")
                    .GetInt32()
            );
            Assert.Equal(
                6,
                body
                    .GetProperty("status")
                    .GetProperty("inProgress")
                    .GetProperty("value")
                    .GetInt32()
            );

            var needsAttention = body.GetProperty("needsAttention");
            Assert.Equal(JsonValueKind.Array, needsAttention.ValueKind);
            Assert.Equal(5, needsAttention.GetArrayLength());
            Assert.True(
                needsAttention[0].TryGetProperty("feedbackId", out _)
            );
            Assert.Equal(
                "In progress",
                needsAttention[0].GetProperty("workflowStatus").GetString()
            );

            var bySource = body.GetProperty("bySource");
            Assert.Equal(JsonValueKind.Array, bySource.ValueKind);
            Assert.True(bySource.GetArrayLength() >= 1);
            Assert.Equal(
                "Counter card",
                bySource[0].GetProperty("source").GetString()
            );
            Assert.Equal(
                8,
                bySource[0].GetProperty("feedback").GetInt32()
            );
        }

        [Fact]
        public async Task GetFeedback_Returns200_WhenSoftLocked()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithOneFeedbackAsync(
                "reports-fb-softlock",
                softLock: true
            );

            using var request = AuthorizedGet(
                FeedbackUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.Equal(
                1,
                body
                    .GetProperty("kpis")
                    .GetProperty("feedbackReceived")
                    .GetProperty("value")
                    .GetInt32()
            );
        }

        private static string FeedbackUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/reports/feedback?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string FormatUtc(DateTime value)
        {
            return value
                .ToUniversalTime()
                .ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                    CultureInfo.InvariantCulture
                );
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
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

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string emailLocalPart,
            bool softLock = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Reports Feedback Owner",
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
                Name = "Reports Feedback Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            if (softLock)
            {
                billing.BillingStatus = BillingStatuses.SoftLock;
                billing.SoftLockEnteredAt = DateTime.UtcNow.AddDays(-1);
            }

            context.BillingAccounts.Add(billing);

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
        )> SeedOwnerWithOneFeedbackAsync(
            string emailLocalPart,
            bool softLock = false
        )
        {
            var seeded = await SeedOwnerAsync(emailLocalPart, softLock);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var qr = new QrCode
            {
                RestaurantLocationId = seeded.LocationId,
                QrType = QrType.CounterCard,
                Status = QrCodeStatus.Active,
                Token = $"{emailLocalPart}-qr-token-1234567890",
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qr);
            await context.SaveChangesAsync();

            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = qr.Id,
                    GuestName = "Soft Lock Guest",
                    GuestContact = "soft@example.com",
                    ContactType = ContactType.Email,
                    Comment = "In window",
                    OffersOptOut = false,
                    CreatedAt = new DateTime(
                        2026,
                        7,
                        14,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                }
            );
            await context.SaveChangesAsync();
            return seeded;
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithFeedbackReportFactsAsync(string emailLocalPart)
        {
            var seeded = await SeedOwnerAsync(emailLocalPart);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var qr = new QrCode
            {
                RestaurantLocationId = seeded.LocationId,
                QrType = QrType.CounterCard,
                Status = QrCodeStatus.Active,
                Token = $"{emailLocalPart}-qr-token-1234567890",
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qr);
            await context.SaveChangesAsync();

            // Previous window: 1 feedback
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = qr.Id,
                    GuestName = "Previous Guest",
                    GuestContact = "previous@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Previous window",
                    OffersOptOut = false,
                    CreatedAt = new DateTime(
                        2026,
                        7,
                        5,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                }
            );

            // Current: 1 New (positive — not needs-attention)
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = qr.Id,
                    GuestName = "Happy Guest",
                    GuestContact = "happy@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Great meal",
                    OffersOptOut = false,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Positive,
                    WorkflowStatus = FeedbackWorkflowStatus.New,
                    CreatedAt = new DateTime(
                        2026,
                        7,
                        11,
                        10,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                }
            );

            // Current: 1 Resolved (opt-out — not marketing opt-in)
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = qr.Id,
                    GuestName = "Resolved Guest",
                    GuestContact = "resolved@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Fixed",
                    OffersOptOut = true,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Negative,
                    WorkflowStatus = FeedbackWorkflowStatus.Resolved,
                    CreatedAt = new DateTime(
                        2026,
                        7,
                        12,
                        10,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                }
            );

            // Current: 5 needs-attention InProgress (plus 1 extra below for cap)
            // Needs0 opt-out; Needs1–4 opt-in.
            for (var i = 0; i < 5; i++)
            {
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        QrCodeId = qr.Id,
                        GuestName = $"Needs Guest {i}",
                        GuestContact = $"needs{i}@example.com",
                        ContactType = ContactType.Email,
                        Comment = $"Needs attention {i}",
                        OffersOptOut = i == 0,
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        WorkflowStatus = FeedbackWorkflowStatus.InProgress,
                        CreatedAt = new DateTime(
                            2026,
                            7,
                            13,
                            12 + i,
                            0,
                            0,
                            DateTimeKind.Utc
                        ),
                    }
                );
            }

            // 6th needs-attention (oldest) — proves list cap 5
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = qr.Id,
                    GuestName = "Needs Guest Extra",
                    GuestContact = "needsx@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Needs attention extra",
                    OffersOptOut = false,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Negative,
                    WorkflowStatus = FeedbackWorkflowStatus.InProgress,
                    CreatedAt = new DateTime(
                        2026,
                        7,
                        10,
                        8,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                }
            );

            await context.SaveChangesAsync();
            return seeded;
        }
    }
}
