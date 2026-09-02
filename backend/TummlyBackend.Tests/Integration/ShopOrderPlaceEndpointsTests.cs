using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class ShopOrderPlaceEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopOrderPlaceEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PlaceFromCart_ClearsCart_AndCreatesAwaitingPaymentOrder()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "table-tents",
                2
            );
            await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "window-stickers",
                4
            );

            // materials: 2*2400 + 4*1400 = 10400; vat 2080; delivery 0; gross 12480
            var place = await PlaceOrderAsync(
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    fromCart = true,
                    deliveryMethod = "standard",
                    expectedGrossPence = 12480,
                    shipTo = ValidShipTo(),
                }
            );

            Assert.Equal(HttpStatusCode.OK, place.StatusCode);
            var body = await ReadJsonAsync(place);
            Assert.Equal("awaiting_payment", body.GetProperty("paymentStatus").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("fulfilmentStatus").ValueKind);
            Assert.Equal("ORD-1", body.GetProperty("orderNumber").GetString());
            Assert.Equal(10400, body.GetProperty("materialsNetPence").GetInt32());
            Assert.Equal(2080, body.GetProperty("vatPence").GetInt32());
            Assert.Equal(0, body.GetProperty("deliveryNetPence").GetInt32());
            Assert.Equal(12480, body.GetProperty("grossPence").GetInt32());
            Assert.Equal(2, body.GetProperty("lines").GetArrayLength());

            using var cartRequest = AuthorizedGet(
                $"/api/shop/cart?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var cart = await ReadJsonAsync(await _client.SendAsync(cartRequest));
            Assert.Equal(0, cart.GetProperty("lines").GetArrayLength());
        }

        [Fact]
        public async Task PlaceExpress_LeavesCartIntact()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            await PutLineAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "table-tents",
                5
            );

            // express line: 3*2400 = 7200; vat 1440; delivery 2000; gross 10640
            var place = await PlaceOrderAsync(
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    lines = new[]
                    {
                        new { skuId = "table-tents", quantity = 3 },
                    },
                    deliveryMethod = "express",
                    expectedGrossPence = 10640,
                    shipTo = ValidShipTo(),
                }
            );

            Assert.Equal(HttpStatusCode.OK, place.StatusCode);
            var body = await ReadJsonAsync(place);
            Assert.Equal("ORD-1", body.GetProperty("orderNumber").GetString());
            Assert.Equal(1, body.GetProperty("lines").GetArrayLength());
            Assert.Equal(2000, body.GetProperty("deliveryNetPence").GetInt32());

            using var cartRequest = AuthorizedGet(
                $"/api/shop/cart?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var cart = await ReadJsonAsync(await _client.SendAsync(cartRequest));
            Assert.Equal(1, cart.GetProperty("lines").GetArrayLength());
            Assert.Equal(5, cart.GetProperty("lines")[0].GetProperty("quantity").GetInt32());
        }

        [Fact]
        public async Task PlaceOrder_Returns400_WhenShipToInvalid()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            var place = await PlaceOrderAsync(
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    lines = new[]
                    {
                        new { skuId = "table-tents", quantity = 1 },
                    },
                    deliveryMethod = "standard",
                    expectedGrossPence = 2880,
                    shipTo = new
                    {
                        contactName = "Ada Lovelace",
                        contactPhone = (string?)null,
                        addressLine1 = "1 High Street",
                        addressLine2 = "London",
                        postcode = "NOT-A-POSTCODE",
                        country = "United Kingdom",
                        deliveryInstructions = (string?)null,
                    },
                }
            );

            Assert.Equal(HttpStatusCode.BadRequest, place.StatusCode);
        }

        [Fact]
        public async Task PlaceOrder_Returns400_WhenTotalsMismatch()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            var place = await PlaceOrderAsync(
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    lines = new[]
                    {
                        new { skuId = "table-tents", quantity = 1 },
                    },
                    deliveryMethod = "standard",
                    expectedGrossPence = 1,
                    shipTo = ValidShipTo(),
                }
            );

            Assert.Equal(HttpStatusCode.BadRequest, place.StatusCode);
        }

        [Fact]
        public async Task PlaceOrder_Returns400_WhenSkuUnknown()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Admin,
                namedInScopeOnly: false
            );

            var place = await PlaceOrderAsync(
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    lines = new[]
                    {
                        new { skuId = "not-a-real-sku", quantity = 1 },
                    },
                    deliveryMethod = "standard",
                    expectedGrossPence = 0,
                    shipTo = ValidShipTo(),
                }
            );

            Assert.Equal(HttpStatusCode.BadRequest, place.StatusCode);
        }

        [Fact]
        public async Task GetDeliveryDefaults_ReturnsLocationPrefill()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.LocationManager,
                namedInScopeOnly: true
            );

            using var request = AuthorizedGet(
                $"/api/shop/locations/{seeded.InScopeLocationId}/delivery-defaults",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(seeded.InScopeLocationId, body.GetProperty("locationId").GetInt32());
            Assert.Equal("In Scope", body.GetProperty("locationName").GetString());
            Assert.Equal("1 High Street", body.GetProperty("addressLine1").GetString());
            Assert.Equal("London", body.GetProperty("addressLine2").GetString());
            Assert.Equal("SE1 1TQ", body.GetProperty("postcode").GetString());
            Assert.Equal("United Kingdom", body.GetProperty("country").GetString());
            Assert.Equal("Site Contact", body.GetProperty("contactName").GetString());
            Assert.Equal("+442074071234", body.GetProperty("contactPhone").GetString());
        }

        private static object ValidShipTo()
        {
            return new
            {
                contactName = "Ada Lovelace",
                contactPhone = "+442074071234",
                addressLine1 = "6 Southwark Street",
                addressLine2 = "London",
                postcode = "SE1 1TQ",
                country = "United Kingdom",
                deliveryInstructions = "Side entrance",
            };
        }

        private async Task<HttpResponseMessage> PlaceOrderAsync(
            string jwt,
            object body
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/shop/orders"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            return await _client.SendAsync(request);
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
                FullName = "Shop Order Owner",
                Email = $"owner-shop-15-{Guid.NewGuid():N}@example.com",
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
                Name = "Shop Order Venue",
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
                City = "London",
                Postcode = "SE1 1TQ",
                LocalContact = "Site Contact",
                LocationPhone = "+442074071234",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                City = "London",
                Postcode = "E1 6AN",
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
                FullName = "Shop Order Member",
                Email = $"member-shop-15-{Guid.NewGuid():N}@example.com",
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
