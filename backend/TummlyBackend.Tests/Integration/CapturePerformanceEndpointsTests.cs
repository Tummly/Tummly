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
    public class CapturePerformanceEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CapturePerformanceEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCapturePerformance_ReturnsWindowedTotals_AndPreviousPeriod()
        {
            // Current [Jul 10, Jul 17); previous equal span [Jul 3, Jul 10).
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedCaptureFactsAsync(
                email: "capture-perf-totals@example.com",
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
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
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
        }

        [Fact]
        public async Task GetCapturePerformance_SumsAcrossQrTypes_ForLocationTotals()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedMultiTypeScansAndFeedbackAsync(
                email: "capture-perf-sum@example.com",
                tokenSuffix: "sumtypes"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(2, body.GetProperty("qrScans").GetInt32());
            Assert.Equal(2, body.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, body.GetProperty("marketingOptIns").GetInt32());
        }

        [Fact]
        public async Task GetCapturePerformance_ReturnsZeroScans_WithFeedbackInWindow()
        {
            // Form starts is derived client-side as feedback÷scans; 0 scans → "—".
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedFeedbackWithoutScansAsync(
                email: "capture-perf-zero-scans@example.com",
                tokenSuffix: "zeroscans",
                feedbackCreatedAt: new DateTime(
                    2026,
                    7,
                    14,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                )
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("qrScans").GetInt32());
            Assert.Equal(1, body.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, body.GetProperty("marketingOptIns").GetInt32());
        }

        [Fact]
        public async Task GetCapturePerformance_ExcludesArchivedQrActivity_AndMatchesPlacementRowSums()
        {
            // Archived QR windowed activity must not inflate location KPIs;
            // Active/Paused totals should equal the sum of placement rows.
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedActiveAndArchivedActivityAsync(
                email: "capture-perf-archived-excluded@example.com",
                tokenSuffix: "archexcl"
            );

            using var performanceRequest = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            performanceRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var performanceResponse = await _client.SendAsync(performanceRequest);

            Assert.Equal(HttpStatusCode.OK, performanceResponse.StatusCode);
            var performanceBody = await ReadJsonAsync(performanceResponse);

            // Active (2 scans, 1 opted-in feedback) + Paused (1 scan, 1 opted-out
            // feedback) only; the Archived QR's 3 scans / 2 opted-in feedback in
            // the same window must be excluded.
            Assert.Equal(3, performanceBody.GetProperty("qrScans").GetInt32());
            Assert.Equal(
                2,
                performanceBody.GetProperty("feedbackSubmitted").GetInt32()
            );
            Assert.Equal(
                1,
                performanceBody.GetProperty("marketingOptIns").GetInt32()
            );
            Assert.Equal(
                0,
                performanceBody.GetProperty("offerClaims").GetInt32()
            );
            Assert.False(
                performanceBody
                    .GetProperty("offerClaimsHasRealData")
                    .GetBoolean()
            );

            using var placementsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                PlacementsUrl(seeded.LocationId, from, to)
            );
            placementsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var placementsResponse = await _client.SendAsync(placementsRequest);

            Assert.Equal(HttpStatusCode.OK, placementsResponse.StatusCode);
            var placementsBody = await ReadJsonAsync(placementsResponse);
            var placements = placementsBody.GetProperty("placements");

            var sumScans = 0;
            var sumFeedback = 0;
            var sumOptIns = 0;
            foreach (var item in placements.EnumerateArray())
            {
                // Archived row must never appear in the placements list.
                Assert.NotEqual(
                    "Archived",
                    item.GetProperty("status").GetString()
                );
                sumScans += item.GetProperty("qrScans").GetInt32();
                sumFeedback += item.GetProperty("feedbackSubmitted").GetInt32();
                sumOptIns += item.GetProperty("marketingOptIns").GetInt32();
            }

            Assert.Equal(
                performanceBody.GetProperty("qrScans").GetInt32(),
                sumScans
            );
            Assert.Equal(
                performanceBody.GetProperty("feedbackSubmitted").GetInt32(),
                sumFeedback
            );
            Assert.Equal(
                performanceBody.GetProperty("marketingOptIns").GetInt32(),
                sumOptIns
            );
        }

        [Fact]
        public async Task GetCapturePerformance_ReturnsZeroOfferClaims_WithoutRealData()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-perf-offers@example.com",
                tokenSuffix: "offers"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("offerClaims").GetInt32());
            Assert.False(body.GetProperty("offerClaimsHasRealData").GetBoolean());
        }

        [Fact]
        public async Task GetCapturePerformance_Returns400_WhenFromMissing()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-perf-missing-from@example.com",
                tokenSuffix: "missfrom"
            );
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/performance?locationId={seeded.LocationId}&to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePerformance_Returns400_WhenFromNotBeforeTo()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-perf-from-gte@example.com",
                tokenSuffix: "fromgte"
            );
            var instant = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, instant, instant)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePerformance_Returns400_WhenSpanExceeds180Days()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-perf-span-max@example.com",
                tokenSuffix: "spanmax"
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(181);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePerformance_AllowsExact180DaySpan()
        {
            var seeded = await SeedOwnerLocationAsync(
                email: "capture-perf-span-ok@example.com",
                tokenSuffix: "spanok"
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(180);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task GetCapturePerformance_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerLocationAsync(
                email: "capture-perf-owner-a@example.com",
                tokenSuffix: "ownera"
            );
            var other = await SeedOwnerLocationAsync(
                email: "capture-perf-owner-b@example.com",
                tokenSuffix: "ownerb"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(other.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePerformance_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(PerformanceUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static string PerformanceUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/capture/performance?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string PlacementsUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/capture/placements?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
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
                FullName = "Capture Perf Owner",
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
                Name = "Capture Perf Venue",
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
                    Token = $"cap-perf-{tokenSuffix}-sg-token123",
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
                FullName = "Capture Perf Facts Owner",
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
                Name = "Capture Perf Facts Venue",
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
                Token = $"cap-perf-{tokenSuffix}-sg-token123",
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

        private async Task<(string Jwt, int LocationId)> SeedMultiTypeScansAndFeedbackAsync(
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
                FullName = "Capture Perf Sum Owner",
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
                Name = "Capture Perf Sum Venue",
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

            var smartGuest = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-perf-{tokenSuffix}-sg-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var counter = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"cap-perf-{tokenSuffix}-cc-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(smartGuest, counter);
            await context.SaveChangesAsync();

            var inWindow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = smartGuest.Id,
                    CreatedAt = inWindow,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = counter.Id,
                    CreatedAt = inWindow.AddHours(1),
                }
            );
            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = smartGuest.Id,
                    GuestName = "Smart Guest",
                    GuestContact = "smart@example.com",
                    ContactType = ContactType.Email,
                    Comment = "From smart guest",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddMinutes(30),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = counter.Id,
                    GuestName = "Counter Guest",
                    GuestContact = "counter@example.com",
                    ContactType = ContactType.Email,
                    Comment = "From counter",
                    OffersOptOut = true,
                    CreatedAt = inWindow.AddHours(2),
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
                FullName = "Capture Perf Archived Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900904",
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
                Name = "Capture Perf Archived Venue",
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
                QrType = QrType.SmartGuest,
                Token = $"cap-perf-{tokenSuffix}-sg-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var paused = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"cap-perf-{tokenSuffix}-cc-token123",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.WindowSticker,
                Token = $"cap-perf-{tokenSuffix}-ws-token123",
                Status = QrCodeStatus.Archived,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(active, paused, archived);
            await context.SaveChangesAsync();

            var inWindow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = active.Id,
                    CreatedAt = inWindow,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = active.Id,
                    CreatedAt = inWindow.AddHours(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = paused.Id,
                    CreatedAt = inWindow.AddHours(2),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = inWindow.AddHours(3),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = inWindow.AddHours(4),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = inWindow.AddHours(5),
                }
            );

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = active.Id,
                    GuestName = "Active Guest",
                    GuestContact = "active@example.com",
                    ContactType = ContactType.Email,
                    Comment = "From active",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddMinutes(30),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = paused.Id,
                    GuestName = "Paused Guest",
                    GuestContact = "paused@example.com",
                    ContactType = ContactType.Email,
                    Comment = "From paused",
                    OffersOptOut = true,
                    CreatedAt = inWindow.AddHours(2).AddMinutes(15),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    GuestName = "Archived Guest One",
                    GuestContact = "archived1@example.com",
                    ContactType = ContactType.Email,
                    Comment = "From archived one",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddHours(3).AddMinutes(15),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    GuestName = "Archived Guest Two",
                    GuestContact = "archived2@example.com",
                    ContactType = ContactType.Email,
                    Comment = "From archived two",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddHours(4).AddMinutes(15),
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

        private async Task<(string Jwt, int LocationId)> SeedFeedbackWithoutScansAsync(
            string email,
            string tokenSuffix,
            DateTime feedbackCreatedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Perf Zero Scans Owner",
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
                Name = "Capture Perf Zero Scans Venue",
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
                Token = $"cap-perf-{tokenSuffix}-sg-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qrCode);
            await context.SaveChangesAsync();

            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = qrCode.Id,
                    GuestName = "No Scan Guest",
                    GuestContact = "noscan@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Feedback without scan in window",
                    OffersOptOut = false,
                    CreatedAt = feedbackCreatedAt,
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

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
