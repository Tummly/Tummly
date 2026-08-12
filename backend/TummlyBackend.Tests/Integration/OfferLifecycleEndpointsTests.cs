using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: GET /api/offers/{id}/claims + /redemptions (ticket 40).
    /// </summary>
    public class OfferLifecycleEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OfferLifecycleEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListClaims_ReturnsIssueRowsForOffer()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-lifecycle-claims");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Maya");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-CLM001",
                claimedAt: DateTime.UtcNow.AddDays(-2)
            );

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/claims",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var items = body.GetProperty("items");
            Assert.Equal(1, items.GetArrayLength());
            var row = items[0];
            Assert.Equal("Maya", row.GetProperty("guestName").GetString());
            Assert.Equal("TUM-CLM001", row.GetProperty("claimCode").GetString());
            Assert.Equal("open", row.GetProperty("status").GetString());
            Assert.Equal("Open", row.GetProperty("statusLabel").GetString());
        }

        [Fact]
        public async Task ListClaims_ReturnsEmpty_WhenNoIssues()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-lifecycle-claims-empty");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/claims",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task ListRedemptions_ReturnsRedeemedAndFailedRows()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-lifecycle-redemptions");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Alex");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var redeemedAt = DateTime.UtcNow.AddHours(-3);
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-RED001",
                claimedAt: DateTime.UtcNow.AddDays(-1),
                redeemedAt: redeemedAt
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                claimCode: "TUM-RED001",
                reason: "already_used",
                attemptedAt: DateTime.UtcNow.AddHours(-1)
            );

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/redemptions",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var items = body.GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());
            Assert.Equal("failed", items[0].GetProperty("kind").GetString());
            Assert.Equal("redeemed", items[1].GetProperty("kind").GetString());
            Assert.Equal(
                "Redeemed",
                items[1].GetProperty("outcomeLabel").GetString()
            );
        }

        [Fact]
        public async Task ListClaims_ReturnsNotFound_ForMissingOffer()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-lifecycle-missing");

            using var request = AuthorizedGet(
                "/api/offers/999999/claims",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
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
            int UserId,
            int RestaurantId,
            int LocationId
        )> SeedOwnerWithLocationAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Offer Lifecycle Owner",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900456",
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
                Name = "Offer Lifecycle Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
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

            return (jwt, user.Id, restaurant.Id, location.Id);
        }

        private async Task<int> SeedLocationGuestAsync(
            int locationId,
            string name
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var location = await context.RestaurantLocations
                .FindAsync(locationId);
            Assert.NotNull(location);

            var master = new MasterGuest
            {
                RestaurantId = location!.RestaurantId,
                Email = $"lifecycle-guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = name,
                CreatedAt = now,
            };
            context.LocationGuests.Add(lg);
            await context.SaveChangesAsync();
            return lg.Id;
        }

        private async Task<int> SeedCatalogOfferAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "10% off next visit",
                Description = "Lifecycle test offer",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();
            return offer.Id;
        }

        private async Task SeedOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            string claimCode,
            DateTime? claimedAt,
            DateTime? redeemedAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var issuedAt = DateTime.UtcNow.AddDays(-7);

            context.OfferIssues.Add(new OfferIssue
            {
                CatalogOfferId = catalogOfferId,
                LocationGuestId = locationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = issuedAt,
                ClaimedAtUtc = claimedAt,
                RedeemedAtUtc = redeemedAt,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = issuedAt.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "10% off next visit",
                Description = "Lifecycle test issue",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
            });
            await context.SaveChangesAsync();
        }

        private async Task SeedFailedAttemptAsync(
            int catalogOfferId,
            int locationId,
            string claimCode,
            string reason,
            DateTime attemptedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.OfferRedeemFailedAttempts.Add(new OfferRedeemFailedAttempt
            {
                CatalogOfferId = catalogOfferId,
                RestaurantLocationId = locationId,
                ClaimCode = claimCode,
                Reason = reason,
                AttemptedAtUtc = attemptedAt,
            });
            await context.SaveChangesAsync();
        }
    }
}
