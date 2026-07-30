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
            var olderScan = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            var journeyAt = new DateTime(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc);

            var seeded = await SeedLastScanAndJourneyAsync(
                email: "capture-snapshot-journey@example.com",
                tokenSuffix: "journey",
                windowScanAt: windowScan,
                allTimeOlderScanAt: olderScan,
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
            // All-time last scan is max of window + older scans (not date-windowed).
            Assert.Equal(
                windowScan,
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
            DateTime allTimeOlderScanAt,
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
                    CreatedAt = allTimeOlderScanAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    CreatedAt = windowScanAt,
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
    }
}
