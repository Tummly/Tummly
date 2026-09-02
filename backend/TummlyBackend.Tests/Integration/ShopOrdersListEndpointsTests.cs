using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class ShopOrdersListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopOrdersListEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListOrders_HidesAwaitingPayment()
        {
            var seeded = await SeedWorkspaceAsync(namedInScopeOnly: false);
            var paidId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing
            );
            await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.AwaitingPayment,
                null
            );

            using var request = AuthorizedGet(
                $"/api/shop/orders?locationId={seeded.InScopeLocationId}&locationScope=all",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(paidId.ToString("D"), body.GetProperty("items")[0].GetProperty("id").GetString());
        }

        [Fact]
        public async Task ListOrders_AppliesFulfilmentFilterAndAggregates()
        {
            var seeded = await SeedWorkspaceAsync(namedInScopeOnly: false);
            await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing
            );
            await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/1"
            );
            await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered,
                deliveredAtUtc: DateTime.UtcNow.AddDays(-10)
            );

            using var request = AuthorizedGet(
                $"/api/shop/orders?locationId={seeded.InScopeLocationId}&locationScope=all&fulfilmentStatus=processing",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Processing",
                body.GetProperty("items")[0].GetProperty("fulfilmentStatus").GetString()
            );

            using var allRequest = AuthorizedGet(
                $"/api/shop/orders?locationId={seeded.InScopeLocationId}&locationScope=all",
                seeded.MemberJwt
            );
            var allBody = await ReadJsonAsync(await _client.SendAsync(allRequest));
            var aggregates = allBody.GetProperty("aggregates");
            Assert.Equal(1, aggregates.GetProperty("inProgress").GetInt32());
            Assert.Equal(1, aggregates.GetProperty("dispatched").GetInt32());
            Assert.Equal(1, aggregates.GetProperty("deliveredLast90Days").GetInt32());
        }

        [Fact]
        public async Task ListOrders_ExcludesOutOfScopeLocations()
        {
            var seeded = await SeedWorkspaceAsync(namedInScopeOnly: true);
            await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing
            );
            await InsertOrderAsync(
                seeded,
                seeded.OutOfScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing
            );

            using var request = AuthorizedGet(
                $"/api/shop/orders?locationId={seeded.InScopeLocationId}&locationScope=all",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                seeded.InScopeLocationId,
                body.GetProperty("items")[0].GetProperty("locationId").GetInt32()
            );
        }

        [Fact]
        public async Task GetOrder_Returns403_ForOutOfScopeLocation()
        {
            var seeded = await SeedWorkspaceAsync(namedInScopeOnly: true);
            var outOfScopeOrderId = await InsertOrderAsync(
                seeded,
                seeded.OutOfScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing
            );

            using var request = AuthorizedGet(
                $"/api/shop/orders/{outOfScopeOrderId}?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOrder_ReturnsOperatorDetailShapeWithoutOpsNotes()
        {
            var seeded = await SeedWorkspaceAsync(namedInScopeOnly: false);
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/dispatch",
                opsNotes: "internal only"
            );

            using var request = AuthorizedGet(
                $"/api/shop/orders/{orderId}?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("ORD-1", body.GetProperty("orderNumber").GetString());
            Assert.Equal("paid", body.GetProperty("paymentStatus").GetString());
            Assert.Equal("Paid", body.GetProperty("paymentStatusLabel").GetString());
            Assert.Equal(
                "in_transit",
                body.GetProperty("fulfilmentStatus").GetString()
            );
            Assert.Equal(
                "Dispatched",
                body.GetProperty("fulfilmentStatusLabel").GetString()
            );
            Assert.True(body.GetProperty("lines").GetArrayLength() > 0);
            Assert.True(body.TryGetProperty("shipTo", out _));
            Assert.True(body.TryGetProperty("paymentSummary", out _));
            Assert.Equal(
                "https://track.example/dispatch",
                body.GetProperty("progress").GetProperty("trackingUrl").GetString()
            );
            Assert.False(body.TryGetProperty("opsNotes", out _));
        }

        private async Task<Guid> InsertOrderAsync(
            PermissionSeed seeded,
            int locationId,
            string paymentStatus,
            string? fulfilmentStatus,
            string? trackingUrl = null,
            string? opsNotes = null,
            DateTime? deliveredAtUtc = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var locationName = await context.RestaurantLocations
                .Where(row => row.Id == locationId)
                .Select(row => row.LocationName)
                .SingleAsync();

            var sequence = await context.ShopOrderSequences
                .SingleOrDefaultAsync(row => row.RestaurantId == seeded.RestaurantId);
            var nextNumber = sequence?.NextNumber ?? 1;
            if (sequence == null)
            {
                context.ShopOrderSequences.Add(new ShopOrderSequence
                {
                    RestaurantId = seeded.RestaurantId,
                    NextNumber = nextNumber + 1,
                });
            }
            else
            {
                sequence.NextNumber = nextNumber + 1;
            }

            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{nextNumber}",
                RestaurantId = seeded.RestaurantId,
                LocationId = locationId,
                LocationNameSnapshot = locationName,
                PlacedByUserId = seeded.MemberUserId,
                PlacedByNameSnapshot = "Shop Member",
                MaterialsNetPence = 2400,
                VatPence = 480,
                DeliveryNetPence = 0,
                GrossPence = 2880,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = paymentStatus,
                FulfilmentStatus = fulfilmentStatus,
                PaidAtUtc = paymentStatus == ShopPaymentStatuses.Paid
                    ? DateTime.UtcNow.AddDays(-2)
                    : null,
                ProcessingStartedAtUtc =
                    fulfilmentStatus == ShopFulfilmentStatuses.Processing
                        ? DateTime.UtcNow.AddDays(-1)
                        : null,
                DispatchedAtUtc =
                    fulfilmentStatus == ShopFulfilmentStatuses.InTransit
                        ? DateTime.UtcNow.AddHours(-6)
                        : null,
                DeliveredAtUtc = deliveredAtUtc,
                TrackingUrl = trackingUrl,
                OpsNotes = opsNotes,
                ShipToContactName = "Ada Lovelace",
                ShipToAddressLine1 = "1 High Street",
                ShipToPostcode = "SE1 1TQ",
                ShipToCountry = "United Kingdom",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow,
                Lines =
                {
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        CatalogSkuId = "table-tents",
                        TitleSnapshot = "Table tents",
                        MaterialType = "Table tents",
                        Quantity = 2,
                        UnitNetPence = 1200,
                        LineNetPence = 2400,
                    },
                },
            };

            context.ShopOrders.Add(order);
            await context.SaveChangesAsync();
            return order.Id;
        }

        private async Task<PermissionSeed> SeedWorkspaceAsync(bool namedInScopeOnly)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Shop Orders Owner",
                Email = $"owner-shop-17-{Guid.NewGuid():N}@example.com",
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
                Name = "Shop Orders Venue",
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
                FullName = "Shop Orders Member",
                Email = $"member-shop-17-{Guid.NewGuid():N}@example.com",
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
                PermissionRole = PermissionRoles.LocationManager,
                LocationScope = namedInScopeOnly
                    ? LocationScopeKind.NamedList
                    : LocationScopeKind.AllLocations,
                NamedLocationIdsJson = namedInScopeOnly
                    ? MembershipLocationScope.SerializeNamedIds([inScope.Id])
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
                member.Id,
                restaurant.Id,
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
            int MemberUserId,
            int RestaurantId,
            int InScopeLocationId,
            int OutOfScopeLocationId
        );
    }
}
