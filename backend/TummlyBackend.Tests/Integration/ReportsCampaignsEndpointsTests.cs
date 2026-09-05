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
    /// Seam: <c>GET /api/reports/campaigns</c> — auth, aggregates,
    /// lifetime empty, Soft-lock read-allowed.
    /// </summary>
    public class ReportsCampaignsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReportsCampaignsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCampaigns_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(CampaignsUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaigns_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerAsync("reports-camp-owner-a");
            var other = await SeedOwnerAsync("reports-camp-owner-b");

            using var request = AuthorizedGet(
                CampaignsUrl(other.LocationId, from, to),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaigns_ReturnsLifetimeEmpty_WhenOnlyDraftExists()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-camp-empty");
            await AddCampaignAsync(
                seeded.LocationId,
                CampaignLifecycleService.DraftStatus,
                "Draft only",
                updatedAt: from.AddDays(1)
            );

            using var request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.False(body.TryGetProperty("campaignsSent", out _));
            Assert.False(body.TryGetProperty("performance", out _));
            Assert.False(body.TryGetProperty("needsAttention", out _));
        }

        [Fact]
        public async Task GetCampaigns_ReturnsAggregates_PreviousPeriod_AndOmitsDeferredKpis()
        {
            // Current [Jul 10, Jul 17); previous [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-camp-agg");

            var currentSentId = await AddCampaignAsync(
                seeded.LocationId,
                CampaignLifecycleService.SentStatus,
                "Current sent",
                updatedAt: new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc),
                goalId: "thank-recent-guests",
                channel: "email"
            );
            var previousSentId = await AddCampaignAsync(
                seeded.LocationId,
                CampaignLifecycleService.SentStatus,
                "Previous sent",
                updatedAt: new DateTime(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc),
                channel: "email"
            );
            var partialId = await AddCampaignAsync(
                seeded.LocationId,
                CampaignLifecycleService.PartiallySentStatus,
                "Partial in window",
                updatedAt: new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc),
                goalId: "boost-quieter-time",
                channel: "sms"
            );
            await AddCampaignAsync(
                seeded.LocationId,
                CampaignLifecycleService.FailedStatus,
                "Failed open",
                updatedAt: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
            );

            var guestA = await AddGuestAsync(
                seeded.LocationId,
                seeded.RestaurantId
            );
            var guestB = await AddGuestAsync(
                seeded.LocationId,
                seeded.RestaurantId
            );
            var guestC = await AddGuestAsync(
                seeded.LocationId,
                seeded.RestaurantId
            );
            var guestD = await AddGuestAsync(
                seeded.LocationId,
                seeded.RestaurantId
            );
            var guestE = await AddGuestAsync(
                seeded.LocationId,
                seeded.RestaurantId
            );

            await AddDeliveryAsync(
                currentSentId,
                guestA,
                "email",
                CampaignFireService.AcceptedOutcome,
                acceptedAt: new DateTime(2026, 7, 14, 13, 0, 0, DateTimeKind.Utc),
                acceptedUnits: 1
            );
            await AddDeliveryAsync(
                currentSentId,
                guestB,
                "sms",
                CampaignFireService.AcceptedOutcome,
                acceptedAt: new DateTime(2026, 7, 14, 14, 0, 0, DateTimeKind.Utc),
                acceptedUnits: 2
            );
            await AddDeliveryAsync(
                currentSentId,
                guestC,
                "email",
                CampaignFireService.RejectedOutcome,
                acceptedAt: null,
                acceptedUnits: null,
                updatedAt: new DateTime(2026, 7, 14, 15, 0, 0, DateTimeKind.Utc)
            );
            await AddDeliveryAsync(
                previousSentId,
                guestD,
                "email",
                CampaignFireService.AcceptedOutcome,
                acceptedAt: new DateTime(2026, 7, 5, 13, 0, 0, DateTimeKind.Utc),
                acceptedUnits: 1
            );
            await AddDeliveryAsync(
                previousSentId,
                guestE,
                "email",
                CampaignFireService.RejectedOutcome,
                acceptedAt: null,
                acceptedUnits: null,
                updatedAt: new DateTime(2026, 7, 5, 14, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.False(body.TryGetProperty("offerClaims", out _));
            Assert.False(body.TryGetProperty("offerRedemptions", out _));
            Assert.False(body.TryGetProperty("unsubscribes", out _));

            var campaignsSent = body.GetProperty("campaignsSent");
            Assert.Equal(2, campaignsSent.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                campaignsSent.GetProperty("valuePrevious").GetInt32()
            );

            var guestsMessaged = body.GetProperty("guestsMessaged");
            Assert.Equal(2, guestsMessaged.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                guestsMessaged.GetProperty("valuePrevious").GetInt32()
            );

            var failedSends = body.GetProperty("failedSends");
            Assert.Equal(1, failedSends.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                failedSends.GetProperty("valuePrevious").GetInt32()
            );

            var performance = body.GetProperty("performance");
            Assert.Equal(2, performance.GetArrayLength());
            Assert.Equal(
                "Partial in window",
                performance[0].GetProperty("name").GetString()
            );
            Assert.Equal(
                partialId,
                performance[0].GetProperty("campaignId").GetInt32()
            );
            Assert.Equal(
                "boost-quieter-time",
                performance[0].GetProperty("goal").GetString()
            );
            Assert.Equal(
                "sms",
                performance[0].GetProperty("channel").GetString()
            );
            Assert.Equal(0, performance[0].GetProperty("sent").GetInt32());
            Assert.Equal(
                CampaignLifecycleService.PartiallySentStatus,
                performance[0].GetProperty("status").GetString()
            );
            Assert.False(performance[0].TryGetProperty("claims", out _));
            Assert.False(performance[0].TryGetProperty("redemptions", out _));
            Assert.False(performance[0].TryGetProperty("unsubscribes", out _));

            Assert.Equal(
                "Current sent",
                performance[1].GetProperty("name").GetString()
            );
            Assert.Equal(3, performance[1].GetProperty("sent").GetInt32());

            var attention = body.GetProperty("needsAttention");
            Assert.Equal(2, attention.GetArrayLength());
            var attentionNames = attention
                .EnumerateArray()
                .Select(row => row.GetProperty("name").GetString())
                .ToHashSet();
            Assert.Contains("Failed open", attentionNames);
            Assert.Contains("Partial in window", attentionNames);
        }

        [Fact]
        public async Task GetCampaigns_Returns200_WhenSoftLocked()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync(
                "reports-camp-softlock",
                softLock: true
            );
            await AddCampaignAsync(
                seeded.LocationId,
                CampaignLifecycleService.SentStatus,
                "Soft-lock sent",
                updatedAt: new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.Equal(
                1,
                body.GetProperty("campaignsSent").GetProperty("value").GetInt32()
            );
        }

        private static string CampaignsUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/reports/campaigns?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
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

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId
        )> SeedOwnerAsync(string emailLocalPart, bool softLock = false)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Reports Campaigns Owner",
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
                Name = "Reports Campaigns Venue",
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

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<int> AddCampaignAsync(
            int locationId,
            string status,
            string name,
            DateTime updatedAt,
            string? goalId = null,
            string? channel = "email"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Status = status,
                Name = name,
                GoalId = goalId,
                Channel = channel,
                CreatedAt = updatedAt,
                UpdatedAt = updatedAt,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
            return campaign.Id;
        }

        private async Task<int> AddGuestAsync(int locationId, int restaurantId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = $"{Guid.NewGuid():N}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();
            return guest.Id;
        }

        private async Task AddDeliveryAsync(
            int campaignId,
            int locationGuestId,
            string channel,
            string outcome,
            DateTime? acceptedAt,
            int? acceptedUnits,
            DateTime? updatedAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var at = updatedAt ?? acceptedAt ?? DateTime.UtcNow;
            context.CampaignRecipientDeliveries.Add(
                new CampaignRecipientDelivery
                {
                    CampaignId = campaignId,
                    LocationGuestId = locationGuestId,
                    Channel = channel,
                    Outcome = outcome,
                    AcceptedAtUtc = acceptedAt,
                    AcceptedUnits = acceptedUnits,
                    UpdatedAtUtc = at,
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
