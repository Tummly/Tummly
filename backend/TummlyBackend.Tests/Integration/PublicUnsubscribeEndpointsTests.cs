using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>api/public/unsubscribe</c> — anonymous preview / confirm / form.
    /// </summary>
    public class PublicUnsubscribeEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PublicUnsubscribeEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Preview_ReturnsValidWithRestaurantName_ForGoodToken()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "preview-ok@example.com",
                restaurantName: "Preview Venue"
            );
            var token = CreateToken(
                seeded.LocationGuestId,
                seeded.RestaurantId
            );

            var response = await _client.GetAsync(
                $"/api/public/unsubscribe/preview?t={Uri.EscapeDataString(token)}"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("valid").GetBoolean());
            Assert.Equal(
                "Preview Venue",
                body.GetProperty("restaurantName").GetString()
            );
            Assert.False(body.TryGetProperty("email", out _));
            Assert.False(body.TryGetProperty("locationGuestId", out _));
        }

        [Fact]
        public async Task Preview_ReturnsInvalid_WhenTokenTampered()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "preview-tamper@example.com",
                restaurantName: "Tamper Venue"
            );
            var token = CreateToken(
                seeded.LocationGuestId,
                seeded.RestaurantId
            );
            var parts = token.Split('.');
            var tampered = parts[0] + "x." + parts[1];

            var response = await _client.GetAsync(
                $"/api/public/unsubscribe/preview?t={Uri.EscapeDataString(tampered)}"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("valid").GetBoolean());
            Assert.False(body.TryGetProperty("restaurantName", out _));
        }

        [Fact]
        public async Task Preview_ReturnsInvalid_WhenTokenExpired()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "preview-expired@example.com",
                restaurantName: "Expired Venue"
            );
            var token = UnsubscribeToken.Create(
                seeded.LocationGuestId,
                seeded.RestaurantId,
                issuedAtUtc: new DateTime(
                    2026,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                secret: JwtSecret(),
                ttl: TimeSpan.FromHours(1)
            );

            var response = await _client.GetAsync(
                $"/api/public/unsubscribe/preview?t={Uri.EscapeDataString(token)}"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("valid").GetBoolean());
        }

        [Fact]
        public async Task Confirm_WithdrawsEmailMarketing_ForValidToken()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "confirm-ok@example.com",
                restaurantName: "Confirm Venue"
            );
            var token = CreateToken(
                seeded.LocationGuestId,
                seeded.RestaurantId
            );

            var response = await _client.PostAsJsonAsync(
                "/api/public/unsubscribe/confirm",
                new { token }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var states = await permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.EmailMarketing]
            );

            var entry = await context.LocationGuestPermissionLedgerEntries
                .SingleAsync(e =>
                    e.LocationGuestId == seeded.LocationGuestId
                    && e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Withdraw
                );
            Assert.Equal(
                LocationGuestPermissionLedgerSources.EmailUnsubscribe,
                entry.Source
            );
        }

        [Fact]
        public async Task Confirm_ReturnsSuccessWithoutWithdraw_WhenTokenTampered()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                email: "confirm-tamper@example.com",
                restaurantName: "Confirm Tamper Venue"
            );
            var token = CreateToken(
                seeded.LocationGuestId,
                seeded.RestaurantId
            );
            var parts = token.Split('.');
            var tampered = parts[0] + "x." + parts[1];

            var response = await _client.PostAsJsonAsync(
                "/api/public/unsubscribe/confirm",
                new { token = tampered }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var states = await permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                states[LocationGuestPermissionKind.EmailMarketing]
            );

            var withdrawCount = await context
                .LocationGuestPermissionLedgerEntries
                .CountAsync(e =>
                    e.LocationGuestId == seeded.LocationGuestId
                    && e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Withdraw
                );
            Assert.Equal(0, withdrawCount);
        }

        [Fact]
        public async Task Form_OnlyTouchesRequestedRestaurant()
        {
            const string sharedEmail = "form-shared@example.com";
            var restaurantA = await SeedGrantedEmailGuestAsync(
                email: sharedEmail,
                restaurantName: "Form Restaurant A"
            );
            var restaurantB = await SeedGrantedEmailGuestAsync(
                email: sharedEmail,
                restaurantName: "Form Restaurant B"
            );

            var response = await _client.PostAsJsonAsync(
                "/api/public/unsubscribe/form",
                new
                {
                    email = "  Form-Shared@Example.COM  ",
                    restaurantId = restaurantA.RestaurantId,
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();

            var statesA = await permissions.GetCurrentStatesAsync(
                restaurantA.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                statesA[LocationGuestPermissionKind.EmailMarketing]
            );

            var statesB = await permissions.GetCurrentStatesAsync(
                restaurantB.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                statesB[LocationGuestPermissionKind.EmailMarketing]
            );
        }

        [Fact]
        public async Task Form_AlwaysReturnsSuccess_WhenZeroMatches()
        {
            var restaurant = await SeedGrantedEmailGuestAsync(
                email: "someone-else@example.com",
                restaurantName: "Zero Match Venue"
            );

            var response = await _client.PostAsJsonAsync(
                "/api/public/unsubscribe/form",
                new
                {
                    email = "nobody@example.com",
                    restaurantId = restaurant.RestaurantId,
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.TryGetProperty("guestsTouched", out _));
            Assert.False(body.TryGetProperty("matchCount", out _));

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var states = await permissions.GetCurrentStatesAsync(
                restaurant.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                states[LocationGuestPermissionKind.EmailMarketing]
            );
        }

        private string CreateToken(int locationGuestId, int restaurantId)
        {
            return UnsubscribeToken.Create(
                locationGuestId,
                restaurantId,
                issuedAtUtc: DateTime.UtcNow,
                secret: JwtSecret()
            );
        }

        private string JwtSecret()
        {
            using var scope = _factory.Services.CreateScope();
            var config = scope.ServiceProvider
                .GetRequiredService<IConfiguration>();
            return config["JwtSettings:Secret"]
                ?? throw new InvalidOperationException(
                    "JwtSettings:Secret missing in test host."
                );
        }

        private async Task<SeededGuest> SeedGrantedEmailGuestAsync(
            string email,
            string restaurantName
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();

            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = $"{restaurantName} Main",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = email,
                NormalizedEmail = normalizedEmail,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(masterGuest);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                MasterGuest = masterGuest,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            var at = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            permissions.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );
            await context.SaveChangesAsync();

            return new SeededGuest(
                restaurant.Id,
                location.Id,
                guest.Id
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private sealed record SeededGuest(
            int RestaurantId,
            int LocationId,
            int LocationGuestId
        );
    }
}
