using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CaptureLocationSnapshotEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CaptureLocationSnapshotEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_ReturnsWindowedTotals_PreviousPeriod_AndRowsThatSumToTotals()
        {
            // Current [Jul 10, Jul 17); previous equal span [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedCaptureFactsAsync(
                email: "capture-snapshot-totals@example.com",
                tokenSuffix: "totals",
                currentScanAt: new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc),
                previousScanAt: new DateTime(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc),
                currentFeedbackAt: new DateTime(2026, 7, 14, 13, 0, 0, DateTimeKind.Utc),
                previousFeedbackAt: new DateTime(2026, 7, 5, 13, 0, 0, DateTimeKind.Utc),
                currentFeedbackOffersOptOut: false,
                previousFeedbackOffersOptOut: true,
                extraScanAtBoundary: new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc)
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("Active", body.GetProperty("captureLocationStatus").GetString());
            Assert.Equal(1, body.GetProperty("qrScans").GetInt32());
            Assert.Equal(1, body.GetProperty("qrScansPrevious").GetInt32());
            Assert.Equal(1, body.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(
                1,
                body.GetProperty("feedbackSubmittedPrevious").GetInt32()
            );
            Assert.Equal(1, body.GetProperty("marketingOptIns").GetInt32());
            Assert.Equal(
                0,
                body.GetProperty("marketingOptInsPrevious").GetInt32()
            );
            Assert.Equal(0, body.GetProperty("offerClaims").GetInt32());
            Assert.False(body.GetProperty("offerClaimsHasRealData").GetBoolean());

            var placements = body.GetProperty("placements");
            Assert.Equal(JsonValueKind.Array, placements.ValueKind);
            Assert.Equal(1, placements.GetArrayLength());

            var sumScans = 0;
            var sumFeedback = 0;
            var sumOptIns = 0;
            foreach (var item in placements.EnumerateArray())
            {
                sumScans += item.GetProperty("qrScans").GetInt32();
                sumFeedback += item.GetProperty("feedbackSubmitted").GetInt32();
                sumOptIns += item.GetProperty("marketingOptIns").GetInt32();
            }

            Assert.Equal(body.GetProperty("qrScans").GetInt32(), sumScans);
            Assert.Equal(
                body.GetProperty("feedbackSubmitted").GetInt32(),
                sumFeedback
            );
            Assert.Equal(
                body.GetProperty("marketingOptIns").GetInt32(),
                sumOptIns
            );
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_ExcludesArchivedQrActivity_AndTotalsEqualRowSums()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedActiveAndArchivedActivityAsync(
                email: "capture-snapshot-archived@example.com",
                tokenSuffix: "archexcl"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);

            // Active (2 scans, 1 opted-in feedback) + Paused (1 scan, 1 opted-out
            // feedback) only; Archived QR's windowed activity excluded.
            Assert.Equal(3, body.GetProperty("qrScans").GetInt32());
            Assert.Equal(2, body.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, body.GetProperty("marketingOptIns").GetInt32());
            Assert.Equal(0, body.GetProperty("offerClaims").GetInt32());
            Assert.False(body.GetProperty("offerClaimsHasRealData").GetBoolean());

            var placements = body.GetProperty("placements");
            var sumScans = 0;
            var sumFeedback = 0;
            var sumOptIns = 0;
            foreach (var item in placements.EnumerateArray())
            {
                Assert.NotEqual(
                    "Archived",
                    item.GetProperty("status").GetString()
                );
                sumScans += item.GetProperty("qrScans").GetInt32();
                sumFeedback += item.GetProperty("feedbackSubmitted").GetInt32();
                sumOptIns += item.GetProperty("marketingOptIns").GetInt32();
            }

            Assert.Equal(body.GetProperty("qrScans").GetInt32(), sumScans);
            Assert.Equal(
                body.GetProperty("feedbackSubmitted").GetInt32(),
                sumFeedback
            );
            Assert.Equal(
                body.GetProperty("marketingOptIns").GetInt32(),
                sumOptIns
            );
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_ReturnsAllTimeLastScanAndLastJourneyUpdate()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var windowScan = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
            // After the selected window — must still win all-time Last scan.
            var newerOutsideWindowScan = new DateTime(
                2026,
                7,
                20,
                12,
                0,
                0,
                DateTimeKind.Utc
            );
            var journeyAt = new DateTime(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc);

            var seeded = await SeedLastScanAndJourneyAsync(
                email: "capture-snapshot-journey@example.com",
                tokenSuffix: "journey",
                windowScanAt: windowScan,
                allTimeNewerScanAt: newerOutsideWindowScan,
                journeyFeedbackAt: journeyAt,
                guestName: "Journey Guest"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var placement = body.GetProperty("placements")[0];
            // All-time last scan is max of window + outside-window scans.
            Assert.Equal(
                newerOutsideWindowScan,
                placement.GetProperty("lastScanAt").GetDateTime()
            );

            var lastJourney = body.GetProperty("lastJourneyUpdate");
            Assert.Equal(JsonValueKind.Object, lastJourney.ValueKind);
            Assert.Equal(
                journeyAt,
                lastJourney.GetProperty("createdAt").GetDateTime()
            );
            Assert.Equal(
                "Journey Guest",
                lastJourney.GetProperty("guestName").GetString()
            );
        }

        [Fact]
        public async Task Snapshot_PlacementWithPaidShopOrderOfType_HasShopPrintedMaterialsTrue()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedShopPrintedPlacementsAsync(
                email: "capture-snapshot-shop-printed@example.com",
                tokenSuffix: "shopprint"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var placements = body.GetProperty("placements");

            // Table Tent: paid order at this location. Guest Card: only an
            // awaiting-payment order. Smart Guest: no order at all.
            Assert.True(
                PlacementByType(placements, "TableTent")
                    .GetProperty("hasShopPrintedMaterials")
                    .GetBoolean()
            );
            Assert.False(
                PlacementByType(placements, "GuestCard")
                    .GetProperty("hasShopPrintedMaterials")
                    .GetBoolean()
            );
            Assert.False(
                PlacementByType(placements, "SmartGuest")
                    .GetProperty("hasShopPrintedMaterials")
                    .GetBoolean()
            );
        }

        [Fact]
        public async Task Snapshot_PaidGuestCardLine_FlagsGuestCardCode()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-snapshot-paid-guest-card@example.com",
                tokenSuffix: "paidgc"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var location = await context.RestaurantLocations
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == seeded.LocationId);
                var ownerUserId = await context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == location.RestaurantId)
                    .Select(row => row.OwnerUserId)
                    .SingleAsync();

                context.QrCodes.Add(
                    new QrCode
                    {
                        RestaurantLocationId = location.Id,
                        QrType = QrType.GuestCard,
                        Token = "cap-snap-paidgc-gc-token123",
                        Status = QrCodeStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                context.ShopOrders.Add(
                    NewShopOrderForLocation(
                        location,
                        ownerUserId,
                        ShopPaymentStatuses.Paid,
                        "qr-guest-cards",
                        "QR Guest Cards"
                    )
                );
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var placements = body.GetProperty("placements");
            Assert.True(
                PlacementByType(placements, "GuestCard")
                    .GetProperty("hasShopPrintedMaterials")
                    .GetBoolean()
            );
            // The owner location's default Smart Guest code has no order.
            Assert.False(
                PlacementByType(placements, "SmartGuest")
                    .GetProperty("hasShopPrintedMaterials")
                    .GetBoolean()
            );
        }

        [Fact]
        public async Task Snapshot_PaidShopOrderAtAnotherLocation_DoesNotFlagThisLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-snapshot-other-site@example.com",
                tokenSuffix: "othersite"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var location = await context.RestaurantLocations
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == seeded.LocationId);
                var restaurant = await context.Restaurants
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == location.RestaurantId);

                // Same restaurant, second site. The paid order belongs to it.
                var otherSite = new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Other Site",
                    Address = "2 High Street",
                    CreatedAt = DateTime.UtcNow,
                };
                context.RestaurantLocations.Add(otherSite);
                await context.SaveChangesAsync();

                context.QrCodes.Add(
                    new QrCode
                    {
                        RestaurantLocationId = location.Id,
                        QrType = QrType.TableTent,
                        Token = "cap-snap-othersite-tt-token123",
                        Status = QrCodeStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                context.ShopOrders.Add(
                    NewShopOrderForLocation(
                        otherSite,
                        restaurant.OwnerUserId,
                        ShopPaymentStatuses.Paid,
                        "table-tents",
                        "Table Tent"
                    )
                );
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(
                PlacementByType(body.GetProperty("placements"), "TableTent")
                    .GetProperty("hasShopPrintedMaterials")
                    .GetBoolean()
            );
        }

        [Fact]
        public async Task Snapshot_DuplicateSkuIdWithDeletedRow_StillFlagsPaidLine()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedShopPrintedPlacementsAsync(
                email: "capture-snapshot-dup-sku@example.com",
                tokenSuffix: "dupsku"
            );

            // Admin soft-deletes a SKU and recreates it under the same id, so
            // the catalog holds a Deleted row and a live row for "table-tents".
            var deletedRowId = await AddDeletedSkuRowAsync(
                "table-tents",
                "WindowSticker"
            );
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    SnapshotUrl(seeded.LocationId, from, to)
                );
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);

                var response = await _client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var body = await ReadJsonAsync(response);
                Assert.True(
                    PlacementByType(body.GetProperty("placements"), "TableTent")
                        .GetProperty("hasShopPrintedMaterials")
                        .GetBoolean()
                );
            }
            finally
            {
                await RemoveSkuRowAsync(deletedRowId);
            }
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_AllowsExact180DaySpan()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-snapshot-span-ok@example.com",
                tokenSuffix: "spanok"
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(180);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_Returns400_WhenFromMissing()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-snapshot-missing-from@example.com",
                tokenSuffix: "missfrom"
            );
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/locations/{seeded.LocationId}/snapshot?to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_Returns400_WhenFromNotBeforeTo()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-snapshot-from-gte@example.com",
                tokenSuffix: "fromgte"
            );
            var instant = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, instant, instant)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_Returns400_WhenSpanExceeds180Days()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-snapshot-span-max@example.com",
                tokenSuffix: "spanmax"
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(181);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerLocationAsync(
                email: "capture-snapshot-owner-a@example.com",
                tokenSuffix: "ownera"
            );
            var other = await SeedOwnerLocationAsync(
                email: "capture-snapshot-owner-b@example.com",
                tokenSuffix: "ownerb"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SnapshotUrl(other.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocationSnapshot_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(SnapshotUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePerformance_NoLongerReturnsSuccessfulKpiPayload()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-perf-retired@example.com",
                tokenSuffix: "perfret"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/performance?locationId={seeded.LocationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePlacements_NoLongerReturnsSuccessfulListPayload()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-placements-retired@example.com",
                tokenSuffix: "placret"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/placements?locationId={seeded.LocationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        private static string SnapshotUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/capture/locations/{locationId}/snapshot?from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
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

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerLocationAsync(
            string email,
            string tokenSuffix
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Snapshot Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900900",
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
                Name = "Capture Snapshot Venue",
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

            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.SmartGuest,
                    Token = $"cap-snap-{tokenSuffix}-sg-token123",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<(string Jwt, int LocationId)> SeedCaptureFactsAsync(
            string email,
            string tokenSuffix,
            DateTime currentScanAt,
            DateTime previousScanAt,
            DateTime currentFeedbackAt,
            DateTime previousFeedbackAt,
            bool currentFeedbackOffersOptOut,
            bool previousFeedbackOffersOptOut,
            DateTime? extraScanAtBoundary = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Snapshot Facts Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900901",
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
                Name = "Capture Snapshot Facts Venue",
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

            var qrCode = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-snap-{tokenSuffix}-sg-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qrCode);
            await context.SaveChangesAsync();

            context.QrScanEvents.Add(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    CreatedAt = currentScanAt,
                }
            );
            context.QrScanEvents.Add(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    CreatedAt = previousScanAt,
                }
            );
            if (extraScanAtBoundary != null)
            {
                context.QrScanEvents.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = location.Id,
                        QrCodeId = qrCode.Id,
                        CreatedAt = extraScanAtBoundary.Value,
                    }
                );
            }

            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    GuestName = "Current Guest",
                    GuestContact = "current@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Current",
                    OffersOptOut = currentFeedbackOffersOptOut,
                    CreatedAt = currentFeedbackAt,
                }
            );
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    GuestName = "Previous Guest",
                    GuestContact = "previous@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Previous",
                    OffersOptOut = previousFeedbackOffersOptOut,
                    CreatedAt = previousFeedbackAt,
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<(string Jwt, int LocationId)> SeedActiveAndArchivedActivityAsync(
            string email,
            string tokenSuffix
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Snapshot Arch Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900902",
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
                Name = "Capture Snapshot Arch Venue",
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

            var active = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"cap-snap-{tokenSuffix}-active-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var paused = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.PackagingSticker,
                Token = $"cap-snap-{tokenSuffix}-paused-token123",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.WindowSticker,
                Token = $"cap-snap-{tokenSuffix}-archived-token123",
                Status = QrCodeStatus.Archived,
                CreatedAt = DateTime.UtcNow,
                ArchivedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(active, paused, archived);
            await context.SaveChangesAsync();

            var windowAt = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

            // Active: 2 scans, 1 opted-in feedback
            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = active.Id,
                    CreatedAt = windowAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = active.Id,
                    CreatedAt = windowAt.AddHours(1),
                }
            );
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = active.Id,
                    GuestName = "Active Guest",
                    GuestContact = "active@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Active",
                    OffersOptOut = false,
                    CreatedAt = windowAt.AddHours(2),
                }
            );

            // Paused: 1 scan, 1 opted-out feedback
            context.QrScanEvents.Add(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = paused.Id,
                    CreatedAt = windowAt,
                }
            );
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = paused.Id,
                    GuestName = "Paused Guest",
                    GuestContact = "paused@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Paused",
                    OffersOptOut = true,
                    CreatedAt = windowAt.AddHours(1),
                }
            );

            // Archived: 3 scans, 2 opted-in feedback — must be excluded
            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = windowAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = windowAt.AddMinutes(10),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = windowAt.AddMinutes(20),
                }
            );
            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    GuestName = "Archived A",
                    GuestContact = "archa@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Archived",
                    OffersOptOut = false,
                    CreatedAt = windowAt.AddHours(1),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    GuestName = "Archived B",
                    GuestContact = "archb@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Archived",
                    OffersOptOut = false,
                    CreatedAt = windowAt.AddHours(2),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<(string Jwt, int LocationId)> SeedLastScanAndJourneyAsync(
            string email,
            string tokenSuffix,
            DateTime windowScanAt,
            DateTime allTimeNewerScanAt,
            DateTime journeyFeedbackAt,
            string guestName
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Snapshot Journey Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900903",
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
                Name = "Capture Snapshot Journey Venue",
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

            var qrCode = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-snap-{tokenSuffix}-sg-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qrCode);
            await context.SaveChangesAsync();

            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    CreatedAt = windowScanAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    CreatedAt = allTimeNewerScanAt,
                }
            );
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    GuestName = guestName,
                    GuestContact = "journey@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Journey",
                    OffersOptOut = false,
                    CreatedAt = journeyFeedbackAt,
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<(string Jwt, int LocationId)> SeedShopPrintedPlacementsAsync(
            string email,
            string tokenSuffix
        )
        {
            var seeded = await SeedOwnerLocationAsync(email, tokenSuffix);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.LocationId);
            var owner = await context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == location.RestaurantId)
                .Select(row => new { row.OwnerUserId })
                .SingleAsync();

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.TableTent,
                    Token = $"cap-snap-{tokenSuffix}-tt-token123",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.GuestCard,
                    Token = $"cap-snap-{tokenSuffix}-gc-token123",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            context.ShopOrders.AddRange(
                NewShopOrderForLocation(
                    location,
                    owner.OwnerUserId,
                    ShopPaymentStatuses.Paid,
                    "table-tents",
                    "Table Tent"
                ),
                NewShopOrderForLocation(
                    location,
                    owner.OwnerUserId,
                    ShopPaymentStatuses.AwaitingPayment,
                    "qr-guest-cards",
                    "QR Guest Cards"
                )
            );
            await context.SaveChangesAsync();

            return seeded;
        }

        private static ShopOrder NewShopOrderForLocation(
            RestaurantLocation location,
            int placedByUserId,
            string paymentStatus,
            string catalogSkuId,
            string title
        )
        {
            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"SNAP-{Guid.NewGuid():N}"[..16],
                RestaurantId = location.RestaurantId,
                LocationId = location.Id,
                LocationNameSnapshot = location.LocationName,
                PlacedByUserId = placedByUserId,
                PlacedByNameSnapshot = "Capture Snapshot Owner",
                MaterialsNetPence = 1000,
                VatPence = 200,
                DeliveryNetPence = 0,
                GrossPence = 1200,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = paymentStatus,
                ShipToContactName = "Ada Lovelace",
                ShipToAddressLine1 = "6 Southwark Street",
                ShipToPostcode = "SE1 1TQ",
                ShipToCountry = "United Kingdom",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            order.Lines.Add(
                new ShopOrderLine
                {
                    Id = Guid.NewGuid(),
                    CatalogSkuId = catalogSkuId,
                    TitleSnapshot = title,
                    MaterialType = catalogSkuId,
                    Quantity = 1,
                    UnitNetPence = 1000,
                    LineNetPence = 1000,
                }
            );
            return order;
        }

        private async Task<int> AddDeletedSkuRowAsync(string skuId, string qrType)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            // Same recipe as the live row: the catalog load includes
            // PlacementRecipe, so a dangling recipe id would hide this row.
            var live = await context.ShopMaterialSkus
                .AsNoTracking()
                .SingleAsync(item =>
                    item.SkuId == skuId
                    && item.Status != ShopMaterialSkuStatuses.Deleted
                );
            var row = new ShopMaterialSku
            {
                SkuId = skuId,
                Title = "Retired Table Tent",
                Category = "tabletop",
                Description = "Soft-deleted before the SKU was recreated.",
                Material = "3mm Matte Frosted Acrylic",
                Dimensions = "105mm x 148mm (A6)",
                QrType = qrType,
                PlacementRecipeId = live.PlacementRecipeId,
                Status = ShopMaterialSkuStatuses.Deleted,
            };
            context.ShopMaterialSkus.Add(row);
            await context.SaveChangesAsync();
            return row.Id;
        }

        private async Task RemoveSkuRowAsync(int skuRowId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var row = await context.ShopMaterialSkus
                .SingleAsync(item => item.Id == skuRowId);
            context.ShopMaterialSkus.Remove(row);
            await context.SaveChangesAsync();
        }

        private static JsonElement PlacementByType(
            JsonElement placements,
            string qrType
        )
        {
            return placements
                .EnumerateArray()
                .Single(item => item.GetProperty("qrType").GetString() == qrType);
        }
    }
}
