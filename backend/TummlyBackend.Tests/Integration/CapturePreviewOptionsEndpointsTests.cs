using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CapturePreviewOptionsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CapturePreviewOptionsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCapturePreviewOptions_ReturnsActiveAndPausedLabelFieldsOnly()
        {
            var seeded = await SeedPreviewCodesAsync(
                email: "capture-preview-options-labels@example.com",
                tokenSuffix: "labels"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PreviewOptionsUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var items = body.GetProperty("items");
            Assert.Equal(JsonValueKind.Array, items.ValueKind);
            Assert.Equal(3, items.GetArrayLength());

            var byId = items
                .EnumerateArray()
                .ToDictionary(item => item.GetProperty("qrCodeId").GetInt32());

            Assert.True(byId.ContainsKey(seeded.ActiveId));
            Assert.True(byId.ContainsKey(seeded.PausedId));
            Assert.True(byId.ContainsKey(seeded.DigitalId));
            Assert.False(byId.ContainsKey(seeded.ArchivedId));

            Assert.Equal("CounterCard", byId[seeded.ActiveId].GetProperty("qrType").GetString());
            Assert.Equal("Active", byId[seeded.ActiveId].GetProperty("status").GetString());

            Assert.Equal("PackagingSticker", byId[seeded.PausedId].GetProperty("qrType").GetString());
            Assert.Equal("Paused", byId[seeded.PausedId].GetProperty("status").GetString());

            Assert.Equal("DigitalGuestLink", byId[seeded.DigitalId].GetProperty("qrType").GetString());
            Assert.Equal("Active", byId[seeded.DigitalId].GetProperty("status").GetString());
            Assert.Equal("WhatsApp list", byId[seeded.DigitalId].GetProperty("linkName").GetString());

            foreach (var item in items.EnumerateArray())
            {
                Assert.False(item.TryGetProperty("qrScans", out _));
                Assert.False(item.TryGetProperty("feedbackSubmitted", out _));
                Assert.False(item.TryGetProperty("marketingOptIns", out _));
                Assert.False(item.TryGetProperty("offerClaims", out _));
                Assert.False(item.TryGetProperty("qrLinkUrl", out _));
            }

            Assert.False(body.TryGetProperty("qrScans", out _));
            Assert.False(body.TryGetProperty("from", out _));
            Assert.False(body.TryGetProperty("to", out _));
        }

        [Fact]
        public async Task GetCapturePreviewOptions_ReturnsEmptyItems_WhenNoActiveOrPausedCodes()
        {
            var seeded = await SeedOwnerLocationWithoutCodesAsync(
                email: "capture-preview-options-empty@example.com",
                tokenSuffix: "empty"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PreviewOptionsUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var items = body.GetProperty("items");
            Assert.Equal(JsonValueKind.Array, items.ValueKind);
            Assert.Equal(0, items.GetArrayLength());
        }

        [Fact]
        public async Task GetCapturePreviewOptions_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerLocationWithoutCodesAsync(
                email: "capture-preview-options-owner-a@example.com",
                tokenSuffix: "ownera"
            );
            var other = await SeedOwnerLocationWithoutCodesAsync(
                email: "capture-preview-options-owner-b@example.com",
                tokenSuffix: "ownerb"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                PreviewOptionsUrl(other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCapturePreviewOptions_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(PreviewOptionsUrl(1));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static string PreviewOptionsUrl(int locationId)
        {
            return $"/api/capture/locations/{locationId}/preview-options";
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int ActiveId,
            int PausedId,
            int DigitalId,
            int ArchivedId
        )> SeedPreviewCodesAsync(string email, string tokenSuffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Capture Preview Options Owner",
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
                Name = "Capture Preview Options Venue",
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
                Token = $"cap-prev-{tokenSuffix}-active-token123",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var paused = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.PackagingSticker,
                Token = $"cap-prev-{tokenSuffix}-paused-token123",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            var digital = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.DigitalGuestLink,
                Token = $"cap-prev-{tokenSuffix}-digital-token123",
                Status = QrCodeStatus.Active,
                LinkName = "WhatsApp list",
                NormalizedLinkName = "whatsapp list",
                Channel = DigitalGuestLinkChannel.WhatsApp,
                CreatedAt = DateTime.UtcNow,
            };
            var archived = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.WindowSticker,
                Token = $"cap-prev-{tokenSuffix}-archived-token123",
                Status = QrCodeStatus.Archived,
                CreatedAt = DateTime.UtcNow,
                ArchivedAt = DateTime.UtcNow,
            };
            context.QrCodes.AddRange(active, paused, digital, archived);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (
                jwt,
                location.Id,
                active.Id,
                paused.Id,
                digital.Id,
                archived.Id
            );
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerLocationWithoutCodesAsync(
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
                FullName = "Capture Preview Options Empty Owner",
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
                Name = $"Capture Preview Options Empty {tokenSuffix}",
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

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }
    }
}
