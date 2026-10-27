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
    /// <c>POST /api/shop/orders/{id}/confirm-payment</c>: the return page asks
    /// the server to read Revolut instead of waiting for the webhook.
    /// </summary>
    public class ShopOrderConfirmPaymentEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string GuestCards = "qr-guest-cards";
        private const string TableTalker = "table-talker";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopOrderConfirmPaymentEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Confirm_RevolutCompleted_MarksPaid_AndConvertsReservation()
        {
            var held = await PayGuestCardOrderAsync();
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(
                held.RevolutOrderId,
                "completed"
            );
            var refundsBefore = _factory.Merchant.RefundOrderCallCount;

            var response = await _client.SendAsync(
                ConfirmRequest(held.Workspace, held.OrderId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("paid", body.GetProperty("paymentStatus").GetString());
            Assert.Equal(
                held.OrderId,
                body.GetProperty("id").GetGuid()
            );
            Assert.Equal(refundsBefore, _factory.Merchant.RefundOrderCallCount);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await db.ShopOrders.AsNoTracking()
                .SingleAsync(row => row.Id == held.OrderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.NotNull(order.PaidAtUtc);
            var reservation = await db.ShopQrSlotReservations.AsNoTracking()
                .SingleAsync(row => row.ShopOrderId == held.OrderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.False((await db.RevolutOrderIntents
                .SingleAsync(row => row.ShopOrderId == held.OrderId)).IsOpen);
        }

        [Fact]
        public async Task Confirm_RevolutPending_ChangesNothing_AndNeverReleasesTheReservation()
        {
            var held = await PayGuestCardOrderAsync();
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(
                held.RevolutOrderId,
                "pending"
            );
            var cancelsBefore = _factory.Merchant.CancelOrderCallCount;
            var refundsBefore = _factory.Merchant.RefundOrderCallCount;

            var response = await _client.SendAsync(
                ConfirmRequest(held.Workspace, held.OrderId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "awaiting_payment",
                body.GetProperty("paymentStatus").GetString()
            );
            Assert.Equal(cancelsBefore, _factory.Merchant.CancelOrderCallCount);
            Assert.Equal(refundsBefore, _factory.Merchant.RefundOrderCallCount);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reservation = await db.ShopQrSlotReservations.AsNoTracking()
                .SingleAsync(row => row.ShopOrderId == held.OrderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
            Assert.True((await db.RevolutOrderIntents
                .SingleAsync(row => row.ShopOrderId == held.OrderId)).IsOpen);
        }

        [Fact]
        public async Task Confirm_RevolutCancelled_DoesNotMarkFailedOrRelease()
        {
            var held = await PayGuestCardOrderAsync();
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(
                held.RevolutOrderId,
                "cancelled"
            );

            var response = await _client.SendAsync(
                ConfirmRequest(held.Workspace, held.OrderId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await db.ShopOrders.AsNoTracking()
                .SingleAsync(row => row.Id == held.OrderId);
            Assert.Equal(ShopPaymentStatuses.AwaitingPayment, order.PaymentStatus);
            var reservation = await db.ShopQrSlotReservations.AsNoTracking()
                .SingleAsync(row => row.ShopOrderId == held.OrderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
        }

        [Fact]
        public async Task Confirm_RevolutReadFails_Returns200Unchanged()
        {
            var held = await PayGuestCardOrderAsync();
            _factory.Merchant.Orders[held.RevolutOrderId] =
                new RevolutOrderRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error"
                );

            var response = await _client.SendAsync(
                ConfirmRequest(held.Workspace, held.OrderId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "awaiting_payment",
                body.GetProperty("paymentStatus").GetString()
            );
        }

        [Fact]
        public async Task Confirm_OrderNotAwaitingPayment_ReturnsUnchanged_WithoutReadingRevolut()
        {
            var held = await PayGuestCardOrderAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                (await db.ShopOrders.SingleAsync(row => row.Id == held.OrderId))
                    .PaymentStatus = ShopPaymentStatuses.PaymentFailed;
                await db.SaveChangesAsync();
            }

            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(
                held.RevolutOrderId,
                "completed"
            );
            var readsBefore = _factory.Merchant.GetOrderCallCount;

            var response = await _client.SendAsync(
                ConfirmRequest(held.Workspace, held.OrderId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "payment_failed",
                body.GetProperty("paymentStatus").GetString()
            );
            Assert.Equal(readsBefore, _factory.Merchant.GetOrderCallCount);
        }

        [Fact]
        public async Task Confirm_OrderWithoutRevolutIntent_ReturnsUnchanged()
        {
            var workspace = await SeedAsync();
            Guid orderId;
            using (var scope = _factory.Services.CreateScope())
            {
                orderId = await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                    workspace,
                    GuestCards
                );
            }

            var readsBefore = _factory.Merchant.GetOrderCallCount;
            var response = await _client.SendAsync(
                ConfirmRequest(workspace, orderId)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "awaiting_payment",
                body.GetProperty("paymentStatus").GetString()
            );
            Assert.Equal(readsBefore, _factory.Merchant.GetOrderCallCount);
        }

        [Fact]
        public async Task Confirm_CalledTwice_IsIdempotent()
        {
            var held = await PayGuestCardOrderAsync();
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(
                held.RevolutOrderId,
                "completed"
            );

            var first = await _client.SendAsync(
                ConfirmRequest(held.Workspace, held.OrderId)
            );
            var readsAfterFirst = _factory.Merchant.GetOrderCallCount;
            var second = await _client.SendAsync(
                ConfirmRequest(held.Workspace, held.OrderId)
            );

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var body = await second.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("paid", body.GetProperty("paymentStatus").GetString());
            Assert.Equal(readsAfterFirst, _factory.Merchant.GetOrderCallCount);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await db.TummlyVatInvoices
                .CountAsync(row => row.RevolutOrderId == held.RevolutOrderId));
            Assert.Single(await db.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == held.OrderId)
                .ToListAsync());
        }

        [Fact]
        public async Task Confirm_MissingOrder_Returns404()
        {
            var workspace = await SeedAsync();

            var response = await _client.SendAsync(
                ConfirmRequest(workspace, Guid.NewGuid())
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Confirm_OtherRestaurantsOrder_Returns404()
        {
            var owner = await SeedAsync();
            var other = await SeedAsync();
            Guid orderId;
            using (var scope = _factory.Services.CreateScope())
            {
                orderId = await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                    other,
                    GuestCards
                );
            }

            var response = await _client.SendAsync(
                ConfirmRequest(owner, orderId)
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Confirm_WithoutToken_Returns401()
        {
            var response = await _client.PostAsync(
                $"/api/shop/orders/{Guid.NewGuid()}/confirm-payment?locationId=1",
                content: null
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static HttpRequestMessage ConfirmRequest(
            ShopCapacityWorkspace workspace,
            Guid orderId
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/shop/orders/{orderId}/confirm-payment?locationId={workspace.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", workspace.Jwt);
            return request;
        }

        private static RevolutOrderRetrieveResult Revolut(
            string revolutOrderId,
            string state
        ) => new(
            Succeeded: true,
            Id: revolutOrderId,
            State: state,
            AmountMinor: 1200,
            CheckoutUrl: FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
            PaymentMethodSummary: "Paid"
        );

        private async Task<ShopCapacityWorkspace> SeedAsync(int freeSlots = 1)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var workspace = await ShopCapacityTestSeed.SeedWorkspaceAsync(
                context,
                scope.ServiceProvider.GetRequiredService<IJwtService>()
            );
            await ShopCapacityTestSeed.MakeOrderableAsync(
                context,
                workspace,
                GuestCards,
                TableTalker
            );
            await ShopCapacityTestSeed.LeaveFreeSlotsAsync(
                context,
                workspace,
                freeSlots
            );
            return workspace;
        }

        private async Task<HeldOrder> PayGuestCardOrderAsync()
        {
            var workspace = await SeedAsync();
            Guid orderId;
            using (var scope = _factory.Services.CreateScope())
            {
                orderId = await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                    workspace,
                    GuestCards
                );
            }

            var pay = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );
            Assert.Equal(HttpStatusCode.OK, pay.StatusCode);

            using var verify = _factory.Services.CreateScope();
            var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var revolutOrderId = await db.RevolutOrderIntents
                .Where(row => row.ShopOrderId == orderId)
                .Select(row => row.OrderId)
                .SingleAsync();
            return new HeldOrder(workspace, orderId, revolutOrderId);
        }

        private sealed record HeldOrder(
            ShopCapacityWorkspace Workspace,
            Guid OrderId,
            string RevolutOrderId
        );
    }
}
