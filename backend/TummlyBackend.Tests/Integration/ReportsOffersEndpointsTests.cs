using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>GET /api/reports/offers</c> — auth, window/previous,
    /// lifetime empty, Soft-lock read-allowed, control signal / performance shape.
    /// </summary>
    public class ReportsOffersEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReportsOffersEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetOffers_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(OffersUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOffers_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerAsync("reports-of-owner-a");
            var other = await SeedOwnerAsync("reports-of-owner-b");

            using var request = AuthorizedGet(
                OffersUrl(other.LocationId, from, to),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOffers_ReturnsLifetimeEmpty_WhenOnlyCatalogOfferExists()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-of-empty");
            await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Catalog only",
                CatalogOfferStatus.Active,
                createdAt: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                OffersUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.False(body.TryGetProperty("kpis", out _));
            Assert.False(body.TryGetProperty("performance", out _));
        }

        [Fact]
        public async Task GetOffers_ReturnsPreviousPeriodCounts_ForEqualLengthWindow()
        {
            // Current [Jul 10, Jul 17); previous [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-of-prev");
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Windowed offer",
                CatalogOfferStatus.Active,
                createdAt: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            );
            var guestId = await SeedLocationGuestAsync(
                seeded.LocationId,
                "Maya"
            );

            await SeedOfferIssueAsync(
                offerId,
                guestId,
                "TUM-OFPREV1",
                issuedAt: new DateTime(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc),
                redeemedAt: new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc)
            );
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                "TUM-OFCURR1",
                issuedAt: new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc),
                redeemedAt: new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                OffersUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());

            var kpis = body.GetProperty("kpis");
            var claims = kpis.GetProperty("offerClaims");
            Assert.Equal(1, claims.GetProperty("value").GetInt32());
            Assert.Equal(1, claims.GetProperty("valuePrevious").GetInt32());

            var redemptions = kpis.GetProperty("redemptions");
            Assert.Equal(1, redemptions.GetProperty("value").GetInt32());
            Assert.Equal(
                1,
                redemptions.GetProperty("valuePrevious").GetInt32()
            );

            var rate = kpis.GetProperty("redemptionRate");
            Assert.Equal(1.0, rate.GetProperty("value").GetDouble());
            Assert.Equal(1.0, rate.GetProperty("valuePrevious").GetDouble());

            Assert.Equal(1, kpis.GetProperty("activeOffers").GetProperty("value").GetInt32());

            var performance = body.GetProperty("performance");
            Assert.Equal(JsonValueKind.Array, performance.ValueKind);
            Assert.Equal(1, performance.GetArrayLength());
            Assert.Equal(offerId, performance[0].GetProperty("offerId").GetInt32());
            Assert.Equal("Windowed offer", performance[0].GetProperty("offer").GetString());
            Assert.False(performance[0].TryGetProperty("source", out _));

            var recent = body.GetProperty("recentRedemptions");
            Assert.Equal(1, recent.GetArrayLength());
            Assert.Equal("redeemed", recent[0].GetProperty("outcome").GetString());
            Assert.Equal("Maya", recent[0].GetProperty("guestName").GetString());
        }

        [Fact]
        public async Task GetOffers_Returns200_WhenSoftLocked()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-of-softlock", softLock: true);
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Soft lock offer",
                CatalogOfferStatus.Active,
                createdAt: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Sam");
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                "TUM-OFSOFT1",
                issuedAt: new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc),
                redeemedAt: new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                OffersUrl(seeded.LocationId, from, to),
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
                    .GetProperty("offerClaims")
                    .GetProperty("value")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task GetOffers_EmitsRepeatedInvalidControlSignal()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-of-signal");
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Signal offer",
                CatalogOfferStatus.Active,
                createdAt: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Lee");
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                "TUM-OFSIG01",
                issuedAt: new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
                claimedAt: new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc)
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc),
                OfferRedeemFailureReasons.AlreadyUsed
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc),
                OfferRedeemFailureReasons.Expired
            );

            using var request = AuthorizedGet(
                OffersUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var signals = body.GetProperty("controlSignals");
            Assert.True(signals.GetArrayLength() >= 1);
            Assert.Equal(
                "repeated-invalid",
                signals[0].GetProperty("kind").GetString()
            );
            Assert.Equal(2, signals[0].GetProperty("count").GetInt32());
            Assert.Equal(
                "redemption-log",
                signals[0].GetProperty("target").GetString()
            );
            Assert.Equal(
                2,
                body
                    .GetProperty("kpis")
                    .GetProperty("invalidAttempts")
                    .GetProperty("value")
                    .GetInt32()
            );
        }

        private static string OffersUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/reports/offers?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
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
                FullName = "Reports Offers Owner",
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
                Name = "Reports Offers Venue",
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

        private async Task<int> SeedCatalogOfferAsync(
            int locationId,
            string title,
            string status,
            DateTime? createdAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = createdAt ?? DateTime.UtcNow;
            var entity = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = status,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Seeded for offers report tests.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        private async Task<int> SeedLocationGuestAsync(
            int locationId,
            string name
        )
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
                Email = $"reports-of-guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = name,
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
                ExpiryAtUtc = expiryAt ?? issuedAt.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "Offers report seed",
                Description = "Seeded issue",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
            });
            await context.SaveChangesAsync();
        }

        private async Task SeedFailedAttemptAsync(
            int catalogOfferId,
            int locationId,
            DateTime attemptedAt,
            string reason
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
                Reason = reason,
            });
            await context.SaveChangesAsync();
        }
    }
}
