using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class ShopCartEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopCartEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCart_ReturnsEmptyLines_WhenNoCartRow()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.LocationManager,
                namedInScopeOnly: true
            );

            using var request = AuthorizedGet(
                $"/api/shop/cart?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(seeded.InScopeLocationId, body.GetProperty("locationId").GetInt32());
            Assert.Equal(0, body.GetProperty("lines").GetArrayLength());
            Assert.Equal(0, body.GetProperty("materialsNetPence").GetInt32());
            Assert.Equal("GBP", body.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task PutCartLine_UpsertReplacesQuantity()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.LocationManager,
                namedInScopeOnly: true
            );

            await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                1
            );
            var second = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                2
            );

            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var body = await ReadJsonAsync(second);
            var lines = body.GetProperty("lines");
            Assert.Equal(1, lines.GetArrayLength());
            var line = lines[0];
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                line.GetProperty("skuId").GetString()
            );
            Assert.Equal(2, line.GetProperty("quantity").GetInt32());
            Assert.Equal(
                "QR Counter Display",
                line.GetProperty("title").GetString()
            );
            Assert.Equal(1899, line.GetProperty("unitNetPence").GetInt32());
            Assert.Equal(1899, line.GetProperty("lineNetPence").GetInt32());
            Assert.Equal(1899, body.GetProperty("materialsNetPence").GetInt32());
        }

        [Fact]
        public async Task DeleteCartLine_RemovesLine()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                2
            );

            using var delete = AuthorizedDelete(
                $"/api/shop/cart/lines/{ShopMaterialsCatalogSeed.CounterDisplaySkuId}?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(delete);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("lines").GetArrayLength());
            Assert.Equal(0, body.GetProperty("materialsNetPence").GetInt32());
        }

        [Fact]
        public async Task PutCartLine_Returns400_WhenSkuInvalid()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "not-a-real-sku",
                1
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutCartLine_Returns400_WhenPackQtyInvalid()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            // Counter Display launch packs are 1 / 2 / 5 — 3 is not a pack.
            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                3
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutCartLine_Returns400_WhenSkuNotAvailableInShop()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            // Table Tent is Published but Coming soon (AvailableInShop off).
            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "table-tents",
                5
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                ShopSkuNotAvailableException.ErrorCode,
                body.GetProperty("code").GetString()
            );
            Assert.Contains(
                "not available",
                body.GetProperty("message").GetString(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PutCartLine_Returns403_WhenAdminHasViewOnly()
        {
            var seeded = await SeedAdminWithShopViewAsync();

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                1
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCart_ReturnsDistinctCarts_PerLocation()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                1
            );
            await PutLineAsync(
                seeded.MemberJwt,
                seeded.OutOfScopeLocationId,
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                2
            );

            using var firstRequest = AuthorizedGet(
                $"/api/shop/cart?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            using var secondRequest = AuthorizedGet(
                $"/api/shop/cart?locationId={seeded.OutOfScopeLocationId}",
                seeded.MemberJwt
            );

            var first = await ReadJsonAsync(await _client.SendAsync(firstRequest));
            var second = await ReadJsonAsync(await _client.SendAsync(secondRequest));

            Assert.Equal(1, first.GetProperty("lines").GetArrayLength());
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                first.GetProperty("lines")[0].GetProperty("skuId").GetString()
            );
            Assert.Equal(1, first.GetProperty("lines")[0].GetProperty("quantity").GetInt32());

            Assert.Equal(1, second.GetProperty("lines").GetArrayLength());
            Assert.Equal(
                ShopMaterialsCatalogSeed.CounterDisplaySkuId,
                second.GetProperty("lines")[0].GetProperty("skuId").GetString()
            );
            Assert.Equal(2, second.GetProperty("lines")[0].GetProperty("quantity").GetInt32());
        }

        [Fact]
        public async Task GuestCards_Available_CartWithoutOffer_Returns409OfferRequired()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", true);

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "qr-guest-cards",
                250
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                OfferCardOfferRequiredException.ErrorCode,
                body.GetProperty("code").GetString()
            );
            Assert.Equal(
                "Set up your card offer before ordering Guest Cards.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GuestCards_ComingSoon_Cart_Returns400SkuNotAvailable()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", false);

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "qr-guest-cards",
                250
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                ShopSkuNotAvailableException.ErrorCode,
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task GuestCards_OfferBelongsToAnotherLocation_Cart_Returns409OfferRequired()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", true);
            await ShopTestSeed.AttachCardOfferAsync(
                _factory,
                seeded.InScopeLocationId,
                new ShopTestSeed.CardOfferSetup(
                    OfferLocationId: seeded.OutOfScopeLocationId
                )
            );

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "qr-guest-cards",
                250
            );

            await AssertOfferRequiredAsync(response);
        }

        [Fact]
        public async Task GuestCards_ArchivedCardOffer_Cart_Returns409OfferRequired()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", true);
            await ShopTestSeed.AttachCardOfferAsync(
                _factory,
                seeded.InScopeLocationId,
                new ShopTestSeed.CardOfferSetup(
                    Status: CatalogOfferStatus.Archived
                )
            );

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "qr-guest-cards",
                250
            );

            await AssertOfferRequiredAsync(response);
        }

        [Fact]
        public async Task GuestCards_ExpiredCardOffer_Cart_Returns409OfferRequired()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", true);
            await ShopTestSeed.AttachCardOfferAsync(
                _factory,
                seeded.InScopeLocationId,
                new ShopTestSeed.CardOfferSetup(
                    Validity: CatalogOfferValidity.ChooseExpiryDate,
                    CustomExpiryDate: new DateOnly(2020, 1, 1)
                )
            );

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "qr-guest-cards",
                250
            );

            await AssertOfferRequiredAsync(response);
        }

        [Fact]
        public async Task GuestCards_PausedCardOffer_Cart_Returns409OfferRequired()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", true);
            await ShopTestSeed.AttachCardOfferAsync(
                _factory,
                seeded.InScopeLocationId,
                new ShopTestSeed.CardOfferSetup(
                    Status: CatalogOfferStatus.Paused
                )
            );

            var response = await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "qr-guest-cards",
                250
            );

            await AssertOfferRequiredAsync(response);
        }

        private static async Task AssertOfferRequiredAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                OfferCardOfferRequiredException.ErrorCode,
                body.GetProperty("code").GetString()
            );
        }

        private async Task<HttpResponseMessage> PutLineAsync(
            string jwt,
            int locationId,
            string skuId,
            int quantity
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/shop/cart/lines"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    locationId,
                    skuId,
                    quantity,
                }),
                Encoding.UTF8,
                "application/json"
            );
            return await _client.SendAsync(request);
        }

        private async Task<PermissionSeed> SeedAdminWithShopViewAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Shop Cart Owner View",
                Email = $"owner-shop-14-view-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
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
                Name = "Shop Cart View Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "Shop Cart View Admin",
                Email = $"admin-shop-14-view-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900113",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Admin,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            context.RestaurantAdminPermissionCells.Add(
                new RestaurantAdminPermissionCell
                {
                    RestaurantId = restaurant.Id,
                    AreaId = OperatorAreaIds.TummlyShop,
                    Level = PermissionLevel.View,
                }
            );
            await context.SaveChangesAsync();

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new PermissionSeed(memberJwt, location.Id, location.Id);
        }

        private async Task<PermissionSeed> SeedOwnerAndMemberAsync(
            string memberRole,
            bool namedInScopeOnly
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Shop Cart Owner",
                Email = $"owner-shop-14-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
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
                Name = "Shop Cart Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(inScope, outOfScope);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "Shop Cart Member",
                Email = $"member-shop-14-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900113",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            var namedIds = namedInScopeOnly
                ? new[] { inScope.Id }
                : new[] { inScope.Id, outOfScope.Id };

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = memberRole,
                LocationScope = namedInScopeOnly
                    ? LocationScopeKind.NamedList
                    : LocationScopeKind.AllLocations,
                NamedLocationIdsJson = namedInScopeOnly
                    ? MembershipLocationScope.SerializeNamedIds(namedIds)
                    : "[]",
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new PermissionSeed(
                memberJwt,
                inScope.Id,
                outOfScope.Id
            );
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedDelete(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
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

        private sealed record PermissionSeed(
            string MemberJwt,
            int InScopeLocationId,
            int OutOfScopeLocationId
        );
    }
}
