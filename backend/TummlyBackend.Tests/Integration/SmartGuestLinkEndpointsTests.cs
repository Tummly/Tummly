using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class SmartGuestLinkEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SmartGuestLinkEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetScan_ReturnsLocationMetadata_ForValidToken()
        {
            const string token = "guest-token-123456789012345678";

            await SeedGuestLocationAsync(
                token,
                restaurantName: "The Golden Fork",
                locationName: "Main"
            );

            var response = await _client.GetAsync($"/api/scan/{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "The Golden Fork",
                body.GetProperty("restaurantName").GetString()
            );
            Assert.Equal(
                "Main",
                body.GetProperty("locationName").GetString()
            );
            Assert.Equal(
                "1 High Street",
                body.GetProperty("address").GetString()
            );
        }

        [Fact]
        public async Task GetScan_RecordsQrScanEvent_ForValidToken()
        {
            const string token = "guest-token-scan-event-123456789";

            await SeedGuestLocationAsync(
                token,
                restaurantName: "The Golden Fork",
                locationName: "Main"
            );

            var response = await _client.GetAsync($"/api/scan/{token}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var locationId = await context.QrCodes
                .Where(qrCode => qrCode.Token == token)
                .Select(qrCode => qrCode.RestaurantLocationId)
                .SingleAsync();

            var scanCount = await context.QrScanEvents.CountAsync(e =>
                e.RestaurantLocationId == locationId
            );
            Assert.Equal(1, scanCount);
        }

        [Fact]
        public async Task GetScan_DoesNotRecordQrScanEvent_ForUnknownToken()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var beforeCount = await context.QrScanEvents.CountAsync();

                var response = await _client.GetAsync(
                    "/api/scan/missing-token-no-scan"
                );

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

                var afterCount = await context.QrScanEvents.CountAsync();
                Assert.Equal(beforeCount, afterCount);
            }
        }

        [Fact]
        public async Task GetScan_Returns404_ForUnknownToken()
        {
            var response = await _client.GetAsync(
                "/api/scan/missing-token"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Link not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Theory]
        [InlineData(QrCodeStatus.Paused)]
        [InlineData(QrCodeStatus.Archived)]
        public async Task GetScan_Returns404_ForInactiveQrCode_WithoutLeakingStatus(
            QrCodeStatus status
        )
        {
            const string token = "guest-token-inactive-qr-code-12";
            await SeedGuestLocationAsync(
                token,
                restaurantName: "The Golden Fork",
                locationName: "Main",
                status: status
            );

            var response = await _client.GetAsync($"/api/scan/{token}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "Link not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetScan_RecordsQrCodeIdOnScanEvent_ForValidToken()
        {
            const string token = "guest-token-scan-qr-code-id-1234";

            await SeedGuestLocationAsync(
                token,
                restaurantName: "The Golden Fork",
                locationName: "Main"
            );

            var response = await _client.GetAsync($"/api/scan/{token}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var qrCodeId = await context.QrCodes
                .Where(qrCode => qrCode.Token == token)
                .Select(qrCode => qrCode.Id)
                .SingleAsync();

            var scanEvent = await context.QrScanEvents
                .SingleAsync(e => e.QrCodeId == qrCodeId);

            Assert.Equal(qrCodeId, scanEvent.QrCodeId);
        }

        [Theory]
        [InlineData(QrCodeStatus.Paused)]
        [InlineData(QrCodeStatus.Archived)]
        public async Task SubmitFeedback_Returns404_ForInactiveQrCode(
            QrCodeStatus status
        )
        {
            var token = $"feedback-inactive-{status.ToString().ToLowerInvariant()}";
            await SeedGuestLocationAsync(
                token,
                "The Golden Fork",
                "Main",
                status: status
            );

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Alex Guest",
                    guestContact = "alex@example.com",
                    comment = "A useful visit."
                }
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SubmitFeedback_PersistsQrCodeId()
        {
            const string token = "feedback-qr-code-id-1234567890";
            await SeedGuestLocationAsync(token, "The Golden Fork", "Main");

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Alex Guest",
                    guestContact = "alex@example.com",
                    comment = "A useful visit."
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var qrCode = await context.QrCodes.SingleAsync(q => q.Token == token);
            var feedback = await context.Feedbacks
                .SingleAsync(f => f.RestaurantLocationId == qrCode.RestaurantLocationId);

            Assert.Equal(qrCode.Id, feedback.QrCodeId);
        }

        [Fact]
        public async Task GetLocations_ReturnsEmptyGuestUrl_WhenNoActiveSmartGuestQrCode()
        {
            const string linkToken = "paused-owner-location-token123";

            var jwt = await SeedOwnerLocationAsync(
                linkToken,
                status: QrCodeStatus.Paused
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var location = body.GetProperty("locations")[0];

            Assert.Equal("", location.GetProperty("guestUrl").GetString());
        }

        [Fact]
        public async Task GetScan_ReturnsEmptyAddress_WhenLocationAddressIsBlank()
        {
            const string token = "guest-token-blank-address";
            await SeedGuestLocationAsync(
                token,
                restaurantName: "The Golden Fork",
                locationName: "Main",
                address: ""
            );

            var response = await _client.GetAsync($"/api/scan/{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("", body.GetProperty("address").GetString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SubmitFeedback_PersistsOffersOptOut(
            bool offersOptOut
        )
        {
            var token = $"offers-opt-out-{offersOptOut.ToString().ToLowerInvariant()}";
            await SeedGuestLocationAsync(token, "The Golden Fork", "Main");

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Alex Guest",
                    guestContact = "alex@example.com",
                    comment = "A useful visit.",
                    offersOptOut
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var locationId = await context.QrCodes
                .Where(qrCode => qrCode.Token == token)
                .Select(qrCode => qrCode.RestaurantLocationId)
                .SingleAsync();
            var feedback = await context.Feedbacks
                .SingleAsync(item => item.RestaurantLocationId == locationId);

            Assert.Equal(offersOptOut, feedback.OffersOptOut);
        }

        [Fact]
        public async Task SubmitFeedback_DefaultsOffersOptOutToFalse_WhenOmitted()
        {
            const string token = "offers-opt-out-default";
            await SeedGuestLocationAsync(token, "The Golden Fork", "Main");

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Alex Guest",
                    guestContact = "alex@example.com",
                    comment = "A useful visit."
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var locationId = await context.QrCodes
                .Where(qrCode => qrCode.Token == token)
                .Select(qrCode => qrCode.RestaurantLocationId)
                .SingleAsync();
            var feedback = await context.Feedbacks
                .SingleAsync(item => item.RestaurantLocationId == locationId);

            Assert.False(feedback.OffersOptOut);
        }

        [Fact]
        public async Task GetLocations_ReturnsGuestUrl_ForAuthenticatedOwner()
        {
            const string linkToken = "owner-location-token1234567890";

            var jwt = await SeedOwnerLocationAsync(linkToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var locations = body.GetProperty("locations");
            Assert.Equal(JsonValueKind.Array, locations.ValueKind);
            Assert.Equal(1, locations.GetArrayLength());

            var location = locations[0];
            Assert.Equal(
                $"https://tummly.example/scan/{linkToken}",
                location.GetProperty("guestUrl").GetString()
            );
            Assert.False(
                location.TryGetProperty("linkToken", out _)
            );
        }

        [Fact]
        public async Task GetLocations_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/restaurant/locations"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task SeedGuestLocationAsync(
            string linkToken,
            string restaurantName,
            string locationName,
            string address = "1 High Street",
            QrCodeStatus status = QrCodeStatus.Active
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 999_999,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = address,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = linkToken,
                Status = status,
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        private async Task<string> SeedOwnerLocationAsync(
            string linkToken,
            QrCodeStatus status = QrCodeStatus.Active
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Alex Owner",
                Email = "owner@example.com",
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
                Name = "The Golden Fork",
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

            context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = linkToken,
                Status = status,
                CreatedAt = DateTime.UtcNow,
            });
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
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
