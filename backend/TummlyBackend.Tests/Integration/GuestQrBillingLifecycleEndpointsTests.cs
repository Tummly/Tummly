using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class GuestQrBillingLifecycleEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestQrBillingLifecycleEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetScan_WhenDormant_Returns200WithDormantPayload()
        {
            const string token = "guest-dormant-resolve-token-1234";
            const string logoKey = "brand-logos/dormant-logo-abc.png";
            await SeedGuestWorkspaceAsync(
                token,
                restaurantName: "Dormant Fork",
                billingStatus: BillingStatuses.Pilot,
                pilotPeriodEnd: DateTime.UtcNow.AddHours(
                    -BillingAccountLifecycleService.PilotDormantHours - 1
                ),
                brandLogoObjectKey: logoKey
            );

            var response = await _client.GetAsync($"/api/scan/{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("dormant", body.GetProperty("status").GetString());
            Assert.Equal(
                "Dormant Fork",
                body.GetProperty("restaurantName").GetString()
            );
            Assert.Equal(
                BrandLogoRules.BuildPublicUrl(logoKey),
                body.GetProperty("brandLogoPublicUrl").GetString()
            );
            Assert.False(body.TryGetProperty("locationName", out _));
            Assert.False(body.TryGetProperty("success", out _));
        }

        [Fact]
        public async Task SubmitFeedback_WhenDormant_IsDenied()
        {
            const string token = "guest-dormant-write-token-123456";
            await SeedGuestWorkspaceAsync(
                token,
                restaurantName: "Dormant Write Venue",
                billingStatus: BillingStatuses.Dormant,
                pilotPeriodEnd: DateTime.UtcNow.AddDays(-20)
            );

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Jane Doe",
                    guestContact = "jane@example.com",
                    comment = "Still open?",
                    offersOptOut = false
                }
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "Link not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetScan_WhenSoftLock_ReturnsLiveFeedbackMetadata()
        {
            const string token = "guest-soft-lock-read-token-1234";
            await SeedGuestWorkspaceAsync(
                token,
                restaurantName: "Soft Lock Read Venue",
                billingStatus: BillingStatuses.SoftLock,
                softLockEnteredAt: DateTime.UtcNow.AddDays(-2),
                pilotPeriodEnd: DateTime.UtcNow.AddDays(-2)
            );

            var response = await _client.GetAsync($"/api/scan/{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Soft Lock Read Venue",
                body.GetProperty("restaurantName").GetString()
            );
            Assert.Equal("Main", body.GetProperty("locationName").GetString());
            Assert.False(body.TryGetProperty("status", out _));
        }

        [Fact]
        public async Task SubmitFeedback_WhenSoftLock_Succeeds()
        {
            const string token = "guest-soft-lock-write-token-123";
            await SeedGuestWorkspaceAsync(
                token,
                restaurantName: "Soft Lock Venue",
                billingStatus: BillingStatuses.SoftLock,
                softLockEnteredAt: DateTime.UtcNow.AddDays(-2),
                pilotPeriodEnd: DateTime.UtcNow.AddDays(-2)
            );

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Sam Guest",
                    guestContact = "sam@example.com",
                    comment = "Soft lock still takes feedback.",
                    offersOptOut = false
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task GetScan_WhenPaused_StaysNotFound()
        {
            const string token = "guest-paused-resolve-token-1234";
            await SeedGuestWorkspaceAsync(
                token,
                restaurantName: "Paused Venue",
                billingStatus: BillingStatuses.Active,
                workspaceStatus: WorkspaceStatus.Paused
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
        public async Task GetScan_InvalidToken_StaysNotFound()
        {
            var response = await _client.GetAsync(
                "/api/scan/invalid-token-guest-qr-32"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "Link not found.",
                body.GetProperty("message").GetString()
            );
        }

        private async Task SeedGuestWorkspaceAsync(
            string linkToken,
            string restaurantName,
            string billingStatus,
            DateTime? pilotPeriodEnd = null,
            DateTime? softLockEnteredAt = null,
            WorkspaceStatus workspaceStatus = WorkspaceStatus.Active,
            string? brandLogoObjectKey = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 900_032,
                CreatedAt = now,
                WorkspaceStatus = workspaceStatus,
                BrandLogoObjectKey = brandLogoObjectKey,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var account = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "pricebook-pilot-test"
            );
            account.BillingStatus = billingStatus;
            account.PilotPeriodEnd = pilotPeriodEnd;
            account.SoftLockEnteredAt = softLockEnteredAt;
            if (billingStatus == BillingStatuses.Dormant)
            {
                account.DormantEnteredAt =
                    softLockEnteredAt ?? pilotPeriodEnd ?? now.AddDays(-5);
            }

            context.BillingAccounts.Add(account);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.SmartGuest,
                    Token = linkToken,
                    Status = QrCodeStatus.Active,
                    CreatedAt = now,
                }
            );
            await context.SaveChangesAsync();
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
