using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CaptureArchiveListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CaptureArchiveListEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetArchived_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/capture/placements/archived"
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetArchived_ReturnsEmptyPage_WhenNoRestaurant()
        {
            var jwt = await SeedUserWithoutRestaurantAsync(
                "cap-arch-no-rest@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("placements").GetArrayLength());
            Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());
            Assert.Equal(
                0,
                body.GetProperty("archiverOptions").GetArrayLength()
            );
        }

        [Fact]
        public async Task GetArchived_Returns400_WhenPageSizeNot25()
        {
            var seeded = await SeedOwnerWithArchivedAsync(
                "cap-arch-pagesize@example.com",
                "cap-arch-pagesize"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?pageSize=50"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetArchived_Returns400_WhenSortInvalid()
        {
            var seeded = await SeedOwnerWithArchivedAsync(
                "cap-arch-badsort@example.com",
                "cap-arch-badsort"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?sort=not-a-sort"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetArchived_PagesAndReportsTotalCount()
        {
            var seeded = await SeedOwnerWithManyArchivedAsync(
                "cap-arch-page@example.com",
                "cap-arch-page",
                count: 3
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?page=1&pageSize=25"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(3, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(3, body.GetProperty("placements").GetArrayLength());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());
        }

        [Fact]
        public async Task GetArchived_FiltersByQrTypeAndSearch()
        {
            var seeded = await SeedOwnerWithMixedArchivedAsync(
                "cap-arch-filter@example.com",
                "cap-arch-filter"
            );

            using var typeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?qrTypes=CounterCard"
            );
            typeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var typeResponse = await _client.SendAsync(typeRequest);
            var typeBody = await ReadJsonAsync(typeResponse);
            Assert.Equal(1, typeBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "CounterCard",
                typeBody.GetProperty("placements")[0]
                    .GetProperty("qrType")
                    .GetString()
            );

            using var searchRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?q=Window"
            );
            searchRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var searchResponse = await _client.SendAsync(searchRequest);
            var searchBody = await ReadJsonAsync(searchResponse);
            Assert.Equal(1, searchBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "WindowSticker",
                searchBody.GetProperty("placements")[0]
                    .GetProperty("qrType")
                    .GetString()
            );
        }

        [Fact]
        public async Task GetArchived_CustomDateRange_BoundsArchivedAtInclusively()
        {
            var seeded = await SeedOwnerWithArchivedAtAsync(
                "cap-arch-custom@example.com",
                "cap-arch-custom",
                archivedAt: new DateTime(
                    2026,
                    7,
                    30,
                    22,
                    0,
                    0,
                    DateTimeKind.Utc
                )
            );

            using var hitRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived"
                    + "?datePreset=custom"
                    + "&dateFrom=2026-07-30T00:00:00.000Z"
                    + "&dateTo=2026-07-31T00:00:00.000Z"
            );
            hitRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var hitBody = await ReadJsonAsync(
                await _client.SendAsync(hitRequest)
            );
            Assert.Equal(1, hitBody.GetProperty("totalCount").GetInt32());

            using var missRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived"
                    + "?datePreset=custom"
                    + "&dateFrom=2026-07-31T00:00:00.000Z"
                    + "&dateTo=2026-08-01T00:00:00.000Z"
            );
            missRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var missBody = await ReadJsonAsync(
                await _client.SendAsync(missRequest)
            );
            Assert.Equal(0, missBody.GetProperty("totalCount").GetInt32());

            using var invalidRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived"
                    + "?datePreset=custom"
                    + "&dateFrom=2026-07-30T00:00:00.000Z"
                    + "&dateTo=2026-07-30T00:00:00.000Z"
            );
            invalidRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var invalidResponse = await _client.SendAsync(invalidRequest);
            Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        }

        [Fact]
        public async Task GetArchived_TodayPreset_ChangesMatchWithOffset()
        {
            var now = DateTime.UtcNow;
            var archivedAt = now.AddMinutes(-5);

            var seeded = await SeedOwnerWithArchivedAtAsync(
                "cap-arch-offset@example.com",
                "cap-arch-offset",
                archivedAt
            );

            using var includeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?datePreset=today&utcOffsetMinutes=0"
            );
            includeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var includeBody = await ReadJsonAsync(
                await _client.SendAsync(includeRequest)
            );
            Assert.Equal(1, includeBody.GetProperty("totalCount").GetInt32());

            // Offset so local "now" is ~00:01 on the next UTC calendar day —
            // local today then starts at approximately utcNow - 1m, excluding
            // an ArchivedAt five minutes ago.
            var nextUtcMidnight = DateTime.SpecifyKind(
                now.Date.AddDays(1),
                DateTimeKind.Utc
            );
            var offsetMinutes =
                (int)(nextUtcMidnight - now).TotalMinutes + 1;

            using var excludeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/placements/archived?datePreset=today&utcOffsetMinutes={offsetMinutes}"
            );
            excludeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var excludeBody = await ReadJsonAsync(
                await _client.SendAsync(excludeRequest)
            );
            Assert.Equal(0, excludeBody.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetArchived_MetricSort_OrdersByScanCountBeforePaging()
        {
            var seeded = await SeedOwnerWithMetricArchivedAsync(
                "cap-arch-metrics@example.com",
                "cap-arch-metrics"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?sort=highest-qr-scans"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var placements = body.GetProperty("placements");
            Assert.Equal(2, placements.GetArrayLength());
            Assert.Equal(5, placements[0].GetProperty("qrScans").GetInt32());
            Assert.Equal(1, placements[1].GetProperty("qrScans").GetInt32());
            Assert.Equal(
                seeded.HighScanQrCodeId,
                placements[0].GetProperty("qrCodeId").GetInt32()
            );
        }

        [Fact]
        public async Task GetArchived_CanRestoreFalse_WhenCatalogSlotOccupied()
        {
            var seeded = await SeedOwnerWithConflictAsync(
                "cap-arch-conflict@example.com",
                "cap-arch-conflict"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var row = body.GetProperty("placements")[0];
            Assert.False(row.GetProperty("canRestore").GetBoolean());
        }

        [Fact]
        public async Task GetArchived_ArchiverOptions_IncludeOffPageNames()
        {
            var seeded = await SeedOwnerWithManyArchivedAsync(
                "cap-arch-facets@example.com",
                "cap-arch-facets",
                count: 2,
                archiverNames: ["Ada Lovelace", "Grace Hopper"]
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/capture/placements/archived?qrTypes=CounterCard"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var body = await ReadJsonAsync(await _client.SendAsync(request));

            // Filtered page may be empty or partial; facets stay unfiltered.
            var options = body.GetProperty("archiverOptions")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToList();
            Assert.Contains("Ada Lovelace", options);
            Assert.Contains("Grace Hopper", options);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
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
                FullName = "No Restaurant",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900100",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role);
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerWithArchivedAsync(
            string email,
            string tokenSuffix
        )
        {
            return await SeedOwnerWithArchivedAtAsync(
                email,
                tokenSuffix,
                DateTime.UtcNow.AddDays(-1)
            );
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithArchivedAtAsync(
            string email,
            string tokenSuffix,
            DateTime archivedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Archive Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900101",
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
                Name = "Archive Venue",
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
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.WindowSticker,
                    Token = $"{tokenSuffix}-window-token123",
                    Status = QrCodeStatus.Archived,
                    ArchivedAt = archivedAt,
                    ArchivedByDisplayName = "Operator",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                }
            );
            await context.SaveChangesAsync();

            return (jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role), location.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithManyArchivedAsync(
            string email,
            string tokenSuffix,
            int count,
            string[]? archiverNames = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Archive Owner Many",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900102",
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
                Name = "Archive Venue Many",
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
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var types = new[]
            {
                QrType.CounterCard,
                QrType.PackagingSticker,
                QrType.DeliveryInsert,
                QrType.WindowSticker,
                QrType.SmartGuest,
            };

            for (var i = 0; i < count; i++)
            {
                var archiver =
                    archiverNames != null && i < archiverNames.Length
                        ? archiverNames[i]
                        : $"Archiver {i}";
                context.QrCodes.Add(
                    new QrCode
                    {
                        RestaurantLocationId = location.Id,
                        QrType = types[i % types.Length],
                        Token = $"{tokenSuffix}-tok-{i}-abcdefgh",
                        Status = QrCodeStatus.Archived,
                        ArchivedAt = DateTime.UtcNow.AddDays(-i - 1),
                        ArchivedByDisplayName = archiver,
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                    }
                );
            }

            await context.SaveChangesAsync();
            return (jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role), location.Id);
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerWithMixedArchivedAsync(
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
                FullName = "Archive Mixed",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900103",
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
                Name = "Archive Mixed Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Downtown",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.CounterCard,
                    Token = $"{tokenSuffix}-counter-token12",
                    Status = QrCodeStatus.Archived,
                    ArchivedAt = DateTime.UtcNow.AddDays(-2),
                    ArchivedByDisplayName = "Ada",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.WindowSticker,
                    Token = $"{tokenSuffix}-window-token123",
                    Status = QrCodeStatus.Archived,
                    ArchivedAt = DateTime.UtcNow.AddDays(-1),
                    ArchivedByDisplayName = "Grace",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                }
            );
            await context.SaveChangesAsync();
            return (jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role), location.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int HighScanQrCodeId
        )> SeedOwnerWithMetricArchivedAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Archive Metrics",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900104",
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
                Name = "Archive Metrics Venue",
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
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var high = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"{tokenSuffix}-high-token12345",
                Status = QrCodeStatus.Archived,
                ArchivedAt = DateTime.UtcNow.AddDays(-5),
                ArchivedByDisplayName = "Ops",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
            };
            var low = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.PackagingSticker,
                Token = $"{tokenSuffix}-low-token123456",
                Status = QrCodeStatus.Archived,
                ArchivedAt = DateTime.UtcNow.AddDays(-1),
                ArchivedByDisplayName = "Ops",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
            };
            context.QrCodes.AddRange(high, low);
            await context.SaveChangesAsync();

            for (var i = 0; i < 5; i++)
            {
                context.QrScanEvents.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = location.Id,
                        QrCodeId = high.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                    }
                );
            }

            context.QrScanEvents.Add(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = low.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                }
            );
            await context.SaveChangesAsync();

            return (jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role), location.Id, high.Id);
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerWithConflictAsync(
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
                FullName = "Archive Conflict",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900105",
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
                Name = "Archive Conflict Venue",
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
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.CounterCard,
                    Token = $"{tokenSuffix}-archived-tok12",
                    Status = QrCodeStatus.Archived,
                    ArchivedAt = DateTime.UtcNow.AddDays(-1),
                    ArchivedByDisplayName = "Ops",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.CounterCard,
                    Token = $"{tokenSuffix}-active-token12",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
            return (jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role), location.Id);
        }
    }
}
