using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CapturePlacementsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CapturePlacementsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCapturePlacements_ReturnsActiveAndPausedOnly_WithWindowedMetricsAndAllTimeLastScan()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPlacementsAsync(
                email: "capture-placements-list@example.com",
                tokenSuffix: "list"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PlacementsUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var placements = body.GetProperty("placements");
            Assert.Equal(2, placements.GetArrayLength());

            var counter = FindByQrType(placements, "CounterCard");
            Assert.Equal("Active", counter.GetProperty("status").GetString());
            Assert.Equal(2, counter.GetProperty("qrScans").GetInt32());
            Assert.Equal(1, counter.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(1, counter.GetProperty("marketingOptIns").GetInt32());
            Assert.Equal(0, counter.GetProperty("offerClaims").GetInt32());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    counter.GetProperty("qrLinkUrl").GetString()
                )
            );
            Assert.Contains(
                seeded.CounterToken,
                counter.GetProperty("qrLinkUrl").GetString()
            );
            // All-time last scan is max CreatedAt (Jul 14), not clipped by the window
            // (Jul 1 scan still exists but is older).
            Assert.Equal(
                new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc)
                    .ToString("O"),
                counter.GetProperty("lastScanAt").GetString()
            );

            var packaging = FindByQrType(placements, "PackagingSticker");
            Assert.Equal("Paused", packaging.GetProperty("status").GetString());
            Assert.Equal(1, packaging.GetProperty("qrScans").GetInt32());
            Assert.Equal(1, packaging.GetProperty("feedbackSubmitted").GetInt32());
            Assert.Equal(0, packaging.GetProperty("marketingOptIns").GetInt32());
            Assert.Equal(0, packaging.GetProperty("offerClaims").GetInt32());
            Assert.Equal(
                new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc)
                    .ToString("O"),
                packaging.GetProperty("lastScanAt").GetString()
            );

            var lastJourney = body.GetProperty("lastJourneyUpdate");
            Assert.Equal(
                "Packaging Guest",
                lastJourney.GetProperty("guestName").GetString()
            );
            Assert.Equal(
                new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc)
                    .ToString("O"),
                lastJourney.GetProperty("createdAt").GetString()
            );

            // Archived row must not appear.
            foreach (var item in placements.EnumerateArray())
            {
                Assert.NotEqual(
                    "WindowSticker",
                    item.GetProperty("qrType").GetString()
                );
                Assert.NotEqual(
                    "Archived",
                    item.GetProperty("status").GetString()
                );
            }
        }

        [Fact]
        public async Task GetCapturePlacements_RowWindowedCounts_SumToCapturePerformancePrimaries()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedPlacementsAsync(
                email: "capture-placements-sum@example.com",
                tokenSuffix: "sum"
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

            using var performanceRequest = new HttpRequestMessage(
                HttpMethod.Get,
                PerformanceUrl(seeded.LocationId, from, to)
            );
            performanceRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var performanceResponse = await _client.SendAsync(performanceRequest);
            Assert.Equal(HttpStatusCode.OK, performanceResponse.StatusCode);
            var performanceBody = await ReadJsonAsync(performanceResponse);

            var placements = placementsBody.GetProperty("placements");
            var sumScans = 0;
            var sumFeedback = 0;
            var sumOptIns = 0;
            var sumClaims = 0;
            foreach (var item in placements.EnumerateArray())
            {
                sumScans += item.GetProperty("qrScans").GetInt32();
                sumFeedback += item.GetProperty("feedbackSubmitted").GetInt32();
                sumOptIns += item.GetProperty("marketingOptIns").GetInt32();
                sumClaims += item.GetProperty("offerClaims").GetInt32();
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
            Assert.Equal(
                performanceBody.GetProperty("offerClaims").GetInt32(),
                sumClaims
            );
        }

        [Fact]
        public async Task GetCapturePlacements_ReturnsNullLastScanAt_WhenNeverScanned()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithUnscannedActiveAsync(
                email: "capture-placements-noscan@example.com",
                tokenSuffix: "noscan"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PlacementsUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var placement = body.GetProperty("placements")[0];
            Assert.Equal(JsonValueKind.Null, placement.GetProperty("lastScanAt").ValueKind);
            Assert.Equal(0, placement.GetProperty("qrScans").GetInt32());
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("lastJourneyUpdate").ValueKind
            );
        }

        [Fact]
        public async Task GetCapturePlacements_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerWithUnscannedActiveAsync(
                email: "capture-placements-owner-a@example.com",
                tokenSuffix: "ownera"
            );
            var other = await SeedOwnerWithUnscannedActiveAsync(
                email: "capture-placements-owner-b@example.com",
                tokenSuffix: "ownerb"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PlacementsUrl(other.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePlacements_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            var response = await _client.GetAsync(PlacementsUrl(1, from, to));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePlacements_Returns400_WhenFromMissing()
        {
            var seeded = await SeedOwnerWithUnscannedActiveAsync(
                email: "capture-placements-from@example.com",
                tokenSuffix: "frommiss"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/capture/placements?locationId={seeded.LocationId}&to=2026-07-17T00:00:00.000Z"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePlacements_ReturnsDigitalGuestLinks_WithLinkNameAndChannel()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedDigitalGuestLinksAsync(
                email: "capture-placements-digital@example.com",
                tokenSuffix: "dgl"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PlacementsUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var placements = body.GetProperty("placements");
            Assert.Equal(3, placements.GetArrayLength());

            var summer = FindByLinkName(placements, "Summer Promo");
            Assert.Equal("DigitalGuestLink", summer.GetProperty("qrType").GetString());
            Assert.Equal("Active", summer.GetProperty("status").GetString());
            Assert.Equal("SocialMedia", summer.GetProperty("channel").GetString());
            Assert.Contains(
                seeded.SummerToken,
                summer.GetProperty("qrLinkUrl").GetString()
            );

            var emailLink = FindByLinkName(placements, "Email blast");
            Assert.Equal("DigitalGuestLink", emailLink.GetProperty("qrType").GetString());
            Assert.Equal("Paused", emailLink.GetProperty("status").GetString());
            Assert.Equal("Email", emailLink.GetProperty("channel").GetString());

            // Archived digital guest link must not appear; name slot free for reuse later.
            foreach (var item in placements.EnumerateArray())
            {
                Assert.NotEqual(
                    "Old WhatsApp",
                    item.TryGetProperty("linkName", out var name)
                        ? name.GetString()
                        : null
                );
            }

            var smartGuest = FindByQrType(placements, "SmartGuest");
            Assert.Equal(JsonValueKind.Null, smartGuest.GetProperty("linkName").ValueKind);
            Assert.Equal(JsonValueKind.Null, smartGuest.GetProperty("channel").ValueKind);
        }

        [Fact]
        public async Task GetCapturePlacements_AllowsManyNonArchivedDigitalGuestLinks_PerLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedDigitalGuestLinksAsync(
                email: "capture-placements-digital-many@example.com",
                tokenSuffix: "dglmany"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PlacementsUrl(seeded.LocationId, from, to)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var digitalCount = body.GetProperty("placements")
                .EnumerateArray()
                .Count(item =>
                    item.GetProperty("qrType").GetString() == "DigitalGuestLink"
                );

            // Two Active/Paused digital links coexist; catalog uniqueness does not apply.
            Assert.Equal(2, digitalCount);
        }

        [Fact]
        public void QrCodeIndexes_EncodeCatalogAndDigitalLinkUniquenessRules()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var entity = context.Model.FindEntityType(typeof(QrCode));
            Assert.NotNull(entity);

            var catalogIndex = entity!
                .GetIndexes()
                .Single(index =>
                    index.Properties.Select(p => p.Name).SequenceEqual(
                        new[] { "RestaurantLocationId", "QrType" }
                    )
                );
            Assert.True(catalogIndex.IsUnique);
            Assert.Equal(
                "[Status] IN (0, 1) AND [QrType] <> 5",
                catalogIndex.GetFilter()
            );

            var linkNameIndex = entity
                .GetIndexes()
                .Single(index =>
                    index.Properties.Select(p => p.Name).SequenceEqual(
                        new[] { "RestaurantLocationId", "NormalizedLinkName" }
                    )
                );
            Assert.True(linkNameIndex.IsUnique);
            Assert.Equal(
                "[QrType] = 5 AND [Status] IN (0, 1) AND [NormalizedLinkName] IS NOT NULL",
                linkNameIndex.GetFilter()
            );
        }

        [Theory]
        [InlineData(QrType.CounterCard)]
        [InlineData(QrType.PackagingSticker)]
        [InlineData(QrType.DeliveryInsert)]
        [InlineData(QrType.WindowSticker)]
        [InlineData(QrType.SmartGuest)]
        [InlineData(QrType.DigitalGuestLink)]
        public async Task PauseAndResumePlacement_FlipsStatusWithoutChangingToken(
            QrType qrType
        )
        {
            var seeded = await SeedSingleQrCodeAsync(
                email: $"capture-pause-{qrType}@example.com",
                token: $"capture-{qrType}-token-123456789012",
                qrType: qrType,
                status: QrCodeStatus.Active
            );

            using var pauseRequest = new HttpRequestMessage(
                HttpMethod.Post,
                PlacementMutationUrl("pause", seeded.LocationId, seeded.QrCodeId)
            );
            pauseRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var pauseResponse = await _client.SendAsync(pauseRequest);
            Assert.Equal(HttpStatusCode.OK, pauseResponse.StatusCode);
            var pauseBody = await ReadJsonAsync(pauseResponse);
            Assert.True(pauseBody.GetProperty("success").GetBoolean());
            Assert.Equal("Paused", pauseBody.GetProperty("status").GetString());

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var qrCode = context.QrCodes.AsEnumerable()
                    .Single(q => q.Id == seeded.QrCodeId);
                Assert.Equal(QrCodeStatus.Paused, qrCode.Status);
                Assert.Equal(seeded.Token, qrCode.Token);
            }

            using var resumeRequest = new HttpRequestMessage(
                HttpMethod.Post,
                PlacementMutationUrl("resume", seeded.LocationId, seeded.QrCodeId)
            );
            resumeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var resumeResponse = await _client.SendAsync(resumeRequest);
            Assert.Equal(HttpStatusCode.OK, resumeResponse.StatusCode);
            var resumeBody = await ReadJsonAsync(resumeResponse);
            Assert.True(resumeBody.GetProperty("success").GetBoolean());
            Assert.Equal("Active", resumeBody.GetProperty("status").GetString());

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var qrCode = context.QrCodes.AsEnumerable()
                    .Single(q => q.Id == seeded.QrCodeId);
                Assert.Equal(QrCodeStatus.Active, qrCode.Status);
                Assert.Equal(seeded.Token, qrCode.Token);
            }
        }

        [Theory]
        [InlineData("pause", QrCodeStatus.Paused, "Only Active QR codes can be paused.")]
        [InlineData("pause", QrCodeStatus.Archived, "Only Active QR codes can be paused.")]
        [InlineData("resume", QrCodeStatus.Active, "Only Paused QR codes can be resumed.")]
        [InlineData("resume", QrCodeStatus.Archived, "Only Paused QR codes can be resumed.")]
        public async Task PlacementMutation_Returns400_ForIllegalTransition(
            string action,
            QrCodeStatus currentStatus,
            string expectedMessage
        )
        {
            var seeded = await SeedSingleQrCodeAsync(
                email: $"capture-illegal-{action}-{currentStatus}@example.com",
                token: $"capture-illegal-{action}-{currentStatus}-token",
                qrType: QrType.CounterCard,
                status: currentStatus
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                PlacementMutationUrl(action, seeded.LocationId, seeded.QrCodeId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(expectedMessage, body.GetProperty("message").GetString());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var qrCode = context.QrCodes.AsEnumerable()
                .Single(q => q.Id == seeded.QrCodeId);
            Assert.Equal(currentStatus, qrCode.Status);
            Assert.Equal(seeded.Token, qrCode.Token);
        }

        [Fact]
        public async Task PauseSmartGuest_BlocksGuestResolveAndHomeGuestUrl()
        {
            var seeded = await SeedSingleQrCodeAsync(
                email: "capture-smart-guest-pause@example.com",
                token: "capture-smart-guest-pause-token",
                qrType: QrType.SmartGuest,
                status: QrCodeStatus.Active
            );

            using (var pauseRequest = new HttpRequestMessage(
                HttpMethod.Post,
                PlacementMutationUrl("pause", seeded.LocationId, seeded.QrCodeId)
            ))
            {
                pauseRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                var pauseResponse = await _client.SendAsync(pauseRequest);
                Assert.Equal(HttpStatusCode.OK, pauseResponse.StatusCode);
            }

            var scanResponse = await _client.GetAsync($"/api/scan/{seeded.Token}");
            Assert.Equal(HttpStatusCode.NotFound, scanResponse.StatusCode);

            var feedbackResponse = await _client.PostAsJsonAsync(
                $"/api/scan/{seeded.Token}/feedback",
                new
                {
                    guestName = "Alex Guest",
                    guestContact = "alex@example.com",
                    comment = "Blocked after pause."
                }
            );
            Assert.Equal(HttpStatusCode.NotFound, feedbackResponse.StatusCode);

            using (var sttContent = new MultipartFormDataContent())
            {
                var audio = new ByteArrayContent(new byte[] { 1, 2, 3 });
                audio.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
                sttContent.Add(audio, "audio", "clip.webm");
                var sttResponse = await _client.PostAsync(
                    $"/api/scan/{seeded.Token}/stt",
                    sttContent
                );
                Assert.Equal(HttpStatusCode.NotFound, sttResponse.StatusCode);
            }

            using var locationsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            locationsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var locationsResponse = await _client.SendAsync(locationsRequest);
            Assert.Equal(HttpStatusCode.OK, locationsResponse.StatusCode);
            var locationsBody = await ReadJsonAsync(locationsResponse);
            var location = locationsBody.GetProperty("locations")[0];
            Assert.Equal("", location.GetProperty("guestUrl").GetString());
        }

        private static JsonElement FindByQrType(
            JsonElement placements,
            string qrType
        )
        {
            foreach (var item in placements.EnumerateArray())
            {
                if (item.GetProperty("qrType").GetString() == qrType)
                {
                    return item;
                }
            }

            throw new Xunit.Sdk.XunitException(
                $"Expected placement with qrType {qrType}."
            );
        }

        private static JsonElement FindByLinkName(
            JsonElement placements,
            string linkName
        )
        {
            foreach (var item in placements.EnumerateArray())
            {
                if (
                    item.TryGetProperty("linkName", out var name)
                    && name.GetString() == linkName
                )
                {
                    return item;
                }
            }

            throw new Xunit.Sdk.XunitException(
                $"Expected placement with linkName {linkName}."
            );
        }

        private static string PlacementsUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/capture/placements?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string PerformanceUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/capture/performance?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string PlacementMutationUrl(
            string action,
            int locationId,
            int qrCodeId
        )
        {
            return $"/api/capture/placements/{qrCodeId}/{action}?locationId={locationId}";
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

        private async Task<(
            string Jwt,
            int LocationId,
            string CounterToken
        )> SeedPlacementsAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Placements Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900910",
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
                Name = "Capture Placements Venue",
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

            var counterToken = $"cap-place-{tokenSuffix}-cc-token123";
            var counter = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = counterToken,
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var packaging = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.PackagingSticker,
                Token = $"cap-place-{tokenSuffix}-ps-token123",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.WindowSticker,
                Token = $"cap-place-{tokenSuffix}-ws-token123",
                Status = QrCodeStatus.Archived,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(counter, packaging, archived);
            await context.SaveChangesAsync();

            // Counter: 1 scan outside window (all-time last scan uses later in-window),
            // 2 scans in window, 1 marketing opt-in feedback in window.
            context.QrScanEvents.AddRange(
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = counter.Id,
                    CreatedAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = counter.Id,
                    CreatedAt = new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = counter.Id,
                    CreatedAt = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc),
                },
                new QrScanEvent
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = packaging.Id,
                    CreatedAt = new DateTime(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc),
                }
            );

            // Archived row exists for filter coverage but has no windowed activity,
            // so Active+Paused row sums still match location Capture performance KPIs.
            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = counter.Id,
                    GuestName = "Counter Guest",
                    GuestContact = "counter@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Counter",
                    OffersOptOut = false,
                    CreatedAt = new DateTime(2026, 7, 14, 13, 0, 0, DateTimeKind.Utc),
                },
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    QrCodeId = packaging.Id,
                    GuestName = "Packaging Guest",
                    GuestContact = "packaging@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Packaging",
                    OffersOptOut = true,
                    CreatedAt = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, counterToken);
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerWithUnscannedActiveAsync(
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
                FullName = "Capture Placements Empty Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900911",
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
                Name = "Capture Placements Empty Venue",
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
                    Token = $"cap-place-{tokenSuffix}-sg-token123",
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

        private async Task<(string Jwt, int LocationId, int QrCodeId, string Token)> SeedSingleQrCodeAsync(
            string email,
            string token,
            QrType qrType,
            QrCodeStatus status
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Transition Owner",
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
                Name = "Capture Transition Venue",
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
                QrType = qrType,
                Token = token,
                Status = status,
                CreatedAt = DateTime.UtcNow,
            };
            if (qrType == QrType.DigitalGuestLink)
            {
                qrCode.LinkName = "Pause Resume Link";
                qrCode.NormalizedLinkName = "pause resume link";
                qrCode.Channel = DigitalGuestLinkChannel.Website;
            }

            context.QrCodes.Add(qrCode);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, qrCode.Id, token);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            string SummerToken
        )> SeedDigitalGuestLinksAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Digital Owner",
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
                Name = "Capture Digital Venue",
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

            var summerToken = $"cap-dgl-{tokenSuffix}-summer-tok12";
            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.SmartGuest,
                    Token = $"cap-dgl-{tokenSuffix}-sg-token1234",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.DigitalGuestLink,
                    Token = summerToken,
                    Status = QrCodeStatus.Active,
                    LinkName = "Summer Promo",
                    NormalizedLinkName = "summer promo",
                    Channel = DigitalGuestLinkChannel.SocialMedia,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.DigitalGuestLink,
                    Token = $"cap-dgl-{tokenSuffix}-email-tok123",
                    Status = QrCodeStatus.Paused,
                    LinkName = "Email blast",
                    NormalizedLinkName = "email blast",
                    Channel = DigitalGuestLinkChannel.Email,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.DigitalGuestLink,
                    Token = $"cap-dgl-{tokenSuffix}-arch-tok1234",
                    Status = QrCodeStatus.Archived,
                    LinkName = "Old WhatsApp",
                    NormalizedLinkName = "old whatsapp",
                    Channel = DigitalGuestLinkChannel.WhatsApp,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, summerToken);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
