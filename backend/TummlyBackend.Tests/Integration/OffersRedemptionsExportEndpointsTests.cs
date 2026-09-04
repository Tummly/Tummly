using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>GET /api/offers/redemptions/export</c> — auth, Soft-lock/Dormant/
    /// chargeback 403 (list stays 200), empty window, window filter, soft-max,
    /// Content-Disposition filename.
    /// </summary>
    public class OffersRedemptionsExportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private static readonly DateTime WindowFrom = new(
            2026,
            7,
            10,
            0,
            0,
            0,
            DateTimeKind.Utc
        );
        private static readonly DateTime WindowTo = new(
            2026,
            7,
            17,
            0,
            0,
            0,
            DateTimeKind.Utc
        );

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersRedemptionsExportEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Export_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                ExportUrl(1, WindowFrom, WindowTo)
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Export_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("orex-ownera");
            var other = await SeedOwnerAsync("orex-ownerb");

            using var request = AuthorizedGet(
                ExportUrl(other.LocationId, WindowFrom, WindowTo),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Export_EmptyWindow_Returns200WithHeadersAndDisposition()
        {
            var seeded = await SeedOwnerAsync("orex-empty");

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType
            );

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-offers-redemptions-{seeded.LocationId}-",
                fileName
            );
            Assert.EndsWith("Z.csv", fileName);

            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("Date/time", csv);
            Assert.Contains("Guest", csv);
            Assert.Contains("Pass reference", csv);
            Assert.Contains("Location", csv);
            Assert.Contains("Staff member", csv);
            Assert.Contains("Outcome", csv);
            Assert.Contains("Reason", csv);
            Assert.Contains("Offer version", csv);
            Assert.Contains("Offer", csv);
            Assert.DoesNotContain("Actions", csv);

            var dataRows = csv
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .ToList();
            Assert.Empty(dataRows);
        }

        [Fact]
        public async Task Export_SoftLock_Returns403WithCode_ListStill200()
        {
            var seeded = await SeedOwnerAsync("orex-soft", softLock: true);

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("soft_lock", json.GetProperty("code").GetString());

            using var listRequest = AuthorizedGet(
                ListUrl(seeded.LocationId),
                seeded.Jwt
            );
            var listResponse = await _client.SendAsync(listRequest);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        }

        [Fact]
        public async Task Export_Dormant_Returns403()
        {
            var seeded = await SeedOwnerAsync("orex-dormant");
            await SetBillingStatusAsync(
                seeded.LocationId,
                BillingStatuses.Dormant
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("dormant", json.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Export_ChargebackRestricted_Returns403()
        {
            var seeded = await SeedOwnerAsync("orex-cb");
            await SetChargebackRestrictedAsync(
                seeded.LocationId,
                restricted: true
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal(
                "chargeback_restricted",
                json.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task Export_WindowFilter_IncludesInsideExcludesOutside()
        {
            var seeded = await SeedOwnerAsync("orex-window");
            var guestId = await SeedLocationGuestAsync(
                seeded.LocationId,
                "Sam"
            );
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Free coffee"
            );
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-IN001",
                claimedAt: WindowFrom.AddDays(1),
                redeemedAt: WindowFrom.AddDays(2),
                title: "Free coffee"
            );
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-OUT001",
                claimedAt: WindowFrom.AddDays(-5),
                redeemedAt: WindowFrom.AddDays(-2),
                title: "Free coffee"
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                claimCode: "TUM-IN002",
                reason: "expired",
                attemptedAt: WindowFrom.AddDays(3)
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                claimCode: "TUM-OUT002",
                reason: "invalid",
                attemptedAt: WindowTo.AddHours(1)
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("TUM-IN001", csv);
            Assert.Contains("TUM-IN002", csv);
            Assert.DoesNotContain("TUM-OUT001", csv);
            Assert.DoesNotContain("TUM-OUT002", csv);
        }

        [Fact]
        public async Task Export_SoftMax_ReturnsAtMost10000NewestRows()
        {
            var seeded = await SeedOwnerAsync("orex-softmax");
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Soft max offer"
            );
            await SeedFailedAttemptsBulkAsync(
                offerId,
                seeded.LocationId,
                count: 10_001,
                baseAttemptedAt: WindowFrom.AddHours(1)
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            var dataRows = csv
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .ToList();
            Assert.True(dataRows.Count <= 10_000);
            Assert.Equal(10_000, dataRows.Count);
        }

        private static string ExportUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/offers/redemptions/export?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string ListUrl(int locationId)
        {
            return $"/api/offers/redemptions?locationId={locationId}";
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
                FullName = "Offers Redemptions Export Owner",
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
                Name = "Offers Redemptions Export Venue",
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

        private async Task SetBillingStatusAsync(
            int locationId,
            string billingStatus
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.RestaurantId)
                .FirstAsync();
            var account = await context.BillingAccounts
                .FirstAsync(a => a.RestaurantId == restaurantId);
            account.BillingStatus = billingStatus;
            if (billingStatus == BillingStatuses.Dormant)
            {
                account.DormantEnteredAt = DateTime.UtcNow.AddDays(-1);
            }

            await context.SaveChangesAsync();
        }

        private async Task SetChargebackRestrictedAsync(
            int locationId,
            bool restricted
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.RestaurantId)
                .FirstAsync();
            var account = await context.BillingAccounts
                .FirstAsync(a => a.RestaurantId == restaurantId);
            account.ChargebackRestricted = restricted;
            await context.SaveChangesAsync();
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
                Email = $"orex-guest-{Guid.NewGuid():N}@example.com",
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

        private async Task<int> SeedCatalogOfferAsync(
            int locationId,
            string title = "10% off next visit"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Export test offer",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();
            return offer.Id;
        }

        private async Task SeedOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            string claimCode,
            DateTime? claimedAt,
            DateTime? redeemedAt = null,
            string title = "10% off next visit"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var issuedAt = WindowFrom.AddDays(-7);

            context.OfferIssues.Add(new OfferIssue
            {
                CatalogOfferId = catalogOfferId,
                LocationGuestId = locationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = issuedAt,
                ClaimedAtUtc = claimedAt,
                RedeemedAtUtc = redeemedAt,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = issuedAt.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Export test issue",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
            });
            await context.SaveChangesAsync();
        }

        private async Task SeedFailedAttemptAsync(
            int catalogOfferId,
            int locationId,
            string claimCode,
            string reason,
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
                ClaimCode = claimCode,
                Reason = reason,
                AttemptedAtUtc = attemptedAt,
            });
            await context.SaveChangesAsync();
        }

        private async Task SeedFailedAttemptsBulkAsync(
            int catalogOfferId,
            int locationId,
            int count,
            DateTime baseAttemptedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var rows = new List<OfferRedeemFailedAttempt>(count);
            for (var i = 0; i < count; i++)
            {
                rows.Add(
                    new OfferRedeemFailedAttempt
                    {
                        CatalogOfferId = catalogOfferId,
                        RestaurantLocationId = locationId,
                        ClaimCode = $"TUM-SM{i:D5}",
                        Reason = "invalid",
                        AttemptedAtUtc = baseAttemptedAt.AddSeconds(i),
                    }
                );
            }

            context.OfferRedeemFailedAttempts.AddRange(rows);
            await context.SaveChangesAsync();
        }
    }
}
