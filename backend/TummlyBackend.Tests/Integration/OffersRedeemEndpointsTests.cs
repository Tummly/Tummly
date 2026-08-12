using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: POST /api/offers/redeem/check + /api/offers/redeem (ticket 38).
    /// </summary>
    public class OffersRedeemEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersRedeemEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CheckRedeem_ValidCode_ReturnsPreview()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-redeem-check-ok");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Maya");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var issueId = await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-CHECK1",
                claimedAt: null
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/redeem/check",
                seeded.Jwt,
                new { locationId = seeded.LocationId, code = "tum-check1" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var preview = body.GetProperty("preview");
            Assert.Equal(issueId.ToString(), preview.GetProperty("issueId").GetString());
            Assert.Equal("Maya", preview.GetProperty("guestName").GetString());
            Assert.Equal("Single-use", preview.GetProperty("usage").GetString());
        }

        [Fact]
        public async Task CheckRedeem_Expired_ReturnsReasonAndWritesFailedAttempt()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-redeem-check-expired");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Sam");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-EXP001",
                expiryAt: DateTime.UtcNow.AddMinutes(-5)
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/redeem/check",
                seeded.Jwt,
                new { locationId = seeded.LocationId, code = "TUM-EXP001" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("expired", body.GetProperty("reason").GetString());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                context.OfferRedeemFailedAttempts.Count(
                    a => a.CatalogOfferId == offerId
                )
            );
        }

        [Fact]
        public async Task MarkRedeemed_PersistsRedeemedAt()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-redeem-mark-ok");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Alex");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var issueId = await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-MARK01",
                claimedAt: null
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/redeem",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    code = "TUM-MARK01",
                    issueId = issueId.ToString(),
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var issue = await context.OfferIssues.FindAsync(issueId);
            Assert.NotNull(issue);
            Assert.NotNull(issue!.RedeemedAtUtc);
            Assert.Null(issue.ClaimedAtUtc);
        }

        [Fact]
        public async Task MarkRedeemed_WrongLocation_Fails()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-redeem-wrong-loc");
            var other = await SeedSecondOwnedLocationAsync(
                seeded.UserId,
                seeded.RestaurantId,
                "offers-redeem-wrong-loc-b"
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Lee");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var issueId = await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-WRONG1"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/redeem",
                other.Jwt,
                new
                {
                    locationId = other.LocationId,
                    code = "TUM-WRONG1",
                    issueId = issueId.ToString(),
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("wrong_location", body.GetProperty("reason").GetString());
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
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
                FullName = "Offers Redeem Owner",
                Email = $"{emailLocalPart}@example.com",
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
                Name = "Offers Redeem Venue",
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

            return (jwt, user.Id, restaurant.Id, location.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedSecondOwnedLocationAsync(
            int userId,
            int restaurantId,
            string emailLocalPart
        )
        {
            _ = emailLocalPart;
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = "Second",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var user = await context.Users.FindAsync(userId);
            Assert.NotNull(user);
            var jwt = jwtService.GenerateToken(
                user!.Id.ToString(),
                user.Email,
                user.Role
            );
            return (jwt, location.Id);
        }

        private async Task<int> SeedCatalogOfferAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var entity = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "£5 off",
                Description = "Seeded for redeem tests.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                StaffInstructions = "Apply £5 off before payment.",
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
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
                Email = $"redeem-guest-{Guid.NewGuid():N}@example.com",
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

        private async Task<int> SeedOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            string claimCode,
            DateTime? claimedAt = null,
            DateTime? expiryAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var issue = new OfferIssue
            {
                CatalogOfferId = catalogOfferId,
                LocationGuestId = locationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = now.AddDays(-1),
                ClaimedAtUtc = claimedAt,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = expiryAt ?? now.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "£5 off",
                Description = "Seeded issue",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                StaffInstructions = "Apply £5 off before payment.",
            };
            context.OfferIssues.Add(issue);
            await context.SaveChangesAsync();
            return issue.Id;
        }
    }
}
