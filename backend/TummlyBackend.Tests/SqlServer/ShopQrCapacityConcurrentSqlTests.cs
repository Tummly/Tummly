using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.SqlServer
{
    /// <summary>
    /// Shop /pay and Capture compete for the last QR slot under the real
    /// <c>BillingAccounts</c> UPDLOCK (spec 6.3 items 14 to 16).
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public sealed class ShopQrCapacityConcurrentSqlTests
    {
        private const string GuestCards = "qr-guest-cards";
        private const string TableTalker = "table-talker";

        private readonly SqlServerFixture _fixture;

        public ShopQrCapacityConcurrentSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task TwoConcurrentPays_DifferentTypes_LastSlot_ExactlyOneSucceeds()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await using var factory = _fixture.CreateFactory();
            var workspace = await SeedAsync(factory, freeSlots: 1);
            var guestCardOrder = await InsertOrderAsync(workspace, GuestCards);
            var talkerOrder = await InsertOrderAsync(workspace, TableTalker);

            var statuses = await RunBlockedAsync(
                workspace,
                PayAsync(factory, workspace, guestCardOrder),
                PayAsync(factory, workspace, talkerOrder)
            );

            Assert.Equal(1, statuses.Count(code => code == HttpStatusCode.OK));
            Assert.Equal(1, statuses.Count(code => code == HttpStatusCode.Conflict));
            await using var verify = _fixture.CreateContext();
            var rows = await verify.ShopQrSlotReservations
                .Where(row => row.RestaurantLocationId == workspace.LocationId)
                .ToListAsync();
            Assert.Single(rows);
            Assert.Equal(
                await ShopCapacityTestSeed.CapAsync(verify, workspace),
                await CountUsedAsync(factory, workspace)
            );
        }

        [SkippableFact]
        public async Task TwoConcurrentPays_SameType_LastSlot_BothSucceedOneSlot()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await using var factory = _fixture.CreateFactory();
            var workspace = await SeedAsync(factory, freeSlots: 1);
            var firstOrder = await InsertOrderAsync(workspace, GuestCards);
            var secondOrder = await InsertOrderAsync(workspace, GuestCards);

            var statuses = await RunBlockedAsync(
                workspace,
                PayAsync(factory, workspace, firstOrder),
                PayAsync(factory, workspace, secondOrder)
            );

            Assert.All(statuses, code => Assert.Equal(HttpStatusCode.OK, code));
            await using var verify = _fixture.CreateContext();
            var rows = await verify.ShopQrSlotReservations
                .Where(row => row.RestaurantLocationId == workspace.LocationId)
                .ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => Assert.Equal(QrType.GuestCard, row.QrType));
            Assert.Equal(
                await ShopCapacityTestSeed.CapAsync(verify, workspace),
                await CountUsedAsync(factory, workspace)
            );
        }

        [SkippableFact]
        public async Task ConcurrentCaptureActivateAndShopPay_LastSlot_ExactlyOneSucceeds()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await using var factory = _fixture.CreateFactory();
            var workspace = await SeedAsync(factory, freeSlots: 1);
            var order = await InsertOrderAsync(workspace, GuestCards);

            var results = await RunBlockedAsync(
                workspace,
                PayAsync(factory, workspace, order),
                CaptureCreateAsync(factory, workspace)
            );

            var payOk = results[0] == HttpStatusCode.OK;
            var captureOk = results[1] == HttpStatusCode.OK;
            Assert.True(payOk ^ captureOk, $"pay={results[0]} capture={results[1]}");

            await using var verify = _fixture.CreateContext();
            var reservations = await verify.ShopQrSlotReservations
                .CountAsync(row => row.RestaurantLocationId == workspace.LocationId);
            Assert.Equal(payOk ? 1 : 0, reservations);
            Assert.Equal(
                await ShopCapacityTestSeed.CapAsync(verify, workspace),
                await CountUsedAsync(factory, workspace)
            );
        }

        /// <summary>
        /// Holds the account lock, starts both calls, waits until both queue
        /// behind it, then releases. The calls then contend for real.
        /// </summary>
        private async Task<HttpStatusCode[]> RunBlockedAsync(
            ShopCapacityWorkspace workspace,
            Func<Task<HttpStatusCode>> first,
            Func<Task<HttpStatusCode>> second
        )
        {
            await using var blocker = _fixture.CreateContext();
            await using var blockerTx = await blocker.Database.BeginTransactionAsync();
            await blocker.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM [BillingAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [RestaurantId] = {workspace.RestaurantId}"
            );

            var firstTask = Task.Run(first);
            var secondTask = Task.Run(second);
            await WaitForBlockedRequestsAsync(blocker, expected: 2);
            await blockerTx.CommitAsync();

            return await Task.WhenAll(firstTask, secondTask);
        }

        private Func<Task<HttpStatusCode>> PayAsync(
            WebApplicationFactory<Program> factory,
            ShopCapacityWorkspace workspace,
            Guid orderId
        ) => async () =>
        {
            using var client = factory.CreateClient();
            using var response = await client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );
            return response.StatusCode;
        };

        private Func<Task<HttpStatusCode>> CaptureCreateAsync(
            WebApplicationFactory<Program> factory,
            ShopCapacityWorkspace workspace
        ) => async () =>
        {
            using var scope = factory.Services.CreateScope();
            var capture = scope.ServiceProvider
                .GetRequiredService<ICaptureQrLifecycleService>();
            var result = await capture.CreateDigitalGuestLinkAsync(
                new CreateDigitalGuestLinkCommand
                {
                    UserId = workspace.OwnerUserId,
                    LocationId = workspace.LocationId,
                    LinkName = $"Race {Guid.NewGuid():N}"[..20],
                    Channel = "Website",
                    Status = "Active",
                }
            );
            return result.Kind == QrLifecycleResultKind.Ok
                ? HttpStatusCode.OK
                : HttpStatusCode.Conflict;
        };

        private async Task<int> CountUsedAsync(
            WebApplicationFactory<Program> factory,
            ShopCapacityWorkspace workspace
        )
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider
                .GetRequiredService<IActiveQrCapacity>()
                .CountUsedAsync(workspace.LocationId, CancellationToken.None);
        }

        private async Task<ShopCapacityWorkspace> SeedAsync(
            WebApplicationFactory<Program> factory,
            int freeSlots
        )
        {
            await using var context = _fixture.CreateContext();
            await ShopMaterialsCatalogSeed.EnsureSeededAsync(context);

            // The database is shared, so Revolut order ids must be unique.
            ((FakeFirstPaidRevolutMerchantClient)factory.Services
                .GetRequiredService<IRevolutMerchantClient>()).OrderIdPrefix =
                $"ord_cap_{Guid.NewGuid():N}_";

            using var scope = factory.Services.CreateScope();
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

        private async Task<Guid> InsertOrderAsync(
            ShopCapacityWorkspace workspace,
            params string[] skuIds
        )
        {
            await using var context = _fixture.CreateContext();
            return await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                context,
                workspace,
                skuIds
            );
        }

        private static async Task WaitForBlockedRequestsAsync(
            ApplicationDbContext context,
            int expected
        )
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (true)
            {
                var blocked = await context.Database
                    .SqlQuery<int>(
                        $"SELECT COUNT(*) AS [Value] FROM sys.dm_exec_requests WHERE blocking_session_id <> 0"
                    )
                    .SingleAsync();
                if (blocked >= expected)
                {
                    return;
                }

                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException(
                        $"Expected {expected} blocked requests, saw {blocked}."
                    );
                }

                await Task.Delay(100);
            }
        }
    }
}
