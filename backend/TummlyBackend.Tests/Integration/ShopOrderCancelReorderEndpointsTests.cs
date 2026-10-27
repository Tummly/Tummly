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
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class ShopOrderCancelReorderEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopOrderCancelReorderEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CancelProcessingOrder_LeavesPaymentPaid()
        {
            var seeded = await SeedWorkspaceAsync();
            var refundCallsBefore = _factory.Merchant.RefundOrderCallCount;
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing,
                revolutOrderId: "rev_cancel_processing_1"
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    reason = ShopCancelReasons.OrderedByMistake,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("paid", body.GetProperty("paymentStatus").GetString());
            Assert.Equal(
                "cancelled",
                body.GetProperty("fulfilmentStatus").GetString()
            );
            Assert.False(body.GetProperty("canCancel").GetBoolean());
            Assert.Equal(
                refundCallsBefore + 1,
                _factory.Merchant.RefundOrderCallCount
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var order = await context.ShopOrders
                    .AsNoTracking()
                    .SingleAsync(row => row.Id == orderId);
                Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
                Assert.Equal(ShopFulfilmentStatuses.Cancelled, order.FulfilmentStatus);
                Assert.Equal(
                    ShopCancelReasons.OrderedByMistake,
                    order.CancelReason
                );
                Assert.NotNull(order.CancelledAtUtc);
                Assert.Equal(seeded.MemberUserId, order.CancelledByUserId);
            }
        }

        [Fact]
        public async Task CancelPaidOrderWithRevolutId_CallsMerchantRefund()
        {
            var seeded = await SeedWorkspaceAsync();
            var refundCallsBefore = _factory.Merchant.RefundOrderCallCount;
            var revolutOrderId = $"ord_shop_cancel_{Guid.NewGuid():N}";
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing,
                revolutOrderId: revolutOrderId
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    reason = ShopCancelReasons.OrderedByMistake,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("paid", body.GetProperty("paymentStatus").GetString());
            Assert.Equal(
                "cancelled",
                body.GetProperty("fulfilmentStatus").GetString()
            );
            Assert.Equal(
                refundCallsBefore + 1,
                _factory.Merchant.RefundOrderCallCount
            );
            Assert.Equal(revolutOrderId, _factory.Merchant.LastRefundOrderId);
            Assert.Null(_factory.Merchant.LastRefundAmountMinor);
            Assert.Equal(
                $"shop-cancel:{orderId:D}",
                _factory.Merchant.LastRefundIdempotencyKey
            );
        }

        [Fact]
        public async Task CancelInTransitOrder_Returns409()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/1"
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    reason = ShopCancelReasons.NoLongerRequired,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "shop_order_not_cancellable",
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task CancelDeliveredOrder_Returns409()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    reason = ShopCancelReasons.NoLongerRequired,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "shop_order_not_cancellable",
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task CancelAlreadyCancelled_IsIdempotent200EvenWithInvalidReason()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Cancelled
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    reason = "not-a-valid-slug",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "cancelled",
                body.GetProperty("fulfilmentStatus").GetString()
            );
        }

        [Fact]
        public async Task CancelAlreadyCancelled_IsIdempotent200()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Cancelled
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    reason = ShopCancelReasons.Other,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "cancelled",
                body.GetProperty("fulfilmentStatus").GetString()
            );
        }

        [Fact]
        public async Task GetOrderDetail_ExposesCanCancelFlags()
        {
            var seeded = await SeedWorkspaceAsync();
            var processingId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing
            );
            var dispatchedId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.InTransit,
                trackingUrl: "https://track.example/2"
            );

            using var processingRequest = AuthorizedGet(
                $"/api/shop/orders/{processingId}?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var processingBody = await ReadJsonAsync(
                await _client.SendAsync(processingRequest)
            );
            Assert.True(processingBody.GetProperty("canCancel").GetBoolean());
            if (processingBody.TryGetProperty("cancelBlockReason", out var processingBlock))
            {
                Assert.Equal(JsonValueKind.Null, processingBlock.ValueKind);
            }

            using var dispatchedRequest = AuthorizedGet(
                $"/api/shop/orders/{dispatchedId}?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt
            );
            var dispatchedBody = await ReadJsonAsync(
                await _client.SendAsync(dispatchedRequest)
            );
            Assert.False(dispatchedBody.GetProperty("canCancel").GetBoolean());
            Assert.Equal(
                "in_transit",
                dispatchedBody.GetProperty("cancelBlockReason").GetString()
            );
        }

        [Fact]
        public async Task ReorderPrefill_RePricesFromCurrentCatalog()
        {
            var seeded = await SeedWorkspaceAsync();
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "table-tents", true);
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered,
                unitNetPence: 1200
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                seeded.InScopeLocationId,
                body.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("ORD-1", body.GetProperty("sourceOrderNumber").GetString());
            var prefillLine = body.GetProperty("lines")[0];
            Assert.Equal("table-tents", prefillLine.GetProperty("skuId").GetString());
            Assert.Equal(5, prefillLine.GetProperty("quantity").GetInt32());
            Assert.Equal(999, prefillLine.GetProperty("unitNetPence").GetInt32());
            Assert.Equal(999, prefillLine.GetProperty("lineNetPence").GetInt32());
            Assert.True(body.TryGetProperty("shipTo", out _));
            Assert.Equal(
                "standard",
                body.GetProperty("deliveryMethod").GetString()
            );
        }

        [Fact]
        public async Task ReorderPrefill_OfOldExpressOrder_ReturnsStandard()
        {
            var seeded = await SeedWorkspaceAsync();
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "table-tents", true);
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered,
                deliveryMethod: ShopDeliveryMethods.Express
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "standard",
                body.GetProperty("deliveryMethod").GetString()
            );
        }

        [Fact]
        public async Task ReorderPrefill_InvalidPackQty_Returns409()
        {
            var seeded = await SeedWorkspaceAsync();
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "table-tents", true);
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var line = await context.ShopOrderLines
                    .SingleAsync(row => row.ShopOrderId == orderId);
                line.Quantity = 2;
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "reorder_no_available_lines",
                body.GetProperty("code").GetString()
            );
            var unavailable = Assert.Single(body.GetProperty("unavailableLines").EnumerateArray());
            Assert.Equal("table-tents", unavailable.GetProperty("skuId").GetString());
            Assert.Equal("Table tents", unavailable.GetProperty("title").GetString());
        }

        [Fact]
        public async Task Reorder_DropsUnavailableLines_ReturnsThemInUnavailableLines()
        {
            var seeded = await SeedWorkspaceAsync();
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "table-tents", true);
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", false);
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered
            );
            await AddOrderLineAsync(orderId, "qr-guest-cards", "QR Guest Cards");

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                new[] { "table-tents" },
                body.GetProperty("lines")
                    .EnumerateArray()
                    .Select(line => line.GetProperty("skuId").GetString())
                    .ToArray()
            );
            var unavailable = Assert.Single(body.GetProperty("unavailableLines").EnumerateArray());
            Assert.Equal("qr-guest-cards", unavailable.GetProperty("skuId").GetString());
            Assert.Equal("QR Guest Cards", unavailable.GetProperty("title").GetString());
        }

        [Fact]
        public async Task Reorder_AllLinesUnavailable_Returns409ReorderNoAvailableLines()
        {
            var seeded = await SeedWorkspaceAsync();
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "table-tents", false);
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "reorder_no_available_lines",
                body.GetProperty("code").GetString()
            );
            var unavailable = Assert.Single(body.GetProperty("unavailableLines").EnumerateArray());
            Assert.Equal("table-tents", unavailable.GetProperty("skuId").GetString());
            Assert.Equal("Table tents", unavailable.GetProperty("title").GetString());
        }

        [Fact]
        public async Task ReorderPrefill_DelistedSku_Returns409()
        {
            var seeded = await SeedWorkspaceAsync();
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered,
                catalogSkuId: "delisted-sku-test"
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "reorder_no_available_lines",
                body.GetProperty("code").GetString()
            );
            var unavailable = Assert.Single(body.GetProperty("unavailableLines").EnumerateArray());
            Assert.Equal("delisted-sku-test", unavailable.GetProperty("skuId").GetString());
            Assert.Equal("Table tents", unavailable.GetProperty("title").GetString());
        }

        [Fact]
        public async Task Reorder_ComingSoonAndDelistedLines_Returns409ListingBoth()
        {
            var seeded = await SeedWorkspaceAsync();
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "qr-guest-cards", false);
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered,
                catalogSkuId: "delisted-sku-test"
            );
            await AddOrderLineAsync(orderId, "qr-guest-cards", "QR Guest Cards");

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "reorder_no_available_lines",
                body.GetProperty("code").GetString()
            );
            var unavailable = body.GetProperty("unavailableLines")
                .EnumerateArray()
                .Select(item => (
                    SkuId: item.GetProperty("skuId").GetString(),
                    Title: item.GetProperty("title").GetString()
                ))
                .OrderBy(item => item.SkuId, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(
                new[]
                {
                    (SkuId: "delisted-sku-test", Title: "Table tents"),
                    (SkuId: "qr-guest-cards", Title: "QR Guest Cards"),
                },
                unavailable
            );
        }

        [Fact]
        public async Task SoftLock_CancelProcessingOrder_IsNotBillingLock403()
        {
            var seeded = await SeedWorkspaceAsync();
            await SetBillingStatusAsync(
                seeded.RestaurantId,
                BillingStatuses.SoftLock
            );
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Processing,
                revolutOrderId: "rev_cancel_soft_lock_1"
            );

            using var request = AuthorizedPost(
                $"/api/shop/orders/{orderId}/cancel",
                seeded.MemberJwt,
                new
                {
                    locationId = seeded.InScopeLocationId,
                    reason = ShopCancelReasons.IncorrectQuantity,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "cancelled",
                body.GetProperty("fulfilmentStatus").GetString()
            );
        }

        [Fact]
        public async Task SoftLock_ReorderPrefill200_SubsequentPlace403()
        {
            var seeded = await SeedWorkspaceAsync();
            await ShopTestSeed.SetSkuAvailableInShopAsync(_factory, "table-tents", true);
            await SetBillingStatusAsync(
                seeded.RestaurantId,
                BillingStatuses.SoftLock
            );
            var orderId = await InsertOrderAsync(
                seeded,
                seeded.InScopeLocationId,
                ShopPaymentStatuses.Paid,
                ShopFulfilmentStatuses.Delivered
            );

            using var reorderRequest = AuthorizedPost(
                $"/api/shop/orders/{orderId}/reorder?locationId={seeded.InScopeLocationId}",
                seeded.MemberJwt,
                body: null
            );
            var reorderResponse = await _client.SendAsync(reorderRequest);
            Assert.Equal(HttpStatusCode.OK, reorderResponse.StatusCode);

            using var placeRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/shop/orders"
            );
            placeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            placeRequest.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    locationId = seeded.InScopeLocationId,
                    lines = new[] { new { skuId = "table-tents", quantity = 5 } },
                    deliveryMethod = "standard",
                    expectedGrossPence = 1598,
                    shipTo = ValidShipTo(),
                }),
                Encoding.UTF8,
                "application/json"
            );
            var placeResponse = await _client.SendAsync(placeRequest);
            Assert.Equal(HttpStatusCode.Forbidden, placeResponse.StatusCode);
            var placeBody = await ReadJsonAsync(placeResponse);
            Assert.Equal("soft_lock", placeBody.GetProperty("code").GetString());
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

        private async Task SetBillingStatusAsync(
            int restaurantId,
            string billingStatus
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var account = await context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.BillingStatus = billingStatus;
            await context.SaveChangesAsync();
        }

        private async Task AddOrderLineAsync(Guid orderId, string skuId, string title)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            context.ShopOrderLines.Add(new ShopOrderLine
            {
                Id = Guid.NewGuid(),
                ShopOrderId = orderId,
                CatalogSkuId = skuId,
                TitleSnapshot = title,
                MaterialType = title,
                Quantity = 5,
                UnitNetPence = 500,
                LineNetPence = 2500,
            });
            await context.SaveChangesAsync();
        }

        private async Task<Guid> InsertOrderAsync(
            PermissionSeed seeded,
            int locationId,
            string paymentStatus,
            string? fulfilmentStatus,
            string? trackingUrl = null,
            int unitNetPence = 1200,
            string catalogSkuId = "table-tents",
            string? revolutOrderId = null,
            string deliveryMethod = ShopDeliveryMethods.Standard
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

            // Default 5 matches launch Table Tent pack tiers (5 / 10 / 20).
            var quantity = 5;
            var materialsNet = unitNetPence * quantity;
            var vatPence = materialsNet / 5;
            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{nextNumber}",
                RestaurantId = seeded.RestaurantId,
                LocationId = locationId,
                LocationNameSnapshot = locationName,
                PlacedByUserId = seeded.MemberUserId,
                PlacedByNameSnapshot = "Shop Member",
                MaterialsNetPence = materialsNet,
                VatPence = vatPence,
                DeliveryNetPence = 0,
                GrossPence = materialsNet + vatPence,
                DeliveryMethod = deliveryMethod,
                PaymentStatus = paymentStatus,
                RevolutOrderId = revolutOrderId,
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
                DeliveredAtUtc =
                    fulfilmentStatus == ShopFulfilmentStatuses.Delivered
                        ? DateTime.UtcNow.AddDays(-1)
                        : null,
                TrackingUrl = trackingUrl,
                ShipToContactName = "Ada Lovelace",
                ShipToAddressLine1 = "1 High Street",
                ShipToPostcode = "SE1 1TQ",
                ShipToCountry = "United Kingdom",
                DeliveryInstructions = "Leave at reception",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow,
                Lines =
                {
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        CatalogSkuId = catalogSkuId,
                        TitleSnapshot = "Table tents",
                        MaterialType = "Table tents",
                        Quantity = quantity,
                        UnitNetPence = unitNetPence,
                        LineNetPence = materialsNet,
                    },
                },
            };

            context.ShopOrders.Add(order);
            await context.SaveChangesAsync();
            return order.Id;
        }

        private async Task<PermissionSeed> SeedWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Shop Cancel Owner",
                Email = $"owner-shop-18-{Guid.NewGuid():N}@example.com",
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
                Name = "Shop Cancel Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(new BillingAccount
            {
                RestaurantId = restaurant.Id,
                RevolutCustomerId = "cust_shop_18",
                SubscriptionPlan = BillingSubscriptionPlans.Starter,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = "default",
            });

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Cancel Test Location",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var member = new User
            {
                FullName = "Shop Cancel Member",
                Email = $"member-shop-18-{Guid.NewGuid():N}@example.com",
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
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
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
                location.Id
            );
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPost(
            string url,
            string jwt,
            object? body
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                );
            }

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
            int InScopeLocationId
        );
    }
}
