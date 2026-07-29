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
    public class CaptureOverviewEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CaptureOverviewEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCaptureOverview_ReturnsAggregatedTotals_AndPreviousPeriod()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedMultiLocationFactsAsync(
                email: "capture-overview-totals@example.com",
                tokenSuffix: "overview-totals"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(2, body.GetProperty("activeLocations").GetInt32());
            Assert.Equal(2, body.GetProperty("totalLocations").GetInt32());
            Assert.Equal(2, body.GetProperty("activeQrPlacements").GetInt32());
            Assert.Equal(2, body.GetProperty("qrScans").GetInt32());
            Assert.Equal(1, body.GetProperty("qrScansPrevious").GetInt32());
            Assert.Equal(2, body.GetProperty("feedbackSubmitted").GetInt32());
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
        public async Task GetCaptureOverview_ExcludesArchivedQrActivity_AcrossLocations()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedArchivedActivityAcrossLocationsAsync(
                email: "capture-overview-archived@example.com",
                tokenSuffix: "overview-arch"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("qrScans").GetInt32());
            Assert.Equal(1, body.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, body.GetProperty("marketingOptIns").GetInt32());
        }

        [Fact]
        public async Task GetCaptureOverview_IncludesPausedQrActivity_ExcludesArchived()
        {
            // Active (1 scan, 1 opted-in) + Paused (1 scan, 1 opted-out) only;
            // Archived activity in the same window must be excluded.
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedActivePausedArchivedActivityAsync(
                email: "capture-overview-paused-incl@example.com",
                tokenSuffix: "overview-paused"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
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
        public async Task GetCaptureOverview_CountsActiveQrPlacementsOnly()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedMixedQrStatusesAsync(
                email: "capture-overview-active-qr@example.com",
                tokenSuffix: "overview-qr"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("activeQrPlacements").GetInt32());
        }

        [Fact]
        public async Task GetCaptureOverview_ReturnsZeroOfferClaims_WithoutRealData()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationsAsync(
                email: "capture-overview-offers@example.com",
                tokenSuffix: "overview-offers",
                locationCount: 1
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
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
        public async Task GetCaptureOverview_Returns400_WhenFromMissing()
        {
            var seeded = await SeedOwnerWithLocationsAsync(
                email: "capture-overview-miss-from@example.com",
                tokenSuffix: "overview-missfrom",
                locationCount: 1
            );
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/overview?to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureOverview_Returns400_WhenFromNotBeforeTo()
        {
            var seeded = await SeedOwnerWithLocationsAsync(
                email: "capture-overview-from-gte@example.com",
                tokenSuffix: "overview-fromgte",
                locationCount: 1
            );
            var instant = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(instant, instant)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureOverview_Returns400_WhenSpanExceeds180Days()
        {
            var seeded = await SeedOwnerWithLocationsAsync(
                email: "capture-overview-span-max@example.com",
                tokenSuffix: "overview-spanmax",
                locationCount: 1
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(181);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureOverview_AllowsExact180DaySpan()
        {
            var seeded = await SeedOwnerWithLocationsAsync(
                email: "capture-overview-span-ok@example.com",
                tokenSuffix: "overview-spanok",
                locationCount: 1
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(180);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task GetCaptureOverview_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(OverviewUrl(from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureOverview_ReturnsZeros_WhenNoRestaurant()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var jwt = await SeedUserWithoutRestaurantAsync(
                email: "capture-overview-no-rest@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                OverviewUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("activeLocations").GetInt32());
            Assert.Equal(0, body.GetProperty("totalLocations").GetInt32());
        }

        private static string OverviewUrl(DateTime from, DateTime to)
        {
            return $"/api/capture/overview?from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
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

        private async Task<(string Jwt, int LocationAId, int LocationBId)>
            SeedMultiLocationFactsAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Overview Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900910",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Capture Overview Group",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var locationA = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 Camden High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var locationB = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                Address = "2 Soho Square",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(locationA, locationB);
            await context.SaveChangesAsync();

            var qrA = new QrCode
            {
                RestaurantLocationId = locationA.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-overview-{tokenSuffix}-a-sg",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var qrB = new QrCode
            {
                RestaurantLocationId = locationB.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-overview-{tokenSuffix}-b-sg",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(qrA, qrB);
            await context.SaveChangesAsync();

            var currentAt = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
            var previousAt = new DateTime(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc);

            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    CreatedAt = currentAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = locationB.Id,
                    QrCodeId = qrB.Id,
                    CreatedAt = currentAt.AddHours(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    CreatedAt = previousAt,
                }
            );

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    GuestName = "Current A",
                    GuestContact = "a@example.com",
                    ContactType = ContactType.Email,
                    Comment = "A current",
                    OffersOptOut = false,
                    CreatedAt = currentAt.AddMinutes(30),
                },
                new Feedback
                {
                    RestaurantLocationId = locationB.Id,
                    QrCodeId = qrB.Id,
                    GuestName = "Current B",
                    GuestContact = "b@example.com",
                    ContactType = ContactType.Email,
                    Comment = "B current",
                    OffersOptOut = true,
                    CreatedAt = currentAt.AddHours(2),
                },
                new Feedback
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    GuestName = "Previous A",
                    GuestContact = "prev@example.com",
                    ContactType = ContactType.Email,
                    Comment = "A previous",
                    OffersOptOut = true,
                    CreatedAt = previousAt.AddMinutes(30),
                }
            );

            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, locationA.Id, locationB.Id);
        }

        private async Task<(string Jwt, int LocationId)> SeedArchivedActivityAcrossLocationsAsync(
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
                FullName = "Capture Overview Archived Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900911",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Capture Overview Archived",
                AccountType = "Multi",
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
                Token = $"cap-overview-{tokenSuffix}-active",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"cap-overview-{tokenSuffix}-archived",
                Status = QrCodeStatus.Archived,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(active, archived);
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
                    QrCodeId = archived.Id,
                    CreatedAt = inWindow.AddHours(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = inWindow.AddHours(2),
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
                    Comment = "Active",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddMinutes(15),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    GuestName = "Archived Guest",
                    GuestContact = "archived@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Archived",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddHours(1).AddMinutes(15),
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

        private async Task<(string Jwt, int LocationId)> SeedActivePausedArchivedActivityAsync(
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
                FullName = "Capture Overview Paused Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900915",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Capture Overview Paused Venue",
                AccountType = "Multi",
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
                Token = $"cap-overview-{tokenSuffix}-active",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var paused = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"cap-overview-{tokenSuffix}-paused",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.WindowSticker,
                Token = $"cap-overview-{tokenSuffix}-archived",
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
                    QrCodeId = paused.Id,
                    CreatedAt = inWindow.AddHours(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = inWindow.AddHours(2),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    CreatedAt = inWindow.AddHours(3),
                }
            );

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = active.Id,
                    GuestName = "Active Guest",
                    GuestContact = "active-paused@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Active",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddMinutes(15),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = paused.Id,
                    GuestName = "Paused Guest",
                    GuestContact = "paused@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Paused",
                    OffersOptOut = true,
                    CreatedAt = inWindow.AddHours(1).AddMinutes(15),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = archived.Id,
                    GuestName = "Archived Guest",
                    GuestContact = "archived-paused@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Archived",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddHours(2).AddMinutes(15),
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

        private async Task<(string Jwt, int LocationId)> SeedMixedQrStatusesAsync(
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
                FullName = "Capture Overview QR Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900912",
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
                Name = "Capture Overview QR Venue",
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

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.SmartGuest,
                    Token = $"cap-overview-{tokenSuffix}-active",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.CounterCard,
                    Token = $"cap-overview-{tokenSuffix}-paused",
                    Status = QrCodeStatus.Paused,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.WindowSticker,
                    Token = $"cap-overview-{tokenSuffix}-archived",
                    Status = QrCodeStatus.Archived,
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

        private async Task<(string Jwt, int LocationId)> SeedOwnerWithLocationsAsync(
            string email,
            string tokenSuffix,
            int locationCount
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Overview Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900913",
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
                Name = "Capture Overview Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            RestaurantLocation? firstLocation = null;
            for (var i = 0; i < locationCount; i++)
            {
                var location = new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = $"Location {i + 1}",
                    Address = $"{i + 1} High Street",
                    CreatedAt = DateTime.UtcNow,
                };
                context.RestaurantLocations.Add(location);
                firstLocation ??= location;
            }
            await context.SaveChangesAsync();

            if (firstLocation != null)
            {
                context.QrCodes.Add(
                    new QrCode
                    {
                        RestaurantLocationId = firstLocation.Id,
                        QrType = QrType.SmartGuest,
                        Token = $"cap-overview-{tokenSuffix}-sg",
                        Status = QrCodeStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, firstLocation?.Id ?? 0);
        }

        private async Task<string> SeedUserWithoutRestaurantAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "No Restaurant Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900914",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
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
