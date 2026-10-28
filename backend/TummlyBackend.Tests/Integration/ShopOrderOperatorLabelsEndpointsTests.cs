using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Display labels and the order date the Operator Shop screens read
    /// straight from the API (list and detail).
    /// </summary>
    public class ShopOrderOperatorLabelsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string GuestCards = "qr-guest-cards";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopOrderOperatorLabelsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AwaitingPaymentOrder_ShowsAwaitingPaymentOnBothStatuses()
        {
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.AwaitingPayment;
                order.FulfilmentStatus = null;
            });

            // The list only shows paid and refunded orders, so only the detail
            // can carry an unpaid order.
            var detail = await GetDetailAsync(workspace, orderId);

            Assert.Equal(
                "Awaiting payment",
                detail.GetProperty("paymentStatusLabel").GetString()
            );
            Assert.Equal(
                "Awaiting payment",
                detail.GetProperty("fulfilmentStatusLabel").GetString()
            );
            Assert.False(detail.GetProperty("isComplimentary").GetBoolean());
            Assert.Equal(
                JsonValueKind.Null,
                detail.GetProperty("orderKindLabel").ValueKind
            );
        }

        [Fact]
        public async Task PaymentFailedOrder_FulfilmentLabelIsAwaitingPayment()
        {
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.PaymentFailed;
                order.FulfilmentStatus = null;
            });

            var detail = await GetDetailAsync(workspace, orderId);

            Assert.Equal(
                "Payment failed",
                detail.GetProperty("paymentStatusLabel").GetString()
            );
            Assert.Equal(
                "Awaiting payment",
                detail.GetProperty("fulfilmentStatusLabel").GetString()
            );
        }

        [Theory]
        [InlineData(ShopFulfilmentStatuses.Processing, "Confirmed")]
        [InlineData(ShopFulfilmentStatuses.InTransit, "Dispatched")]
        [InlineData(ShopFulfilmentStatuses.Delivered, "Delivered")]
        [InlineData(ShopFulfilmentStatuses.Cancelled, "Cancelled")]
        public async Task PaidOrder_KeepsFulfilmentLabels(
            string fulfilment,
            string expected
        )
        {
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.Paid;
                order.FulfilmentStatus = fulfilment;
                if (fulfilment is ShopFulfilmentStatuses.InTransit or ShopFulfilmentStatuses.Delivered)
                {
                    order.DispatchedAtUtc = DateTime.UtcNow.AddHours(-6);
                }

                if (fulfilment == ShopFulfilmentStatuses.Delivered)
                {
                    order.DeliveredAtUtc = DateTime.UtcNow.AddHours(-1);
                }
            });

            var detail = await GetDetailAsync(workspace, orderId);
            var item = await GetListItemAsync(workspace, orderId);

            Assert.Equal("Paid", detail.GetProperty("paymentStatusLabel").GetString());
            Assert.Equal(
                expected,
                detail.GetProperty("fulfilmentStatusLabel").GetString()
            );
            Assert.Equal("Paid", item.GetProperty("paymentStatusLabel").GetString());
            Assert.Equal(
                expected,
                item.GetProperty("fulfilmentStatusLabel").GetString()
            );
        }

        [Fact]
        public async Task PaidOrderWithPreparation_ReadsPreparingOnDetailAndList()
        {
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.Paid;
                order.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
                order.PreparationStartedAtUtc = DateTime.UtcNow;
            });

            var detail = await GetDetailAsync(workspace, orderId);
            var item = await GetListItemAsync(workspace, orderId);

            Assert.Equal(
                "Preparing",
                detail.GetProperty("fulfilmentStatusLabel").GetString()
            );
            Assert.Equal(
                "Preparing",
                item.GetProperty("fulfilmentStatusLabel").GetString()
            );
        }

        [Fact]
        public async Task PaidOrderInProduction_ReadsInProductionOnDetailAndList()
        {
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.Paid;
                order.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
                order.PreparationStartedAtUtc = DateTime.UtcNow.AddHours(-2);
                order.ProductionStartedAtUtc = DateTime.UtcNow;
            });

            var detail = await GetDetailAsync(workspace, orderId);
            var item = await GetListItemAsync(workspace, orderId);

            Assert.Equal(
                "In production",
                detail.GetProperty("fulfilmentStatusLabel").GetString()
            );
            Assert.Equal(
                "In production",
                item.GetProperty("fulfilmentStatusLabel").GetString()
            );
        }

        [Fact]
        public async Task PaidOrderWithTrackingUrlOnly_DoesNotReadDispatchedOrShowTracking()
        {
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.Paid;
                order.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
                order.PreparationStartedAtUtc = DateTime.UtcNow.AddHours(-2);
                order.TrackingUrl = "https://track.example/label-only";
            });

            var detail = await GetDetailAsync(workspace, orderId);

            Assert.Equal(
                "Preparing",
                detail.GetProperty("fulfilmentStatusLabel").GetString()
            );
            Assert.Null(
                detail.GetProperty("progress").GetProperty("trackingUrl").GetString()
            );
        }

        [Fact]
        public async Task ComplimentaryOrder_IsFreeAndCarriesStarterKitLabel()
        {
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.Paid;
                order.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
                order.IsComplimentary = true;
            });

            var detail = await GetDetailAsync(workspace, orderId);
            var item = await GetListItemAsync(workspace, orderId);

            foreach (var json in new[] { detail, item })
            {
                Assert.Equal("Free", json.GetProperty("paymentStatusLabel").GetString());
                Assert.True(json.GetProperty("isComplimentary").GetBoolean());
                Assert.Equal(
                    "Complimentary Starter Kit",
                    json.GetProperty("orderKindLabel").GetString()
                );
            }
        }

        [Fact]
        public async Task OrderDate_IsLondonTime_At2330UtcInBst()
        {
            // 23:30 UTC on 15 Jul is 00:30 on 16 Jul in London (BST).
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.PaymentStatus = ShopPaymentStatuses.Paid;
                order.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
                order.CreatedAtUtc = new DateTime(
                    2026, 7, 15, 23, 30, 0, DateTimeKind.Utc
                );
            });

            var detail = await GetDetailAsync(workspace, orderId);
            var item = await GetListItemAsync(workspace, orderId);

            Assert.Equal("16 Jul 2026", detail.GetProperty("orderDate").GetString());
            Assert.Equal("16 Jul 2026", item.GetProperty("orderDate").GetString());
        }

        [Fact]
        public async Task OrderDate_IsUnchanged_At2330UtcInGmt()
        {
            // London is on UTC in winter.
            var (workspace, orderId) = await SeedOrderAsync(order =>
            {
                order.CreatedAtUtc = new DateTime(
                    2026, 1, 15, 23, 30, 0, DateTimeKind.Utc
                );
            });

            var detail = await GetDetailAsync(workspace, orderId);

            Assert.Equal("15 Jan 2026", detail.GetProperty("orderDate").GetString());
        }

        private async Task<(ShopCapacityWorkspace, Guid)> SeedOrderAsync(
            Action<ShopOrder> configure
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var workspace = await ShopCapacityTestSeed.SeedWorkspaceAsync(
                context,
                scope.ServiceProvider.GetRequiredService<IJwtService>()
            );
            var orderId = await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                context,
                workspace,
                GuestCards
            );
            var order = await context.ShopOrders.SingleAsync(row => row.Id == orderId);
            configure(order);
            await context.SaveChangesAsync();
            return (workspace, orderId);
        }

        private async Task<JsonElement> GetDetailAsync(
            ShopCapacityWorkspace workspace,
            Guid orderId
        )
        {
            using var request = Authorized(
                workspace,
                $"/api/shop/orders/{orderId}?locationId={workspace.LocationId}"
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private async Task<JsonElement> GetListItemAsync(
            ShopCapacityWorkspace workspace,
            Guid orderId
        )
        {
            using var request = Authorized(
                workspace,
                $"/api/shop/orders?locationId={workspace.LocationId}&pageSize=100"
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("items")
                .EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == orderId)
                .Clone();
        }

        private static HttpRequestMessage Authorized(
            ShopCapacityWorkspace workspace,
            string url
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", workspace.Jwt);
            return request;
        }
    }
}
