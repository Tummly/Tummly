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
    /// Seam: <c>GET /api/reports/capture</c> — auth, window/previous,
    /// lifetime empty, Soft-lock read-allowed.
    /// </summary>
    public class ReportsCaptureEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReportsCaptureEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCapture_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(CaptureUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCapture_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerAsync("reports-cap-owner-a");
            var other = await SeedOwnerAsync("reports-cap-owner-b");

            using var request = AuthorizedGet(
                CaptureUrl(other.LocationId, from, to),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCapture_ReturnsLifetimeEmpty_WhenLocationHasNoQr()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-cap-no-qr");

            using var request = AuthorizedGet(
                CaptureUrl(seeded.LocationId, from, to),
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
        public async Task GetCapture_ReturnsLifetimeEmpty_WhenNeverScannedActiveOrPausedQr()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerAsync("reports-cap-empty");

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrType = QrType.CounterCard,
                    Status = QrCodeStatus.Active,
                    Token = "reports-cap-empty-qr-token123",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            using var request = AuthorizedGet(
                CaptureUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.False(body.TryGetProperty("funnel", out _));
            Assert.False(body.TryGetProperty("placements", out _));
        }

        [Fact]
        public async Task GetCapture_ReturnsPreviousPeriodCounts_ForEqualLengthWindow()
        {
            // Current [Jul 10, Jul 17); previous [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithCaptureWindowsAsync(
                "reports-cap-prev"
            );

            using var request = AuthorizedGet(
                CaptureUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());

            var funnel = body.GetProperty("funnel");
            AssertMetric(funnel.GetProperty("qrScans"), 3, 1);
            AssertMetric(funnel.GetProperty("feedbackSubmitted"), 2, 1);
            AssertMetric(funnel.GetProperty("contactableGuests"), 1, 1);
            AssertMetric(funnel.GetProperty("offerClaimed"), 1, 1);

            var placements = body.GetProperty("placements");
            Assert.Equal(2, placements.GetArrayLength());

            var byName = placements
                .EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("name").GetString()!,
                    row => row,
                    StringComparer.Ordinal
                );
            Assert.False(byName.ContainsKey("Digital guest link"));
            Assert.Equal(
                "Active",
                byName["Counter card"].GetProperty("status").GetString()
            );
            Assert.Equal(2, byName["Counter card"].GetProperty("scans").GetInt32());
            Assert.Equal(
                2,
                byName["Counter card"].GetProperty("feedback").GetInt32()
            );
            Assert.Equal(
                1,
                byName["Counter card"].GetProperty("contactable").GetInt32()
            );
            Assert.False(
                byName["Counter card"].TryGetProperty("claims", out _)
            );
            Assert.Equal(
                "Paused",
                byName["Window sticker"].GetProperty("status").GetString()
            );
            Assert.Equal(
                0,
                byName["Window sticker"].GetProperty("scans").GetInt32()
            );
        }

        [Fact]
        public async Task GetCapture_Returns200_WhenSoftLocked()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithCaptureWindowsAsync(
                "reports-cap-softlock",
                softLock: true
            );

            using var request = AuthorizedGet(
                CaptureUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("lifetimeEmpty").GetBoolean());
            Assert.Equal(
                3,
                body
                    .GetProperty("funnel")
                    .GetProperty("qrScans")
                    .GetProperty("value")
                    .GetInt32()
            );
        }

        private static void AssertMetric(
            JsonElement metric,
            int value,
            int valuePrevious
        )
        {
            Assert.Equal(value, metric.GetProperty("value").GetInt32());
            Assert.Equal(
                valuePrevious,
                metric.GetProperty("valuePrevious").GetInt32()
            );
        }

        private static string CaptureUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/reports/capture?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
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
                FullName = "Reports Capture Owner",
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
                Name = "Reports Capture Venue",
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
        )> SeedOwnerWithCaptureWindowsAsync(
            string emailLocalPart,
            bool softLock = false
        )
        {
            var seeded = await SeedOwnerAsync(emailLocalPart, softLock);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var counter = new QrCode
            {
                RestaurantLocationId = seeded.LocationId,
                QrType = QrType.CounterCard,
                Status = QrCodeStatus.Active,
                Token = $"{emailLocalPart}-counter-token12",
                CreatedAt = DateTime.UtcNow,
            };
            var window = new QrCode
            {
                RestaurantLocationId = seeded.LocationId,
                QrType = QrType.WindowSticker,
                Status = QrCodeStatus.Paused,
                Token = $"{emailLocalPart}-window-token123",
                CreatedAt = DateTime.UtcNow,
            };
            var digital = new QrCode
            {
                RestaurantLocationId = seeded.LocationId,
                QrType = QrType.DigitalGuestLink,
                Status = QrCodeStatus.Active,
                Token = $"{emailLocalPart}-dgl-token12345",
                LinkName = "Email blast",
                NormalizedLinkName = "email blast",
                Channel = DigitalGuestLinkChannel.Email,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = seeded.LocationId,
                QrType = QrType.DeliveryInsert,
                Status = QrCodeStatus.Archived,
                Token = $"{emailLocalPart}-archived-token1",
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(counter, window, digital, archived);
            await context.SaveChangesAsync();

            var currentScan = new DateTime(
                2026,
                7,
                14,
                12,
                0,
                0,
                DateTimeKind.Utc
            );
            var previousScan = new DateTime(
                2026,
                7,
                5,
                12,
                0,
                0,
                DateTimeKind.Utc
            );

            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = counter.Id,
                    CreatedAt = currentScan,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = counter.Id,
                    CreatedAt = currentScan.AddHours(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = counter.Id,
                    CreatedAt = previousScan,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = digital.Id,
                    CreatedAt = currentScan,
                }
            );

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = counter.Id,
                    GuestName = "Current Contactable",
                    GuestContact = "current@example.com",
                    ContactType = ContactType.Email,
                    Comment = "In window",
                    OffersOptOut = false,
                    CreatedAt = currentScan,
                },
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = counter.Id,
                    GuestName = "Current Opt-out",
                    GuestContact = "optout@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Opted out",
                    OffersOptOut = true,
                    CreatedAt = currentScan.AddMinutes(30),
                },
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    QrCodeId = counter.Id,
                    GuestName = "Previous Guest",
                    GuestContact = "previous@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Previous window",
                    OffersOptOut = false,
                    CreatedAt = previousScan,
                }
            );

            var offer = new CatalogOffer
            {
                RestaurantLocationId = seeded.LocationId,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "10% off next visit",
                Description = "Capture report thank-you offer",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();

            var location = await context.RestaurantLocations.FindAsync(
                seeded.LocationId
            );
            var restaurantId = location!.RestaurantId;

            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = $"{emailLocalPart}-guest@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                RestaurantLocationId = seeded.LocationId,
                MasterGuestId = master.Id,
                Name = "Capture Guest",
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            var issuedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            context.OfferIssues.AddRange(
                new OfferIssue
                {
                    CatalogOfferId = offer.Id,
                    LocationGuestId = guest.Id,
                    ClaimCode = $"{emailLocalPart}-TY1",
                    IssuedAtUtc = issuedAt,
                    ClaimedAtUtc = currentScan,
                    Source = OfferIssueSources.GuestFormThankYou,
                    ExpiryAtUtc = issuedAt.AddDays(14),
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "10% off next visit",
                    Description = "Thank-you current",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                },
                new OfferIssue
                {
                    CatalogOfferId = offer.Id,
                    LocationGuestId = guest.Id,
                    ClaimCode = $"{emailLocalPart}-TY0",
                    IssuedAtUtc = issuedAt,
                    ClaimedAtUtc = previousScan,
                    Source = OfferIssueSources.GuestFormThankYou,
                    ExpiryAtUtc = issuedAt.AddDays(14),
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "10% off next visit",
                    Description = "Thank-you previous",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                },
                new OfferIssue
                {
                    CatalogOfferId = offer.Id,
                    LocationGuestId = guest.Id,
                    ClaimCode = $"{emailLocalPart}-CAM",
                    IssuedAtUtc = issuedAt,
                    ClaimedAtUtc = currentScan,
                    Source = OfferIssueSources.Campaign,
                    ExpiryAtUtc = issuedAt.AddDays(14),
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "10% off next visit",
                    Description = "Campaign must not count",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                }
            );

            await context.SaveChangesAsync();
            return seeded;
        }
    }
}
