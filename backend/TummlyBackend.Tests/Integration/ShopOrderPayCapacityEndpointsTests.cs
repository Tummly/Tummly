using System.Net;
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
    /// QR capacity at <c>POST /api/shop/orders/{id}/pay</c> (spec 6.3).
    /// </summary>
    public class ShopOrderPayCapacityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string GuestCards = "qr-guest-cards";
        private const string TableTalker = "table-talker";
        private const string TableTents = "table-tents";
        private const string WindowStickers = "window-stickers";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopOrderPayCapacityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Pay_AtLimit_NeedsNewQr_Returns409_AndNoRevolutOrderCreated()
        {
            var workspace = await SeedAsync(freeSlots: 0, GuestCards);
            var cap = await CapAsync(workspace);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var createsBefore = _factory.Merchant.CreateOrderCallCount;

            var pay = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );

            Assert.Equal(HttpStatusCode.Conflict, pay.StatusCode);
            var body = await pay.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("qr_limit_reached", body.GetProperty("code").GetString());
            Assert.Equal(cap, body.GetProperty("limit").GetInt32());
            Assert.Equal(createsBefore, _factory.Merchant.CreateOrderCallCount);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Empty(await db.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == orderId)
                .ToListAsync());
            Assert.Empty(await db.RevolutOrderIntents
                .Where(row => row.ShopOrderId == orderId)
                .ToListAsync());
        }

        [Fact]
        public async Task Pay_ReorderAtLimit_ReusesActive_SucceedsAndWritesNoReservation()
        {
            var workspace = await SeedAsync(freeSlots: 0, TableTents);
            var orderId = await InsertOrderAsync(workspace, TableTents);

            var pay = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );

            Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Empty(await db.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == orderId)
                .ToListAsync());
        }

        [Fact]
        public async Task Pay_MixedReuseAndNew_AtLimitMinusOne_WritesExactlyOneReservation()
        {
            var workspace = await SeedAsync(
                freeSlots: 1,
                GuestCards,
                TableTents,
                WindowStickers
            );
            var orderId = await InsertOrderAsync(
                workspace,
                TableTents,
                GuestCards,
                WindowStickers
            );

            var pay = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );

            Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reservation = Assert.Single(await db.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == orderId)
                .ToListAsync());
            Assert.Equal(QrType.GuestCard, reservation.QrType);
            Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
            Assert.Equal(workspace.LocationId, reservation.RestaurantLocationId);
            Assert.Equal(
                reservation.CreatedAtUtc.AddMinutes(30),
                reservation.ReconcileAfterUtc
            );
        }

        [Fact]
        public async Task Pay_Repeated_SameOrder_OneReservationOneIntent()
        {
            var workspace = await SeedAsync(freeSlots: 1, GuestCards);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var key = Guid.NewGuid().ToString("D");
            var createsBefore = _factory.Merchant.CreateOrderCallCount;

            var first = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId, key)
            );
            var second = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId, key)
            );

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal(createsBefore + 1, _factory.Merchant.CreateOrderCallCount);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Single(await db.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == orderId)
                .ToListAsync());
            Assert.Single(await db.RevolutOrderIntents
                .Where(row => row.ShopOrderId == orderId)
                .ToListAsync());
        }

        [Fact]
        public async Task Pay_NeedsSlotWithExistingHeldReservationForSameKey_SucceedsAtLimit()
        {
            var workspace = await SeedAsync(freeSlots: 1, GuestCards);
            var firstOrder = await InsertOrderAsync(workspace, GuestCards);
            var secondOrder = await InsertOrderAsync(workspace, GuestCards);

            var first = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, firstOrder)
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            // The first order holds the last slot. A different type is refused.
            var tentless = await InsertOrderAsync(workspace, TableTalker);
            var other = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, tentless)
            );
            Assert.Equal(HttpStatusCode.Conflict, other.StatusCode);

            var second = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, secondOrder)
            );

            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rows = await db.ShopQrSlotReservations
                .Where(row => row.RestaurantLocationId == workspace.LocationId)
                .ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => Assert.Equal(QrType.GuestCard, row.QrType));
            Assert.All(rows, row =>
                Assert.Equal(ShopQrSlotReservationStatuses.Open, row.Status));
        }

        private async Task<ShopCapacityWorkspace> SeedAsync(
            int freeSlots,
            params string[] orderableSkus
        )
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
                orderableSkus.Append(TableTalker).Distinct().ToArray()
            );
            await ShopCapacityTestSeed.LeaveFreeSlotsAsync(
                context,
                workspace,
                freeSlots
            );
            return workspace;
        }

        private async Task<int> CapAsync(ShopCapacityWorkspace workspace)
        {
            using var scope = _factory.Services.CreateScope();
            return await ShopCapacityTestSeed.CapAsync(
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                workspace
            );
        }

        private async Task<Guid> InsertOrderAsync(
            ShopCapacityWorkspace workspace,
            params string[] skuIds
        )
        {
            using var scope = _factory.Services.CreateScope();
            return await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                workspace,
                skuIds
            );
        }
    }
}
