using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class ShopInterestEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopInterestEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetInterestProducts_ReturnsEmpty_AtLaunch()
        {
            await ClearInterestTablesAsync();
            var seeded = await SeedOperatorAndAdminAsync();

            using var request = AuthorizedGet(
                $"/api/shop/interest/products?locationId={seeded.LocationId}",
                seeded.OwnerJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task PostInterest_RecordsProductBusinessLocationUserTimestamp_AndThanks()
        {
            var seeded = await SeedOperatorAndAdminAsync();
            await SeedInterestProductAsync(
                "tray-liners",
                "Tray liners",
                "Custom tray liner concepts."
            );

            using var request = AuthorizedPostJson(
                "/api/shop/interest",
                seeded.OwnerJwt,
                new
                {
                    productKey = "tray-liners",
                    locationId = seeded.LocationId,
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Thanks — we have noted your interest.",
                body.GetProperty("message").GetString()
            );
            Assert.False(body.TryGetProperty("checkoutUrl", out _));
            Assert.False(body.TryGetProperty("cart", out _));

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var row = Assert.Single(
                context.ShopInterestRegistrations
                    .Where(r =>
                        r.RestaurantId == seeded.RestaurantId
                        && r.UserId == seeded.OwnerUserId
                        && r.ProductKey == "tray-liners"
                    )
                    .ToList()
            );
            Assert.Equal("Tray liners", row.ProductTitleSnapshot);
            Assert.Equal(seeded.RestaurantName, row.RestaurantNameSnapshot);
            Assert.Equal(seeded.LocationId, row.LocationId);
            Assert.Equal(seeded.LocationName, row.LocationNameSnapshot);
            Assert.Equal(seeded.OwnerFullName, row.UserFullNameSnapshot);
            Assert.Equal(seeded.OwnerEmail, row.UserEmailSnapshot);
            Assert.True(row.CreatedAtUtc <= DateTime.UtcNow.AddSeconds(5));
            Assert.True(row.CreatedAtUtc >= DateTime.UtcNow.AddMinutes(-2));
        }

        [Fact]
        public async Task PostInterest_DoesNotCreateCartLines()
        {
            var seeded = await SeedOperatorAndAdminAsync();
            await SeedInterestProductAsync(
                "stickers",
                "Stickers",
                "Future sticker concepts."
            );

            using var interest = AuthorizedPostJson(
                "/api/shop/interest",
                seeded.OwnerJwt,
                new
                {
                    productKey = "stickers",
                    locationId = seeded.LocationId,
                }
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(interest)).StatusCode);

            using var cart = AuthorizedGet(
                $"/api/shop/cart?locationId={seeded.LocationId}",
                seeded.OwnerJwt
            );
            var cartResponse = await _client.SendAsync(cart);
            Assert.Equal(HttpStatusCode.OK, cartResponse.StatusCode);
            var cartBody = await ReadJsonAsync(cartResponse);
            Assert.Equal(0, cartBody.GetProperty("lines").GetArrayLength());
        }

        [Fact]
        public async Task PostInterest_RejectsUnknownProduct()
        {
            var seeded = await SeedOperatorAndAdminAsync();

            using var request = AuthorizedPostJson(
                "/api/shop/interest",
                seeded.OwnerJwt,
                new
                {
                    productKey = "does-not-exist",
                    locationId = seeded.LocationId,
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AdminList_ReturnsRegistrationsAndCountsByProduct()
        {
            await ClearInterestTablesAsync();
            var seeded = await SeedOperatorAndAdminAsync();
            await SeedInterestProductAsync("tray-liners", "Tray liners", "A");
            await SeedInterestProductAsync("stickers", "Stickers", "B");

            // Register is idempotent per user+product+location — second tray post
            // must not create a duplicate row.
            for (var i = 0; i < 2; i++)
            {
                using var post = AuthorizedPostJson(
                    "/api/shop/interest",
                    seeded.OwnerJwt,
                    new
                    {
                        productKey = "tray-liners",
                        locationId = seeded.LocationId,
                    }
                );
                Assert.Equal(
                    HttpStatusCode.OK,
                    (await _client.SendAsync(post)).StatusCode
                );
            }

            using var sticker = AuthorizedPostJson(
                "/api/shop/interest",
                seeded.OwnerJwt,
                new
                {
                    productKey = "stickers",
                    locationId = seeded.LocationId,
                }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(sticker)).StatusCode
            );

            using var adminGet = AuthorizedGet(
                "/api/admin/shop-interest",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(adminGet);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(2, body.GetProperty("items").GetArrayLength());

            var counts = body.GetProperty("countsByProduct");
            Assert.Equal(2, counts.GetArrayLength());
            var tray = counts.EnumerateArray()
                .First(row =>
                    row.GetProperty("productKey").GetString() == "tray-liners"
                );
            Assert.Equal(1, tray.GetProperty("count").GetInt32());
            Assert.Equal("Tray liners", tray.GetProperty("title").GetString());
            var stickers = counts.EnumerateArray()
                .First(row =>
                    row.GetProperty("productKey").GetString() == "stickers"
                );
            Assert.Equal(1, stickers.GetProperty("count").GetInt32());
        }

        [Fact]
        public async Task PostInterest_RejectsInactivePromotedProduct()
        {
            var seeded = await SeedOperatorAndAdminAsync();
            await SeedInterestProductAsync(
                "promoted-tray",
                "Promoted tray",
                "Was interest, now shop.",
                isActive: false
            );

            using var request = AuthorizedPostJson(
                "/api/shop/interest",
                seeded.OwnerJwt,
                new
                {
                    productKey = "promoted-tray",
                    locationId = seeded.LocationId,
                }
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AdminInterestProducts_ListIncludesActiveAndPromoted()
        {
            await ClearInterestTablesAsync();
            var seeded = await SeedOperatorAndAdminAsync();
            await SeedInterestProductAsync("active-one", "Active", "A");
            await SeedInterestProductAsync(
                "shop-one",
                "In Shop",
                "B",
                isActive: false,
                linkedSkuId: "shop-one"
            );

            using var request = AuthorizedGet(
                "/api/admin/shop-interest/products",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        }

        private async Task ClearInterestTablesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            context.ShopInterestRegistrations.RemoveRange(
                context.ShopInterestRegistrations
            );
            context.ShopInterestProductImages.RemoveRange(
                context.ShopInterestProductImages
            );
            context.ShopInterestProducts.RemoveRange(
                context.ShopInterestProducts
            );
            await context.SaveChangesAsync();
        }

        private async Task SeedInterestProductAsync(
            string productKey,
            string title,
            string description,
            bool isActive = true,
            string? linkedSkuId = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            if (
                context.ShopInterestProducts.Any(row =>
                    row.ProductKey == productKey
                )
            )
            {
                return;
            }

            context.ShopInterestProducts.Add(
                new ShopInterestProduct
                {
                    ProductKey = productKey,
                    Title = title,
                    Description = description,
                    Material = string.Empty,
                    Dimensions = string.Empty,
                    LinkedSkuId = linkedSkuId,
                    IsActive = isActive,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task<Seeded> SeedOperatorAndAdminAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var ownerEmail = $"owner-interest-08-{Guid.NewGuid():N}@example.com";
            var owner = new User
            {
                FullName = "Interest Owner",
                Email = ownerEmail,
                PasswordHash = "hash",
                PhoneNumber = "07700900801",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Interest Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main Dining",
                Address = "8 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            owner.SelectedRestaurantId = restaurant.Id;

            var tummlyAdmin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-interest-08-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(tummlyAdmin);
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                restaurant.Name,
                location.Id,
                location.LocationName,
                owner.Id,
                owner.FullName,
                owner.Email,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateAdminToken(tummlyAdmin)
            );
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPostJson(
            string url,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record Seeded(
            int RestaurantId,
            string RestaurantName,
            int LocationId,
            string LocationName,
            int OwnerUserId,
            string OwnerFullName,
            string OwnerEmail,
            string OwnerJwt,
            string AdminJwt
        );
    }
}
