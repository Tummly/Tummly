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
    public class ShopCatalogEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopCatalogEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCatalog_Returns200_WithSixSkus_WhenViewInScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.LocationManager,
                namedInScopeOnly: true
            );

            using var request = AuthorizedGet(
                $"/api/shop/catalog?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "tummly_uk_materials_catalog_v1",
                body.GetProperty("catalogVersion").GetString()
            );

            var items = body.GetProperty("items");
            Assert.Equal(6, items.GetArrayLength());

            var tableTents = items.EnumerateArray()
                .First(row =>
                    row.GetProperty("skuId").GetString() == "table-tents"
                );
            Assert.Equal("Table tents", tableTents.GetProperty("title").GetString());
            Assert.Equal(2400, tableTents.GetProperty("unitNetPence").GetInt32());
            Assert.True(tableTents.GetProperty("isPlanIncluded").GetBoolean());
            Assert.Equal(
                "Essential",
                tableTents.GetProperty("popularBadge").GetString()
            );
        }

        [Fact]
        public async Task GetCatalog_Returns403_WhenStaffHasNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Staff,
                namedInScopeOnly: true
            );

            using var request = AuthorizedGet(
                $"/api/shop/catalog?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCatalog_Returns400_WhenLocationIdMissing()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            using var request = AuthorizedGet(
                "/api/shop/catalog",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCatalog_Returns404_WhenLocationUnknown()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            using var request = AuthorizedGet(
                "/api/shop/catalog?locationId=999999",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCatalog_Returns403_WhenLocationOutsideScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.LocationManager,
                namedInScopeOnly: true
            );

            using var request = AuthorizedGet(
                $"/api/shop/catalog?locationId={seeded.OutOfScopeLocationId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCatalogItem_ReturnsDetail_ForKnownSku()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            using var request = AuthorizedGet(
                $"/api/shop/catalog/receipt-stickers?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var item = body.GetProperty("item");
            Assert.Equal("receipt-stickers", item.GetProperty("skuId").GetString());
            Assert.Equal("ReceiptSticker", item.GetProperty("qrType").GetString());
            Assert.True(item.GetProperty("mintOnShopFulfilment").GetBoolean());
            Assert.Equal(
                "tummly_uk_materials_catalog_v1",
                item.GetProperty("catalogVersion").GetString()
            );
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
                FullName = "Shop Catalog Owner",
                Email = $"owner-shop-13-{Guid.NewGuid():N}@example.com",
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
                Name = "Shop Catalog Venue",
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
                FullName = "Shop Catalog Member",
                Email = $"member-shop-13-{Guid.NewGuid():N}@example.com",
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
