using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: Offers Performance + per-offer metrics + list lifetime counts
    /// (ticket 29).
    /// </summary>
    public class OffersMetricsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersMetricsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetOffersPerformance_ReturnsWindowScopedKpis()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync("offers-perf-window");

            var activeOfferId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Active A",
                status: "active"
            );
            await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Active B",
                status: "active"
            );
            await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Paused",
                status: "paused"
            );

            var guestId = await SeedLocationGuestAsync(seeded.LocationId);

            // Issued in window
            await SeedOfferIssueAsync(
                activeOfferId,
                guestId,
                claimCode: "TUM-AAAAAA",
                issuedAt: new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                claimedAt: null
            );
            // Issued at to (excluded by half-open)
            await SeedOfferIssueAsync(
                activeOfferId,
                guestId,
                claimCode: "TUM-BBBBBB",
                issuedAt: to,
                claimedAt: null
            );
            // Claimed in window (issued before window)
            await SeedOfferIssueAsync(
                activeOfferId,
                guestId,
                claimCode: "TUM-CCCCCC",
                issuedAt: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc)
            );
            // Redeemed in window
            await SeedOfferIssueAsync(
                activeOfferId,
                guestId,
                claimCode: "TUM-DDDDDD",
                issuedAt: new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 11, 1, 0, 0, DateTimeKind.Utc),
                redeemedAt: new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                PerformanceUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(2, body.GetProperty("activeOffers").GetInt32());
            Assert.Equal(2, body.GetProperty("offersIssued").GetInt32());
            Assert.Equal(2, body.GetProperty("claims").GetInt32());
            Assert.Equal(1, body.GetProperty("redemptions").GetInt32());
            Assert.Equal(
                0.5,
                body.GetProperty("claimToRedemptionRate").GetDouble(),
                precision: 5
            );
        }

        [Fact]
        public async Task GetOffersPerformance_ClaimToRedemptionRate_IsNull_WhenClaimsZero()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync("offers-perf-rate-null");
            await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Active only",
                status: "active"
            );

            using var request = AuthorizedGet(
                PerformanceUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("activeOffers").GetInt32());
            Assert.Equal(0, body.GetProperty("claims").GetInt32());
            Assert.Equal(0, body.GetProperty("redemptions").GetInt32());
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("claimToRedemptionRate").ValueKind
            );
        }

        [Fact]
        public async Task GetOffersPerformance_Returns400_WhenFromMissingOrNotBeforeTo()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-perf-validation");

            using var missing = AuthorizedGet(
                $"/api/offers/performance?locationId={seeded.LocationId}&to=2026-07-17T00:00:00Z",
                seeded.Jwt
            );
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (await _client.SendAsync(missing)).StatusCode
            );

            using var badOrder = AuthorizedGet(
                PerformanceUrl(
                    seeded.LocationId,
                    new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc)
                ),
                seeded.Jwt
            );
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (await _client.SendAsync(badOrder)).StatusCode
            );
        }

        [Fact]
        public async Task GetOfferMetrics_ReturnsWindowScopedDetailsKpis()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync("offers-metrics-details");
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Details offer",
                status: "active"
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId);

            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-EEEEEE",
                issuedAt: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 12, 8, 0, 0, DateTimeKind.Utc),
                redeemedAt: new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc),
                expiryAt: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
            );
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-FFFFFF",
                issuedAt: new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc),
                expiryAt: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
            );
            // Expired unused in window
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-GGGGGG",
                issuedAt: new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 6, 20, 1, 0, 0, DateTimeKind.Utc),
                expiryAt: new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc)
            );
            // Expired but cancelled — excluded
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-HHHHHH",
                issuedAt: new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 6, 21, 1, 0, 0, DateTimeKind.Utc),
                cancelledAt: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                expiryAt: new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)
            );

            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                attemptedAt: new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc)
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                attemptedAt: to
            );

            using var request = AuthorizedGet(
                MetricsUrl(offerId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(2, body.GetProperty("claims").GetInt32());
            Assert.Equal(1, body.GetProperty("redemptions").GetInt32());
            Assert.Equal(
                0.5,
                body.GetProperty("redemptionRate").GetDouble(),
                precision: 5
            );
            Assert.Equal(1, body.GetProperty("expiredUnused").GetInt32());
            Assert.Equal(1, body.GetProperty("failedAttempts").GetInt32());
        }

        [Fact]
        public async Task GetOfferMetrics_Returns404_WhenOfferMissing()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-metrics-404");
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = AuthorizedGet(
                MetricsUrl(999_999, from, to),
                seeded.Jwt
            );
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await _client.SendAsync(request)).StatusCode
            );
        }

        [Fact]
        public async Task GetOffersList_ReturnsLiveLifetimeClaimsAndRedeemed()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-list-lifetime");
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Lifetime row",
                status: "active"
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId);

            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-IIIIII",
                issuedAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                redeemedAt: new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            );
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-JJJJJJ",
                issuedAt: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)
            );
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-KKKKKK",
                issuedAt: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                claimedAt: null
            );

            using var request = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=all&page=1&pageSize=25",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var item = (await ReadJsonAsync(response))
                .GetProperty("items")
                .EnumerateArray()
                .Single(row => row.GetProperty("id").GetInt32() == offerId);

            Assert.Equal(2, item.GetProperty("lifetimeClaims").GetInt32());
            Assert.Equal(1, item.GetProperty("lifetimeRedeemed").GetInt32());
        }

        private static string PerformanceUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            var fromText = from.ToString(
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture
            );
            var toText = to.ToString(
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture
            );
            return $"/api/offers/performance?locationId={locationId}&from={fromText}&to={toText}";
        }

        private static string MetricsUrl(
            int offerId,
            DateTime from,
            DateTime to
        )
        {
            var fromText = from.ToString(
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture
            );
            var toText = to.ToString(
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture
            );
            return $"/api/offers/{offerId}/metrics?from={fromText}&to={toText}";
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
                FullName = "Offers Metrics Owner",
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
                Name = "Offers Metrics Venue",
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

        private async Task<int> SeedCatalogOfferAsync(
            int locationId,
            string title,
            string status
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var entity = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = status,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Seeded for metrics tests.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        private async Task<int> SeedLocationGuestAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var location = await context.RestaurantLocations
                .FindAsync(locationId);
            Assert.NotNull(location);

            var master = new MasterGuest
            {
                RestaurantId = location!.RestaurantId,
                Email = $"metrics-guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                CreatedAt = now,
            };
            context.LocationGuests.Add(lg);
            await context.SaveChangesAsync();
            return lg.Id;
        }

        private async Task SeedOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            string claimCode,
            DateTime issuedAt,
            DateTime? claimedAt,
            DateTime? redeemedAt = null,
            DateTime? cancelledAt = null,
            DateTime? expiryAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.OfferIssues.Add(new OfferIssue
            {
                CatalogOfferId = catalogOfferId,
                LocationGuestId = locationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = issuedAt,
                ClaimedAtUtc = claimedAt,
                RedeemedAtUtc = redeemedAt,
                CancelledAtUtc = cancelledAt,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = expiryAt
                    ?? issuedAt.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "Metrics seed",
                Description = "Seeded issue",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
            });
            await context.SaveChangesAsync();
        }

        private async Task SeedFailedAttemptAsync(
            int catalogOfferId,
            int locationId,
            DateTime attemptedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.OfferRedeemFailedAttempts.Add(new OfferRedeemFailedAttempt
            {
                CatalogOfferId = catalogOfferId,
                RestaurantLocationId = locationId,
                AttemptedAtUtc = attemptedAt,
                ClaimCode = "TUM-XXXXXX",
                Reason = "code_not_found",
            });
            await context.SaveChangesAsync();
        }
    }
}
