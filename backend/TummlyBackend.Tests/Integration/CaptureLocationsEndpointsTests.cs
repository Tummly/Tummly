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
    public class CaptureLocationsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CaptureLocationsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCaptureLocations_ReturnsPaginatedRows_WithMetrics()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedTwoLocationFactsAsync(
                email: "capture-locations-rows@example.com",
                tokenSuffix: "loc-rows"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(20, body.GetProperty("pageSize").GetInt32());

            var items = body.GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());

            // Default sort: Highest QR scans — Camden has 2, Soho has 1.
            var first = items[0];
            Assert.Equal(seeded.LocationAId, first.GetProperty("locationId").GetInt32());
            Assert.Equal("Camden", first.GetProperty("locationName").GetString());
            Assert.Equal("Active", first.GetProperty("status").GetString());
            Assert.Equal(1, first.GetProperty("activePlacementsCount").GetInt32());
            Assert.Equal(2, first.GetProperty("qrScans").GetInt32());
            Assert.Equal(2, first.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, first.GetProperty("marketingOptIns").GetInt32());
            Assert.Equal(0, first.GetProperty("offerClaims").GetInt32());
            Assert.False(first.TryGetProperty("submissionRate", out _));
            Assert.NotEqual(JsonValueKind.Null, first.GetProperty("lastActivityAt").ValueKind);

            var second = items[1];
            Assert.Equal(seeded.LocationBId, second.GetProperty("locationId").GetInt32());
            Assert.Equal("Soho", second.GetProperty("locationName").GetString());
            Assert.Equal(1, second.GetProperty("qrScans").GetInt32());
            Assert.Equal(1, second.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(0, second.GetProperty("marketingOptIns").GetInt32());
            Assert.Equal(0, second.GetProperty("offerClaims").GetInt32());
        }

        [Fact]
        public async Task GetCaptureLocations_IncludesPausedQrEngagement_AndLastActivity_ExcludesArchived()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedActivePausedArchivedAsync(
                email: "capture-locations-paused@example.com",
                tokenSuffix: "loc-paused"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var item = body.GetProperty("items")[0];
            // Active (1 scan, 1 opted-in) + Paused (1 scan, 1 opted-out); Archived excluded.
            Assert.Equal(2, item.GetProperty("qrScans").GetInt32());
            Assert.Equal(2, item.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, item.GetProperty("marketingOptIns").GetInt32());
            // Active placements exclude Paused + Archived.
            Assert.Equal(1, item.GetProperty("activePlacementsCount").GetInt32());
            // lastActivityAt is all-time max over Active+Paused (feedback after scan).
            var lastActivity = item.GetProperty("lastActivityAt").GetDateTime();
            Assert.Equal(
                new DateTime(2026, 7, 20, 15, 0, 0, DateTimeKind.Utc),
                DateTime.SpecifyKind(lastActivity, DateTimeKind.Utc)
            );
        }

        [Fact]
        public async Task GetCaptureLocations_CountsActivePlacementsOnly()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedMixedQrStatusesAsync(
                email: "capture-locations-active-qr@example.com",
                tokenSuffix: "loc-qr"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                1,
                body.GetProperty("items")[0]
                    .GetProperty("activePlacementsCount")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task GetCaptureLocations_SortsByHighestSubmissionRate_ZeroScanLast()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var jwt = await SeedSubmissionRateSortAsync(
                email: "capture-locations-rate-sort@example.com",
                tokenSuffix: "loc-rate"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, sort: "highest-submission-rate")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var items = body.GetProperty("items");
            Assert.Equal(3, items.GetArrayLength());
            // High rate (2/2=100%) first, mid (1/2=50%) second, zero-scan last.
            Assert.Equal("HighRate", items[0].GetProperty("locationName").GetString());
            Assert.Equal("MidRate", items[1].GetProperty("locationName").GetString());
            Assert.Equal("ZeroScan", items[2].GetProperty("locationName").GetString());
            Assert.Equal(0, items[2].GetProperty("qrScans").GetInt32());
        }

        [Theory]
        [InlineData("highest-qr-scans", "Zulu", "Alpha")]
        [InlineData("highest-marketing-opt-ins", "Alpha", "Zulu")]
        [InlineData("highest-offer-claims", "Alpha", "Zulu")]
        [InlineData("most-active-placements", "Alpha", "Zulu")]
        [InlineData("most-recent-activity", "Alpha", "Zulu")]
        [InlineData("location-name-az", "Alpha", "Zulu")]
        public async Task GetCaptureLocations_SortsByAllowedKeys(
            string sort,
            string firstName,
            string secondName
        )
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var jwt = await SeedDistinctSortFactsAsync(
                email: $"capture-locations-sort-{sort}@example.com",
                tokenSuffix: $"sort-{sort.GetHashCode(StringComparison.Ordinal):x}"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, sort: sort)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var items = (await ReadJsonAsync(response)).GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());
            Assert.Equal(firstName, items[0].GetProperty("locationName").GetString());
            Assert.Equal(secondName, items[1].GetProperty("locationName").GetString());
        }

        [Fact]
        public async Task GetCaptureOverview_IgnoresLocationPerformanceFilters()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedTwoLocationFactsAsync(
                email: "capture-overview-ignores-filters@example.com",
                tokenSuffix: "ov-filt"
            );

            using var filteredListRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, locationIds: [seeded.LocationBId])
            );
            filteredListRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var filteredList = await ReadJsonAsync(
                await _client.SendAsync(filteredListRequest)
            );
            Assert.Equal(1, filteredList.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                seeded.LocationBId,
                filteredList.GetProperty("items")[0]
                    .GetProperty("locationId")
                    .GetInt32()
            );
            Assert.Equal(
                1,
                filteredList.GetProperty("items")[0]
                    .GetProperty("qrScans")
                    .GetInt32()
            );

            using var overviewRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/overview?from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            overviewRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var overview = await ReadJsonAsync(
                await _client.SendAsync(overviewRequest)
            );

            // Restaurant-wide: Camden (2) + Soho (1), independent of list filters.
            Assert.Equal(3, overview.GetProperty("qrScans").GetInt32());
            Assert.Equal(3, overview.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, overview.GetProperty("marketingOptIns").GetInt32());
            Assert.Equal(2, overview.GetProperty("activeLocations").GetInt32());
            Assert.Equal(2, overview.GetProperty("totalLocations").GetInt32());
        }

        [Fact]
        public async Task GetCaptureLocations_FiltersBySearch_LocationIds_AndStatus()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedTwoLocationFactsAsync(
                email: "capture-locations-filters@example.com",
                tokenSuffix: "loc-filt"
            );

            using var searchRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, q: "cam")
            );
            searchRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var searchResponse = await _client.SendAsync(searchRequest);
            var searchBody = await ReadJsonAsync(searchResponse);
            Assert.Equal(1, searchBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Camden",
                searchBody.GetProperty("items")[0].GetProperty("locationName").GetString()
            );

            using var locationRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, locationIds: [seeded.LocationBId])
            );
            locationRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var locationResponse = await _client.SendAsync(locationRequest);
            var locationBody = await ReadJsonAsync(locationResponse);
            Assert.Equal(1, locationBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                seeded.LocationBId,
                locationBody.GetProperty("items")[0].GetProperty("locationId").GetInt32()
            );

            using var pausedRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, status: ["Paused"])
            );
            pausedRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var pausedResponse = await _client.SendAsync(pausedRequest);
            var pausedBody = await ReadJsonAsync(pausedResponse);
            Assert.Equal(0, pausedBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(0, pausedBody.GetProperty("items").GetArrayLength());

            using var activeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, status: ["Active"])
            );
            activeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var activeResponse = await _client.SendAsync(activeRequest);
            var activeBody = await ReadJsonAsync(activeResponse);
            Assert.Equal(2, activeBody.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetCaptureLocations_PaginatesWithPageSize20()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var jwt = await SeedManyLocationsAsync(
                email: "capture-locations-page@example.com",
                tokenSuffix: "loc-page",
                locationCount: 25
            );

            using var page1Request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, page: 1, sort: "location-name-az")
            );
            page1Request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            var page1 = await ReadJsonAsync(await _client.SendAsync(page1Request));
            Assert.Equal(25, page1.GetProperty("totalCount").GetInt32());
            Assert.Equal(20, page1.GetProperty("items").GetArrayLength());
            Assert.Equal(20, page1.GetProperty("pageSize").GetInt32());

            using var page2Request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, page: 2, sort: "location-name-az")
            );
            page2Request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            var page2 = await ReadJsonAsync(await _client.SendAsync(page2Request));
            Assert.Equal(5, page2.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task GetCaptureLocations_Returns403_ForTummlyStaffJwt()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();
            var admin = new Admin
            {
                FullName = "Staff",
                Email = "staff-capture-12@example.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();
            var jwt = jwtService.GenerateAdminToken(admin);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocations_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(LocationsUrl(from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocations_Returns400_WhenFromMissing()
        {
            var seeded = await SeedOwnerOnlyAsync(
                email: "capture-locations-miss-from@example.com"
            );
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/locations?to={Uri.EscapeDataString(FormatUtc(to))}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocations_Returns400_WhenSpanExceeds180Days()
        {
            var seeded = await SeedOwnerOnlyAsync(
                email: "capture-locations-span-max@example.com"
            );
            var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(181);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocations_ReturnsPersistedCaptureLocationStatus_DefaultActive()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedLocationCaptureStatusAsync(
                email: "capture-locations-status@example.com",
                tokenSuffix: "loc-status"
            );

            using var allRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            allRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var allResponse = await _client.SendAsync(allRequest);
            Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            var allBody = await ReadJsonAsync(allResponse);
            Assert.Equal(2, allBody.GetProperty("totalCount").GetInt32());

            var items = allBody.GetProperty("items");
            var activeRow = FindLocationById(items, seeded.ActiveLocationId);
            var pausedRow = FindLocationById(items, seeded.PausedLocationId);
            Assert.Equal("Active", activeRow.GetProperty("status").GetString());
            Assert.Equal("Paused", pausedRow.GetProperty("status").GetString());

            using var pausedRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, status: ["Paused"])
            );
            pausedRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var pausedResponse = await _client.SendAsync(pausedRequest);
            var pausedBody = await ReadJsonAsync(pausedResponse);
            Assert.Equal(1, pausedBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                seeded.PausedLocationId,
                pausedBody.GetProperty("items")[0].GetProperty("locationId").GetInt32()
            );

            using var activeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to, status: ["Active"])
            );
            activeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var activeResponse = await _client.SendAsync(activeRequest);
            var activeBody = await ReadJsonAsync(activeResponse);
            Assert.Equal(1, activeBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                seeded.ActiveLocationId,
                activeBody.GetProperty("items")[0].GetProperty("locationId").GetInt32()
            );

            // Restore set persists for location-pause selective restore (not on wire yet).
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var paused = await context.RestaurantLocations
                    .AsNoTracking()
                    .SingleAsync(l => l.Id == seeded.PausedLocationId);
                Assert.Equal(
                    CaptureLocationStatus.Paused,
                    paused.CaptureLocationStatus
                );
                Assert.Equal(
                    System.Text.Json.JsonSerializer.Serialize(
                        new[] { seeded.RestoreQrCodeId }
                    ),
                    paused.CaptureLocationPauseRestoreQrCodeIdsJson
                );
            }
        }

        [Fact]
        public async Task GetCaptureLocations_Returns403_WhenNoRestaurant()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var jwt = await SeedUserWithoutRestaurantAsync(
                email: "capture-locations-no-rest@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PauseLocationCapture_PausesActiveCodes_StoresRestoreSet_LeavesAlreadyPaused()
        {
            var seeded = await SeedLocationPauseActivateAsync(
                email: "capture-loc-pause@example.com",
                tokenSuffix: "locpause"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/locations/{seeded.LocationId}/pause"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("Paused", body.GetProperty("status").GetString());
            Assert.Equal(3, body.GetProperty("pausedCount").GetInt32());
            Assert.Equal(
                3,
                body.GetProperty("pauseRestoreQrCodeCount").GetInt32()
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var location = await context.RestaurantLocations
                    .AsNoTracking()
                    .SingleAsync(l => l.Id == seeded.LocationId);
                Assert.Equal(
                    CaptureLocationStatus.Paused,
                    location.CaptureLocationStatus
                );
                var restoreIds = JsonSerializer.Deserialize<int[]>(
                    location.CaptureLocationPauseRestoreQrCodeIdsJson!
                )!;
                Assert.Equal(
                    new[]
                    {
                        seeded.ActiveCounterCardId,
                        seeded.ActiveSmartGuestId,
                        seeded.ActiveDigitalId,
                    }.OrderBy(id => id),
                    restoreIds.OrderBy(id => id)
                );

                var statuses = await context.QrCodes
                    .AsNoTracking()
                    .Where(q => q.RestaurantLocationId == seeded.LocationId)
                    .ToDictionaryAsync(q => q.Id, q => q.Status);
                Assert.Equal(
                    QrCodeStatus.Paused,
                    statuses[seeded.ActiveCounterCardId]
                );
                Assert.Equal(
                    QrCodeStatus.Paused,
                    statuses[seeded.ActiveSmartGuestId]
                );
                Assert.Equal(
                    QrCodeStatus.Paused,
                    statuses[seeded.ActiveDigitalId]
                );
                Assert.Equal(
                    QrCodeStatus.Paused,
                    statuses[seeded.AlreadyPausedId]
                );
                Assert.Equal(
                    QrCodeStatus.Archived,
                    statuses[seeded.ArchivedId]
                );
            }
        }

        [Fact]
        public async Task ActivateLocationCapture_RestoresOnlyRestoreSet_SetsLocationActive()
        {
            var seeded = await SeedLocationPauseActivateAsync(
                email: "capture-loc-activate@example.com",
                tokenSuffix: "locact"
            );

            using (var pauseRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/locations/{seeded.LocationId}/pause"
            ))
            {
                pauseRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                var pauseResponse = await _client.SendAsync(pauseRequest);
                Assert.Equal(HttpStatusCode.OK, pauseResponse.StatusCode);
            }

            using var activateRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/locations/{seeded.LocationId}/activate"
            );
            activateRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var activateResponse = await _client.SendAsync(activateRequest);

            Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
            var body = await ReadJsonAsync(activateResponse);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("Active", body.GetProperty("status").GetString());
            Assert.Equal(3, body.GetProperty("activatedCount").GetInt32());
            Assert.Equal(
                0,
                body.GetProperty("pauseRestoreQrCodeCount").GetInt32()
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var location = await context.RestaurantLocations
                    .AsNoTracking()
                    .SingleAsync(l => l.Id == seeded.LocationId);
                Assert.Equal(
                    CaptureLocationStatus.Active,
                    location.CaptureLocationStatus
                );
                Assert.True(
                    string.IsNullOrEmpty(
                        location.CaptureLocationPauseRestoreQrCodeIdsJson
                    )
                );

                var statuses = await context.QrCodes
                    .AsNoTracking()
                    .Where(q => q.RestaurantLocationId == seeded.LocationId)
                    .ToDictionaryAsync(q => q.Id, q => q.Status);
                Assert.Equal(
                    QrCodeStatus.Active,
                    statuses[seeded.ActiveCounterCardId]
                );
                Assert.Equal(
                    QrCodeStatus.Active,
                    statuses[seeded.ActiveSmartGuestId]
                );
                Assert.Equal(
                    QrCodeStatus.Active,
                    statuses[seeded.ActiveDigitalId]
                );
                Assert.Equal(
                    QrCodeStatus.Paused,
                    statuses[seeded.AlreadyPausedId]
                );
                Assert.Equal(
                    QrCodeStatus.Archived,
                    statuses[seeded.ArchivedId]
                );
            }
        }

        [Fact]
        public async Task ActivateLocationCapture_DropsArchivedFromRestoreSet()
        {
            var seeded = await SeedLocationPauseActivateAsync(
                email: "capture-loc-activate-arch@example.com",
                tokenSuffix: "locactarch"
            );

            using (var pauseRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/locations/{seeded.LocationId}/pause"
            ))
            {
                pauseRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                Assert.Equal(
                    HttpStatusCode.OK,
                    (await _client.SendAsync(pauseRequest)).StatusCode
                );
            }

            using (var archiveRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/placements/{seeded.ActiveCounterCardId}/archive?locationId={seeded.LocationId}"
            ))
            {
                archiveRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                Assert.Equal(
                    HttpStatusCode.OK,
                    (await _client.SendAsync(archiveRequest)).StatusCode
                );
            }

            using var activateRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/locations/{seeded.LocationId}/activate"
            );
            activateRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var activateResponse = await _client.SendAsync(activateRequest);
            Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
            var body = await ReadJsonAsync(activateResponse);
            Assert.Equal(2, body.GetProperty("activatedCount").GetInt32());

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var statuses = await context.QrCodes
                    .AsNoTracking()
                    .Where(q => q.RestaurantLocationId == seeded.LocationId)
                    .ToDictionaryAsync(q => q.Id, q => q.Status);
                Assert.Equal(
                    QrCodeStatus.Archived,
                    statuses[seeded.ActiveCounterCardId]
                );
                Assert.Equal(
                    QrCodeStatus.Active,
                    statuses[seeded.ActiveSmartGuestId]
                );
                Assert.Equal(
                    QrCodeStatus.Active,
                    statuses[seeded.ActiveDigitalId]
                );
                Assert.Equal(
                    QrCodeStatus.Paused,
                    statuses[seeded.AlreadyPausedId]
                );
            }
        }

        [Fact]
        public async Task PauseLocationCapture_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedLocationPauseActivateAsync(
                email: "capture-loc-pause-ownera@example.com",
                tokenSuffix: "locpa"
            );
            var other = await SeedLocationPauseActivateAsync(
                email: "capture-loc-pause-ownerb@example.com",
                tokenSuffix: "locpb"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/capture/locations/{other.LocationId}/pause"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCaptureLocations_ExposesPauseRestoreQrCodeCount_WhenPaused()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedLocationCaptureStatusAsync(
                email: "capture-locations-restore-count@example.com",
                tokenSuffix: "loc-rc"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                LocationsUrl(from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var items = (await ReadJsonAsync(response)).GetProperty("items");
            var pausedRow = FindLocationById(items, seeded.PausedLocationId);
            var activeRow = FindLocationById(items, seeded.ActiveLocationId);
            Assert.Equal(
                1,
                pausedRow.GetProperty("pauseRestoreQrCodeCount").GetInt32()
            );
            Assert.Equal(
                0,
                activeRow.GetProperty("pauseRestoreQrCodeCount").GetInt32()
            );
        }

        private static string LocationsUrl(
            DateTime from,
            DateTime to,
            string? q = null,
            string? sort = null,
            int page = 1,
            string[]? status = null,
            int[]? locationIds = null
        )
        {
            var url =
                $"/api/capture/locations?from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}&page={page}&pageSize=20";
            if (!string.IsNullOrWhiteSpace(q))
            {
                url += $"&q={Uri.EscapeDataString(q)}";
            }

            if (!string.IsNullOrWhiteSpace(sort))
            {
                url += $"&sort={Uri.EscapeDataString(sort)}";
            }

            if (status != null)
            {
                foreach (var value in status)
                {
                    url += $"&status={Uri.EscapeDataString(value)}";
                }
            }

            if (locationIds != null)
            {
                foreach (var id in locationIds)
                {
                    url += $"&locationIds={id}";
                }
            }

            return url;
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

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private async Task<(string Jwt, int LocationAId, int LocationBId)>
            SeedTwoLocationFactsAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Locations Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900100",
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
                Name = "Capture Locations Group",
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
                Token = $"cap-loc-{tokenSuffix}-a",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var qrB = new QrCode
            {
                RestaurantLocationId = locationB.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-loc-{tokenSuffix}-b",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(qrA, qrB);
            await context.SaveChangesAsync();

            var currentAt = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    CreatedAt = currentAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    CreatedAt = currentAt.AddHours(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = locationB.Id,
                    QrCodeId = qrB.Id,
                    CreatedAt = currentAt.AddHours(2),
                }
            );

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    GuestName = "A1",
                    GuestContact = "a1@example.com",
                    ContactType = ContactType.Email,
                    Comment = "A1",
                    OffersOptOut = false,
                    CreatedAt = currentAt.AddMinutes(30),
                },
                new Feedback
                {
                    RestaurantLocationId = locationA.Id,
                    QrCodeId = qrA.Id,
                    GuestName = "A2",
                    GuestContact = "a2@example.com",
                    ContactType = ContactType.Email,
                    Comment = "A2",
                    OffersOptOut = true,
                    CreatedAt = currentAt.AddHours(1).AddMinutes(10),
                },
                new Feedback
                {
                    RestaurantLocationId = locationB.Id,
                    QrCodeId = qrB.Id,
                    GuestName = "B1",
                    GuestContact = "b1@example.com",
                    ContactType = ContactType.Email,
                    Comment = "B1",
                    OffersOptOut = true,
                    CreatedAt = currentAt.AddHours(2).AddMinutes(10),
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

        private async Task<(string Jwt, int LocationId)> SeedActivePausedArchivedAsync(
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
                FullName = "Capture Locations Paused Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900101",
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
                Name = "Capture Locations Paused",
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
                Token = $"cap-loc-{tokenSuffix}-active",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var paused = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"cap-loc-{tokenSuffix}-paused",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.WindowSticker,
                Token = $"cap-loc-{tokenSuffix}-archived",
                Status = QrCodeStatus.Archived,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(active, paused, archived);
            await context.SaveChangesAsync();

            var inWindow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
            // Outside window but all-time last activity (feedback later than scans).
            var allTimeFeedback = new DateTime(2026, 7, 20, 15, 0, 0, DateTimeKind.Utc);

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
                    QrCodeId = active.Id,
                    CreatedAt = allTimeFeedback.AddHours(-1),
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
                    CreatedAt = inWindow.AddMinutes(30),
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
                    CreatedAt = inWindow.AddHours(1).AddMinutes(30),
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
                    CreatedAt = inWindow.AddHours(2).AddMinutes(30),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = paused.Id,
                    GuestName = "Late Guest",
                    GuestContact = "late@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Late",
                    OffersOptOut = true,
                    CreatedAt = allTimeFeedback,
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
                FullName = "Capture Locations QR Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900102",
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
                Name = "Capture Locations QR",
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

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.SmartGuest,
                    Token = $"cap-loc-{tokenSuffix}-active",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.CounterCard,
                    Token = $"cap-loc-{tokenSuffix}-paused",
                    Status = QrCodeStatus.Paused,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.WindowSticker,
                    Token = $"cap-loc-{tokenSuffix}-archived",
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

        private async Task<string> SeedDistinctSortFactsAsync(
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
                FullName = "Capture Locations Sort Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900107",
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
                Name = "Capture Locations Sort",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            // Alpha: fewer scans, more marketing opt-ins, more active placements,
            // more recent activity. Zulu: opposite on those axes (more scans).
            // Default highest-qr-scans → Zulu, Alpha; other keys → Alpha, Zulu.
            var alpha = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Alpha",
                Address = "1 Alpha Street",
                CreatedAt = DateTime.UtcNow,
            };
            var zulu = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Zulu",
                Address = "2 Zulu Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(alpha, zulu);
            await context.SaveChangesAsync();

            var alphaQr1 = new QrCode
            {
                RestaurantLocationId = alpha.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-sort-{tokenSuffix}-a1",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var alphaQr2 = new QrCode
            {
                RestaurantLocationId = alpha.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-sort-{tokenSuffix}-a2",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var alphaQr3 = new QrCode
            {
                RestaurantLocationId = alpha.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-sort-{tokenSuffix}-a3",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var zuluQr = new QrCode
            {
                RestaurantLocationId = zulu.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-sort-{tokenSuffix}-z",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(alphaQr1, alphaQr2, alphaQr3, zuluQr);
            await context.SaveChangesAsync();

            var olderAt = new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc);
            var newerAt = new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc);

            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = alpha.Id,
                    QrCodeId = alphaQr1.Id,
                    CreatedAt = newerAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = zulu.Id,
                    QrCodeId = zuluQr.Id,
                    CreatedAt = olderAt,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = zulu.Id,
                    QrCodeId = zuluQr.Id,
                    CreatedAt = olderAt.AddHours(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = zulu.Id,
                    QrCodeId = zuluQr.Id,
                    CreatedAt = olderAt.AddHours(2),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = zulu.Id,
                    QrCodeId = zuluQr.Id,
                    CreatedAt = olderAt.AddHours(3),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = zulu.Id,
                    QrCodeId = zuluQr.Id,
                    CreatedAt = olderAt.AddHours(4),
                }
            );

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = alpha.Id,
                    QrCodeId = alphaQr1.Id,
                    GuestName = "A1",
                    GuestContact = "a1@example.com",
                    ContactType = ContactType.Email,
                    Comment = "A1",
                    OffersOptOut = false,
                    CreatedAt = newerAt.AddMinutes(5),
                },
                new Feedback
                {
                    RestaurantLocationId = alpha.Id,
                    QrCodeId = alphaQr1.Id,
                    GuestName = "A2",
                    GuestContact = "a2@example.com",
                    ContactType = ContactType.Email,
                    Comment = "A2",
                    OffersOptOut = false,
                    CreatedAt = newerAt.AddMinutes(10),
                },
                new Feedback
                {
                    RestaurantLocationId = zulu.Id,
                    QrCodeId = zuluQr.Id,
                    GuestName = "Z1",
                    GuestContact = "z1@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Z1",
                    OffersOptOut = true,
                    CreatedAt = olderAt.AddMinutes(30),
                }
            );

            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private async Task<string> SeedSubmissionRateSortAsync(
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
                FullName = "Capture Locations Rate Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900103",
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
                Name = "Capture Locations Rate",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var high = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "HighRate",
                Address = "1 High",
                CreatedAt = DateTime.UtcNow,
            };
            var mid = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "MidRate",
                Address = "2 Mid",
                CreatedAt = DateTime.UtcNow,
            };
            var zero = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "ZeroScan",
                Address = "3 Zero",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(high, mid, zero);
            await context.SaveChangesAsync();

            var qrHigh = new QrCode
            {
                RestaurantLocationId = high.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-loc-{tokenSuffix}-high",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var qrMid = new QrCode
            {
                RestaurantLocationId = mid.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-loc-{tokenSuffix}-mid",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var qrZero = new QrCode
            {
                RestaurantLocationId = zero.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-loc-{tokenSuffix}-zero",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(qrHigh, qrMid, qrZero);
            await context.SaveChangesAsync();

            var inWindow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = high.Id,
                    QrCodeId = qrHigh.Id,
                    CreatedAt = inWindow,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = high.Id,
                    QrCodeId = qrHigh.Id,
                    CreatedAt = inWindow.AddMinutes(1),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = mid.Id,
                    QrCodeId = qrMid.Id,
                    CreatedAt = inWindow,
                },
                new QrScanEvent
                {
                    RestaurantLocationId = mid.Id,
                    QrCodeId = qrMid.Id,
                    CreatedAt = inWindow.AddMinutes(1),
                }
            );

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = high.Id,
                    QrCodeId = qrHigh.Id,
                    GuestName = "H1",
                    GuestContact = "h1@example.com",
                    ContactType = ContactType.Email,
                    Comment = "H1",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddMinutes(10),
                },
                new Feedback
                {
                    RestaurantLocationId = high.Id,
                    QrCodeId = qrHigh.Id,
                    GuestName = "H2",
                    GuestContact = "h2@example.com",
                    ContactType = ContactType.Email,
                    Comment = "H2",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddMinutes(11),
                },
                new Feedback
                {
                    RestaurantLocationId = mid.Id,
                    QrCodeId = qrMid.Id,
                    GuestName = "M1",
                    GuestContact = "m1@example.com",
                    ContactType = ContactType.Email,
                    Comment = "M1",
                    OffersOptOut = false,
                    CreatedAt = inWindow.AddMinutes(10),
                }
            );

            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private async Task<string> SeedManyLocationsAsync(
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
                FullName = "Capture Locations Page Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900104",
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
                Name = "Capture Locations Page",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            for (var i = 0; i < locationCount; i++)
            {
                context.RestaurantLocations.Add(
                    new RestaurantLocation
                    {
                        RestaurantId = restaurant.Id,
                        LocationName = $"Loc {i:D2}",
                        Address = $"{i} Street",
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }

            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private async Task<string> SeedOwnerOnlyAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Locations Validation Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900105",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            context.Restaurants.Add(
                new Restaurant
                {
                    Name = "Capture Locations Validation",
                    AccountType = "Multi",
                    OwnerUserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
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
                FullName = "No Restaurant User",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900106",
                Role = "Owner",
                AccountType = "Multi",
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

        private static JsonElement FindLocationById(
            JsonElement items,
            int locationId
        )
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.GetProperty("locationId").GetInt32() == locationId)
                {
                    return item;
                }
            }

            throw new Xunit.Sdk.XunitException(
                $"Expected location row with id {locationId}."
            );
        }

        private async Task<(
            string Jwt,
            int ActiveLocationId,
            int PausedLocationId,
            int RestoreQrCodeId
        )> SeedLocationCaptureStatusAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Location Status Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900107",
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
                Name = "Capture Location Status Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var activeLocation = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Active Venue",
                Address = "1 High Street",
                CaptureLocationStatus = CaptureLocationStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var pausedLocation = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Paused Venue",
                Address = "2 High Street",
                CaptureLocationStatus = CaptureLocationStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(activeLocation, pausedLocation);
            await context.SaveChangesAsync();

            var restoreQr = new QrCode
            {
                RestaurantLocationId = pausedLocation.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-loc-status-{tokenSuffix}-sg-tok1",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(restoreQr);
            await context.SaveChangesAsync();

            pausedLocation.CaptureLocationPauseRestoreQrCodeIdsJson =
                JsonSerializer.Serialize(new[] { restoreQr.Id });
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, activeLocation.Id, pausedLocation.Id, restoreQr.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int ActiveCounterCardId,
            int ActiveSmartGuestId,
            int ActiveDigitalId,
            int AlreadyPausedId,
            int ArchivedId
        )> SeedLocationPauseActivateAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Location Pause Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900108",
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
                Name = "Capture Location Pause Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Pause Venue",
                Address = "9 Pause Street",
                CaptureLocationStatus = CaptureLocationStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var activeCounter = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = $"cap-loc-pa-{tokenSuffix}-cc",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var activeSmartGuest = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = $"cap-loc-pa-{tokenSuffix}-sg",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var activeDigital = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.DigitalGuestLink,
                Token = $"cap-loc-pa-{tokenSuffix}-dg",
                Status = QrCodeStatus.Active,
                LinkName = "Pause Link",
                NormalizedLinkName = "pause link",
                Channel = DigitalGuestLinkChannel.Email,
                CreatedAt = DateTime.UtcNow,
            };
            var alreadyPaused = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.PackagingSticker,
                Token = $"cap-loc-pa-{tokenSuffix}-ps",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.WindowSticker,
                Token = $"cap-loc-pa-{tokenSuffix}-ws",
                Status = QrCodeStatus.Archived,
                ArchivedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(
                activeCounter,
                activeSmartGuest,
                activeDigital,
                alreadyPaused,
                archived
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (
                jwt,
                location.Id,
                activeCounter.Id,
                activeSmartGuest.Id,
                activeDigital.Id,
                alreadyPaused.Id,
                archived.Id
            );
        }
    }
}
