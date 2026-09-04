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
    /// Seam: <c>GET /api/reports/overview</c> — auth, window/previous,
    /// lifetime empty, Soft-lock read-allowed.
    /// </summary>
    public class ReportsOverviewEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReportsOverviewEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetOverview_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(OverviewUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOverview_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerAsync("reports-ov-owner-a");
            var other = await SeedOwnerAsync("reports-ov-owner-b");

            using var request = AuthorizedGet(
                OverviewUrl(other.LocationId, from, to),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOverview_ReturnsLifetimeEmpty_WhenNoEverActivity()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-ov-empty");

            using var request = AuthorizedGet(
                OverviewUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.False(body.TryGetProperty("funnel", out _));
        }

        [Fact]
        public async Task GetOverview_ReturnsPreviousPeriodCounts_ForEqualLengthWindow()
        {
            // Current [Jul 10, Jul 17); previous [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithFeedbackInWindowsAsync(
                "reports-ov-prev",
                currentCreatedAt: new DateTime(
                    2026,
                    7,
                    14,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                previousCreatedAt: new DateTime(
                    2026,
                    7,
                    5,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                )
            );

            using var request = AuthorizedGet(
                OverviewUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());

            var feedbackReceived = body
                .GetProperty("funnel")
                .GetProperty("feedbackReceived");
            Assert.Equal(1, feedbackReceived.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                feedbackReceived.GetProperty("valuePrevious").GetInt32()
            );

            var feedbackMessages = body
                .GetProperty("privateFeedback")
                .GetProperty("feedbackMessages");
            Assert.Equal(1, feedbackMessages.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                feedbackMessages.GetProperty("valuePrevious").GetInt32()
            );
        }

        [Fact]
        public async Task GetOverview_Returns200_WhenSoftLocked()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithFeedbackInWindowsAsync(
                "reports-ov-softlock",
                currentCreatedAt: new DateTime(
                    2026,
                    7,
                    14,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                previousCreatedAt: null,
                softLock: true
            );

            using var request = AuthorizedGet(
                OverviewUrl(seeded.LocationId, from, to),
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
                    .GetProperty("funnel")
                    .GetProperty("feedbackReceived")
                    .GetProperty("value")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task GetOverview_CountsTerminalCampaignSends_AndClearsLifetimeEmpty()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-ov-terminal-send");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.Campaigns.Add(
                    new Campaign
                    {
                        RestaurantLocationId = seeded.LocationId,
                        Status = CampaignLifecycleService.PartiallySentStatus,
                        Name = "Partial send",
                        GoalId = "bring-back",
                        Channel = "email",
                        CreatedAt = new DateTime(
                            2026,
                            7,
                            12,
                            10,
                            0,
                            0,
                            DateTimeKind.Utc
                        ),
                        UpdatedAt = new DateTime(
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
                context.Campaigns.Add(
                    new Campaign
                    {
                        RestaurantLocationId = seeded.LocationId,
                        Status = CampaignLifecycleService.FailedStatus,
                        Name = "Failed send",
                        GoalId = "bring-back",
                        Channel = "email",
                        CreatedAt = new DateTime(
                            2026,
                            7,
                            5,
                            10,
                            0,
                            0,
                            DateTimeKind.Utc
                        ),
                        UpdatedAt = new DateTime(
                            2026,
                            7,
                            5,
                            10,
                            0,
                            0,
                            DateTimeKind.Utc
                        ),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedGet(
                OverviewUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());

            var campaignsSent = body
                .GetProperty("funnel")
                .GetProperty("campaignsSent");
            Assert.Equal(1, campaignsSent.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                campaignsSent.GetProperty("valuePrevious").GetInt32()
            );
        }

        private static string OverviewUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/reports/overview?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
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
                FullName = "Reports Overview Owner",
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
                Name = "Reports Overview Venue",
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
        )> SeedOwnerWithFeedbackInWindowsAsync(
            string emailLocalPart,
            DateTime currentCreatedAt,
            DateTime? previousCreatedAt,
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
                    GuestName = "Current Guest",
                    GuestContact = "current@example.com",
                    ContactType = ContactType.Email,
                    Comment = "In window",
                    OffersOptOut = false,
                    CreatedAt = currentCreatedAt,
                }
            );

            if (previousCreatedAt != null)
            {
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
                        CreatedAt = previousCreatedAt.Value,
                    }
                );
            }

            await context.SaveChangesAsync();
            return seeded;
        }
    }
}
