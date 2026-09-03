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
    public class AdminShopOrderFulfilmentEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminShopOrderFulfilmentEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListShopOrders_DefaultsToProcessingFilter()
        {
            var seeded = await SeedWorkspaceAsync();
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/1"
            );

            using var request = AuthorizedGet(
                "/api/admin/shop-orders",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());
            Assert.Equal(
                ShopFulfilmentStatuses.Processing,
                body.GetProperty("items")[0].GetProperty("fulfilmentStatus").GetString()
            );
            Assert.True(body.GetProperty("items")[0].TryGetProperty("opsNotes", out _));
            Assert.True(
                body.GetProperty("items")[0].TryGetProperty("revolutOrderId", out _)
            );
        }

        [Fact]
        public async Task ListShopOrders_Returns403_ForOperatorJwt()
        {
            var seeded = await SeedWorkspaceAsync();
            using var request = AuthorizedGet(
                "/api/admin/shop-orders",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PatchFulfilment_ProcessingToInTransitToDelivered_SetsTimestamps()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );

            using var dispatchRequest = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new
                {
                    fulfilmentStatus = ShopFulfilmentStatuses.InTransit,
                    trackingUrl = "https://track.example/abc",
                    opsNotes = "handed to carrier",
                }
            );
            var dispatchResponse = await _client.SendAsync(dispatchRequest);
            Assert.Equal(HttpStatusCode.OK, dispatchResponse.StatusCode);

            var afterDispatch = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.InTransit, afterDispatch.FulfilmentStatus);
            Assert.NotNull(afterDispatch.DispatchedAtUtc);
            Assert.Null(afterDispatch.DeliveredAtUtc);
            Assert.Equal("https://track.example/abc", afterDispatch.TrackingUrl);
            Assert.Equal("handed to carrier", afterDispatch.OpsNotes);

            using var deliverRequest = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.Delivered }
            );
            var deliverResponse = await _client.SendAsync(deliverRequest);
            Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);

            var afterDeliver = await ReadOrderAsync(orderId);
            Assert.Equal(ShopFulfilmentStatuses.Delivered, afterDeliver.FulfilmentStatus);
            Assert.NotNull(afterDeliver.DeliveredAtUtc);
            Assert.Equal("https://track.example/abc", afterDeliver.TrackingUrl);
        }

        [Fact]
        public async Task PatchFulfilment_IllegalBackward_Returns409()
        {
            var seeded = await SeedWorkspaceAsync();
            var deliveredId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Delivered,
                deliveredAtUtc: DateTime.UtcNow.AddDays(-1)
            );
            var inTransitId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/x"
            );

            using var backFromDelivered = AuthorizedPatch(
                $"/api/admin/shop-orders/{deliveredId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.Processing }
            );
            var deliveredResponse = await _client.SendAsync(backFromDelivered);
            Assert.Equal(HttpStatusCode.Conflict, deliveredResponse.StatusCode);
            var deliveredBody = await ReadJsonAsync(deliveredResponse);
            Assert.False(deliveredBody.GetProperty("success").GetBoolean());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    deliveredBody.GetProperty("code").GetString()
                )
            );

            using var backFromTransit = AuthorizedPatch(
                $"/api/admin/shop-orders/{inTransitId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.Processing }
            );
            var transitResponse = await _client.SendAsync(backFromTransit);
            Assert.Equal(HttpStatusCode.Conflict, transitResponse.StatusCode);
        }

        [Fact]
        public async Task PatchFulfilment_RejectsNonHttpsTrackingUrl()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing
            );

            using var ftpRequest = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new
                {
                    fulfilmentStatus = ShopFulfilmentStatuses.InTransit,
                    trackingUrl = "ftp://files.example/track",
                }
            );
            var ftpResponse = await _client.SendAsync(ftpRequest);
            Assert.Equal(HttpStatusCode.BadRequest, ftpResponse.StatusCode);

            using var httpRequest = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new
                {
                    fulfilmentStatus = ShopFulfilmentStatuses.InTransit,
                    trackingUrl = "http://track.example/insecure",
                }
            );
            var httpResponse = await _client.SendAsync(httpRequest);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

            using var badRequest = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new
                {
                    fulfilmentStatus = ShopFulfilmentStatuses.InTransit,
                    trackingUrl = "not-a-url",
                }
            );
            var badResponse = await _client.SendAsync(badRequest);
            Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
        }

        [Fact]
        public async Task PatchFulfilment_DeliveredWithReceiptStickers_MintsActiveQrIdempotently()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/stickers",
                catalogSkuId: "receipt-stickers",
                titleSnapshot: "Receipt stickers"
            );

            using var firstDeliver = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.Delivered }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(firstDeliver)).StatusCode
            );

            var firstCount = await CountActiveReceiptStickersAsync(seeded.LocationId);
            Assert.Equal(1, firstCount);

            // Already delivered: opsNotes-only update must not mint again.
            using var notesOnly = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new { opsNotes = "confirmed at door" }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(notesOnly)).StatusCode
            );
            Assert.Equal(
                1,
                await CountActiveReceiptStickersAsync(seeded.LocationId)
            );

            // Second order at same location with receipt-stickers: skip mint.
            var secondOrderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/stickers-2",
                catalogSkuId: "receipt-stickers",
                titleSnapshot: "Receipt stickers"
            );
            using var secondDeliver = AuthorizedPatch(
                $"/api/admin/shop-orders/{secondOrderId}/fulfilment",
                seeded.AdminJwt,
                new { fulfilmentStatus = ShopFulfilmentStatuses.Delivered }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(secondDeliver)).StatusCode
            );
            Assert.Equal(
                1,
                await CountActiveReceiptStickersAsync(seeded.LocationId)
            );
        }

        [Fact]
        public async Task ExportCsv_UsesPersistedShipToAndLines_DefaultProcessing()
        {
            var seeded = await SeedWorkspaceAsync();
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.Processing,
                shipToContactName: "Ada Export",
                shipToAddressLine2: "Unit 4",
                deliveryInstructions: "Leave at desk"
            );
            await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/skip"
            );

            using var request = AuthorizedGet(
                "/api/admin/shop-orders/export.csv",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType
            );

            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("orderNumber", csv);
            Assert.Contains("locationNameSnapshot", csv);
            Assert.Contains("shipToContactName", csv);
            Assert.Contains("Ada Export", csv);
            Assert.Contains("Unit 4", csv);
            Assert.Contains("Leave at desk", csv);
            Assert.Contains("table-tents", csv);
            Assert.Contains("processing", csv);
            Assert.DoesNotContain("https://track.example/skip", csv);
        }

        [Fact]
        public async Task OperatorDetail_StillOmitsOpsNotes_AfterAdminWritesNotes()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/ops"
            );

            using var patch = AuthorizedPatch(
                $"/api/admin/shop-orders/{orderId}/fulfilment",
                seeded.AdminJwt,
                new { opsNotes = "warehouse aisle B" }
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(patch)).StatusCode);

            using var detail = AuthorizedGet(
                $"/api/shop/orders/{orderId}?locationId={seeded.LocationId}",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(detail);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.TryGetProperty("opsNotes", out _));
        }

        private async Task<int> CountActiveReceiptStickersAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            return await context.QrCodes.CountAsync(row =>
                row.RestaurantLocationId == locationId
                && row.QrType == QrType.ReceiptSticker
                && row.Status == QrCodeStatus.Active
            );
        }

        private async Task<ShopOrder> ReadOrderAsync(Guid orderId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            return await context.ShopOrders.AsNoTracking().SingleAsync(row =>
                row.Id == orderId
            );
        }

        private async Task<Guid> InsertOrderAsync(
            Seeded seeded,
            string? fulfilmentStatus,
            string? trackingUrl = null,
            string? opsNotes = null,
            DateTime? deliveredAtUtc = null,
            string catalogSkuId = "table-tents",
            string titleSnapshot = "Table tents",
            string shipToContactName = "Ada Lovelace",
            string? shipToAddressLine2 = null,
            string? deliveryInstructions = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var sequence = await context.ShopOrderSequences
                .SingleOrDefaultAsync(row => row.RestaurantId == seeded.RestaurantId);
            var nextNumber = sequence?.NextNumber ?? 1;
            if (sequence == null)
            {
                context.ShopOrderSequences.Add(
                    new ShopOrderSequence
                    {
                        RestaurantId = seeded.RestaurantId,
                        NextNumber = nextNumber + 1,
                    }
                );
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
                LocationId = seeded.LocationId,
                LocationNameSnapshot = seeded.LocationName,
                PlacedByUserId = seeded.OwnerUserId,
                PlacedByNameSnapshot = "Shop Owner",
                MaterialsNetPence = 2400,
                VatPence = 480,
                DeliveryNetPence = 0,
                GrossPence = 2880,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = ShopPaymentStatuses.Paid,
                FulfilmentStatus = fulfilmentStatus,
                PaidAtUtc = DateTime.UtcNow.AddDays(-2),
                ProcessingStartedAtUtc =
                    fulfilmentStatus == ShopFulfilmentStatuses.Processing
                    || fulfilmentStatus == ShopFulfilmentStatuses.InTransit
                    || fulfilmentStatus == ShopFulfilmentStatuses.Delivered
                        ? DateTime.UtcNow.AddDays(-1)
                        : null,
                DispatchedAtUtc =
                    fulfilmentStatus == ShopFulfilmentStatuses.InTransit
                    || fulfilmentStatus == ShopFulfilmentStatuses.Delivered
                        ? DateTime.UtcNow.AddHours(-6)
                        : null,
                DeliveredAtUtc = deliveredAtUtc,
                TrackingUrl = trackingUrl,
                OpsNotes = opsNotes,
                ShipToContactName = shipToContactName,
                ShipToContactPhone = "07700900000",
                ShipToAddressLine1 = "1 High Street",
                ShipToAddressLine2 = shipToAddressLine2,
                ShipToPostcode = "SE1 1TQ",
                ShipToCountry = "United Kingdom",
                DeliveryInstructions = deliveryInstructions,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow,
                Lines =
                {
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        CatalogSkuId = catalogSkuId,
                        TitleSnapshot = titleSnapshot,
                        MaterialType = titleSnapshot,
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

        private async Task<Seeded> SeedWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Admin Shop Owner",
                Email = $"owner-admin-shop-20-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Single",
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
                Name = "Admin Shop Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main Hall",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
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
                Email = $"admin-shop-20-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(tummlyAdmin);
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                location.Id,
                location.LocationName,
                owner.Id,
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

        private static HttpRequestMessage AuthorizedPatch(
            string url,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
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
            int LocationId,
            string LocationName,
            int OwnerUserId,
            string OwnerJwt,
            string AdminJwt
        );
    }
}
