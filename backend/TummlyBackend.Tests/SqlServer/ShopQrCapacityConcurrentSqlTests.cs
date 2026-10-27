using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Interfaces;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;
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

        [SkippableFact]
        public async Task Sweep_Completed_OnSqlServer_ConvertsUnderBillingLock()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await using var factory = _fixture.CreateFactory();
            var workspace = await SeedAsync(factory, freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            using var client = factory.CreateClient();
            using (var pay = await client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)))
            {
                Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
            }

            await using var read = _fixture.CreateContext();
            var intent = await read.RevolutOrderIntents
                .SingleAsync(row => row.ShopOrderId == orderId);
            var reservation = await read.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            var merchant = (FakeFirstPaidRevolutMerchantClient)factory.Services
                .GetRequiredService<IRevolutMerchantClient>();
            merchant.Orders[intent.OrderId] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: intent.OrderId,
                State: "completed",
                AmountMinor: 1200,
                CheckoutUrl: FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
                PaymentMethodSummary: "Paid"
            );
            var sweeper = new ShopQrSlotReservationSweeperBackgroundService(
                factory.Services.GetRequiredService<IServiceScopeFactory>(),
                new AdjustableTimeProvider(
                    new DateTimeOffset(
                        reservation.CreatedAtUtc.AddMinutes(31),
                        TimeSpan.Zero
                    )
                ),
                NullLogger<ShopQrSlotReservationSweeperBackgroundService>.Instance
            );

            // The sweep has no ambient transaction. It must open one and queue
            // behind the account lock like any other writer.
            await using var blocker = _fixture.CreateContext();
            await using var blockerTx = await blocker.Database.BeginTransactionAsync();
            await blocker.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM [BillingAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [RestaurantId] = {workspace.RestaurantId}"
            );
            var sweep = Task.Run(() => sweeper.SweepAsync(CancellationToken.None));
            await WaitForBlockedRequestsAsync(blocker, expected: 1);
            Assert.False(sweep.IsCompleted);
            await blockerTx.CommitAsync();
            await sweep;

            await using var verify = _fixture.CreateContext();
            var order = await verify.ShopOrders.SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            var converted = await verify.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, converted.Status);
            var code = await verify.QrCodes.SingleAsync(row =>
                row.RestaurantLocationId == workspace.LocationId
                && row.QrType == QrType.GuestCard
            );
            Assert.Equal(QrCodeStatus.Active, code.Status);
            Assert.Equal(code.Id, converted.ConvertedQrCodeId);
        }

        [SkippableFact]
        public async Task Sweep_ReservationConvertedWhileRevolutIsRead_StaysConverted_OnSqlServer()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await using var factory = _fixture.CreateFactory();
            var workspace = await SeedAsync(factory, freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            using var client = factory.CreateClient();
            using (var pay = await client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)))
            {
                Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
            }

            await using var read = _fixture.CreateContext();
            var intent = await read.RevolutOrderIntents
                .SingleAsync(row => row.ShopOrderId == orderId);
            var reservation = await read.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            var merchant = (FakeFirstPaidRevolutMerchantClient)factory.Services
                .GetRequiredService<IRevolutMerchantClient>();
            merchant.Orders[intent.OrderId] = new RevolutOrderRetrieveResult(
                Succeeded: true,
                Id: intent.OrderId,
                State: "processing"
            );
            // The paid path converts the row after the sweeper read it and
            // before its back-off write.
            merchant.OnGetOrderAsync = async _ =>
            {
                merchant.OnGetOrderAsync = null;
                await using var other = _fixture.CreateContext();
                await other.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [ShopQrSlotReservations] SET [Status] = N'converted', [ConvertedQrCodeId] = 4242 WHERE [ShopOrderId] = {orderId}"
                );
            };
            var sweeper = new ShopQrSlotReservationSweeperBackgroundService(
                factory.Services.GetRequiredService<IServiceScopeFactory>(),
                new AdjustableTimeProvider(
                    new DateTimeOffset(
                        reservation.CreatedAtUtc.AddMinutes(31),
                        TimeSpan.Zero
                    )
                ),
                NullLogger<ShopQrSlotReservationSweeperBackgroundService>.Instance
            );

            await sweeper.SweepAsync(CancellationToken.None);

            await using var verify = _fixture.CreateContext();
            var after = await verify.ShopQrSlotReservations
                .AsNoTracking()
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, after.Status);
            Assert.Equal(0, after.ReconcileAttempts);
            Assert.Null(after.LastReconcileError);
        }

        [SkippableFact]
        public async Task Release_RowConvertedAfterTheDecisionToRelease_StaysConverted()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await using var factory = _fixture.CreateFactory();
            var workspace = await SeedAsync(factory, freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            await using (var seed = _fixture.CreateContext())
            {
                seed.ShopQrSlotReservations.Add(OpenReservation(workspace, orderId));
                await seed.SaveChangesAsync();
            }

            // The paid path converts the row just before the release writes.
            var interceptor = new BeforeReservationUpdateInterceptor(() =>
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(
                    _fixture.ConnectionString
                );
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE [ShopQrSlotReservations] SET [Status] = N'converted', [ConvertedQrCodeId] = 4242 WHERE [ShopOrderId] = @id";
                command.Parameters.AddWithValue("@id", orderId);
                command.ExecuteNonQuery();
            });
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_fixture.ConnectionString)
                .AddInterceptors(interceptor)
                .Options;
            await using (var releasing = new ApplicationDbContext(options))
            {
                await ShopQrSlotReservationRelease.ReleaseOpenForOrderAsync(
                    releasing,
                    orderId,
                    DateTime.UtcNow,
                    CancellationToken.None
                );
                await releasing.SaveChangesAsync();
            }

            Assert.True(interceptor.Fired);
            await using var verify = _fixture.CreateContext();
            var row = await verify.ShopQrSlotReservations
                .AsNoTracking()
                .SingleAsync(r => r.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, row.Status);
            Assert.Equal(4242, row.ConvertedQrCodeId);
        }

        [SkippableFact]
        public async Task Release_RefreshesRowsTheContextAlreadyTracks()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await using var factory = _fixture.CreateFactory();
            var workspace = await SeedAsync(factory, freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            await using var context = _fixture.CreateContext();
            context.ShopQrSlotReservations.Add(OpenReservation(workspace, orderId));
            await context.SaveChangesAsync();
            var tracked = await context.ShopQrSlotReservations
                .SingleAsync(r => r.ShopOrderId == orderId);

            var released = await ShopQrSlotReservationRelease.ReleaseOpenForOrderAsync(
                context,
                orderId,
                DateTime.UtcNow,
                CancellationToken.None
            );

            Assert.Equal(1, released);
            Assert.Equal(ShopQrSlotReservationStatuses.Released, tracked.Status);
        }

        private static ShopQrSlotReservation OpenReservation(
            ShopCapacityWorkspace workspace,
            Guid orderId
        ) => new()
        {
            Id = Guid.NewGuid(),
            ShopOrderId = orderId,
            RestaurantLocationId = workspace.LocationId,
            QrType = QrType.GuestCard,
            Status = ShopQrSlotReservationStatuses.Open,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ReconcileAfterUtc = DateTime.UtcNow.AddMinutes(30),
        };

        /// <summary>
        /// Runs a hook once, right before the first UPDATE of a reservation row
        /// is sent, so a test can interleave another writer.
        /// </summary>
        private sealed class BeforeReservationUpdateInterceptor
            : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
        {
            private readonly Action _hook;

            public BeforeReservationUpdateInterceptor(Action hook)
            {
                _hook = hook;
            }

            public bool Fired { get; private set; }

            public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>
                ReaderExecuting(
                    System.Data.Common.DbCommand command,
                    Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
                    Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result
                )
            {
                Fire(command);
                return result;
            }

            public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>>
                ReaderExecutingAsync(
                    System.Data.Common.DbCommand command,
                    Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
                    Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result,
                    CancellationToken cancellationToken = default
                )
            {
                Fire(command);
                return ValueTask.FromResult(result);
            }

            public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>
                NonQueryExecuting(
                    System.Data.Common.DbCommand command,
                    Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
                    Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result
                )
            {
                Fire(command);
                return result;
            }

            public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>>
                NonQueryExecutingAsync(
                    System.Data.Common.DbCommand command,
                    Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
                    Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
                    CancellationToken cancellationToken = default
                )
            {
                Fire(command);
                return ValueTask.FromResult(result);
            }

            private void Fire(System.Data.Common.DbCommand command)
            {
                if (
                    Fired
                    || !command.CommandText.Contains("UPDATE", StringComparison.Ordinal)
                    || !command.CommandText.Contains(
                        "[ShopQrSlotReservations]",
                        StringComparison.Ordinal
                    )
                )
                {
                    return;
                }

                Fired = true;
                _hook();
            }
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
