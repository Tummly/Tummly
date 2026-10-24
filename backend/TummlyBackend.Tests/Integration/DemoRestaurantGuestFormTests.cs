using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// The product-demo restaurant's public QR token must never store visitor
    /// Feedback, guest identity or consent, or issue Offers (ENG07).
    /// </summary>
    public class DemoRestaurantGuestFormTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        // Matches DemoSession:OwnerEmail in appsettings.Testing.json.
        private const string DemoOwnerEmail = "product-demo@example.com";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DemoRestaurantGuestFormTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SubmitFeedback_ToDemoRestaurant_StoresNothing()
        {
            const string token = "demo-guest-form-no-persist";
            var seeded = await SeedDemoRestaurantAsync(token);

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Visitor",
                    guestContact = "real.visitor@gmail.com",
                    comment = "Trying the demo.",
                    offersOptOut = false,
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("demo").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.Feedbacks.AnyAsync(f => f.RestaurantLocationId == seeded.LocationId)
            );
            Assert.False(
                await context.MasterGuests.AnyAsync(g => g.RestaurantId == seeded.RestaurantId)
            );
        }

        [Fact]
        public async Task ThankYouOfferUnlock_ToDemoRestaurant_Returns403()
        {
            const string token = "demo-guest-form-unlock";
            await SeedDemoRestaurantAsync(token);

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/thank-you-offer/unlock",
                new { unlockToken = "any-token" }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetScan_ForDemoRestaurant_StoresNoScanOrFormOpenEvents()
        {
            const string token = "demo-guest-form-scan-get";
            var seeded = await SeedDemoRestaurantAsync(token);

            var response = await _client.GetAsync($"/api/scan/{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.QrScanEvents.AnyAsync(e => e.RestaurantLocationId == seeded.LocationId)
            );
            Assert.False(
                await context.QrFormOpenEvents.AnyAsync(e => e.RestaurantLocationId == seeded.LocationId)
            );
        }

        [Fact]
        public async Task SubmitFeedback_ToMaveroDemoToken_StoresNothing_EvenWithoutOwnerEmail()
        {
            await using var factory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?> { ["DemoSession:OwnerEmail"] = "" }
                    )
                )
            );
            var seeded = await SeedRestaurantOwnedByAsync(
                factory.Services,
                "someone-else@example.com",
                DemoSessionClaims.MaveroTableTentToken
            );
            var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                $"/api/scan/{DemoSessionClaims.MaveroTableTentToken}/feedback",
                new
                {
                    guestName = "Visitor",
                    guestContact = "real.visitor@gmail.com",
                    comment = "Trying the demo.",
                    offersOptOut = false,
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.Feedbacks.AnyAsync(f => f.RestaurantLocationId == seeded.LocationId)
            );
        }

        private Task<(int RestaurantId, int LocationId)> SeedDemoRestaurantAsync(
            string linkToken
        ) => SeedRestaurantOwnedByAsync(_factory.Services, DemoOwnerEmail, linkToken);

        private static async Task<(int RestaurantId, int LocationId)> SeedRestaurantOwnedByAsync(
            IServiceProvider services,
            string ownerEmail,
            string linkToken
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var owner = await context.Users.FirstOrDefaultAsync(u => u.Email == ownerEmail);
            if (owner == null)
            {
                owner = new User
                {
                    Email = ownerEmail,
                    FullName = "Product Demo Owner",
                    PhoneNumber = "5551234500",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "Owner",
                    AccountType = "Single",
                    IsEmailVerified = true,
                    IsApprovedByAdmin = true,
                    TermsAccepted = true,
                    HasCompletedFirstSignIn = true,
                };
                context.Users.Add(owner);
                await context.SaveChangesAsync();
            }

            var restaurant = new Restaurant
            {
                Name = "Mavero Kitchen",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                Address = "1 Demo Street",
                CreatedAt = DateTime.UtcNow,
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
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            return (restaurant.Id, location.Id);
        }
    }
}
