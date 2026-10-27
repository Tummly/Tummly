using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Conversion of Shop QR slot reservations on payment, and release on
    /// failure or cancel (spec 4.4.3 to 4.4.5, tests 6.2, 6.4, 6.6).
    /// </summary>
    public class ShopQrSlotConversionTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string SigningSecret = "whsec_placeholder";
        private const string GuestCards = "qr-guest-cards";
        private const string TableTalker = "table-talker";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopQrSlotConversionTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Paid_GuestCardOrder_NoCode_MintsOneActive_PrintJobReady()
        {
            var workspace = await SeedAsync(freeSlots: 2);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);

            await CompleteViaWebhookAsync(revolutOrderId);

            await using var db = OpenDb();
            var code = Assert.Single(await GuestCardCodesAsync(db, workspace));
            Assert.Equal(QrCodeStatus.Active, code.Status);
            Assert.Null(code.ArchivedAt);
            Assert.False(string.IsNullOrWhiteSpace(code.Token));
            var reservation = await db.Context.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.Equal(code.Id, reservation.ConvertedQrCodeId);
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.Null(order.ReviewReason);
            var asset = await db.Context.PrintReadyQrAssets
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(QrType.GuestCard, asset.QrType);
            Assert.Equal(PrintReadyQrAssetStatus.Ready, asset.Status);
        }

        [Fact]
        public async Task Paid_SecondOrder_ReusesSameCode()
        {
            var workspace = await SeedAsync(freeSlots: 2);
            var first = await InsertOrderAsync(workspace, GuestCards);
            await CompleteViaWebhookAsync(await PayAsync(workspace, first));
            var second = await InsertOrderAsync(workspace, GuestCards);

            await CompleteViaWebhookAsync(await PayAsync(workspace, second));

            await using var db = OpenDb();
            var code = Assert.Single(await GuestCardCodesAsync(db, workspace));
            // The second order found an Active code, so it wrote no reservation.
            Assert.Empty(await db.Context.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == second)
                .ToListAsync());
            var secondOrder = await db.Context.ShopOrders
                .SingleAsync(row => row.Id == second);
            Assert.Equal(ShopPaymentStatuses.Paid, secondOrder.PaymentStatus);
            Assert.Equal(code.Id, (await db.Context.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == first)).ConvertedQrCodeId);
        }

        [Fact]
        public async Task Paid_PausedCodeExists_ReactivatesKeepingToken()
        {
            var workspace = await SeedAsync(freeSlots: 2);
            var paused = await AddGuestCardCodeAsync(workspace, QrCodeStatus.Paused);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);

            await CompleteViaApplierAsync(revolutOrderId);

            await using var db = OpenDb();
            var code = Assert.Single(await GuestCardCodesAsync(db, workspace));
            Assert.Equal(paused.Id, code.Id);
            Assert.Equal(paused.Token, code.Token);
            Assert.Equal(QrCodeStatus.Active, code.Status);
            var reservation = await db.Context.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(paused.Id, reservation.ReactivateQrCodeId);
            Assert.Equal(paused.Id, reservation.ConvertedQrCodeId);
        }

        [Fact]
        public async Task Paid_OnlyArchivedCodeExists_MintsNewCode()
        {
            var workspace = await SeedAsync(freeSlots: 2);
            var archived = await AddGuestCardCodeAsync(workspace, QrCodeStatus.Archived);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);

            await CompleteViaApplierAsync(revolutOrderId);

            await using var db = OpenDb();
            var codes = await GuestCardCodesAsync(db, workspace);
            Assert.Equal(2, codes.Count);
            var stillArchived = codes.Single(code => code.Id == archived.Id);
            Assert.Equal(QrCodeStatus.Archived, stillArchived.Status);
            var minted = codes.Single(code => code.Id != archived.Id);
            Assert.Equal(QrCodeStatus.Active, minted.Status);
            Assert.NotEqual(archived.Token, minted.Token);
            Assert.Equal(
                minted.Id,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == orderId)).ConvertedQrCodeId
            );
        }

        [Fact]
        public async Task Paid_PlanCapLoweredAfterReserve_StillConverts()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);
            await SetPlanAsync(workspace, BillingSubscriptionPlans.Free);

            await CompleteViaApplierAsync(revolutOrderId);

            await using var db = OpenDb();
            var code = Assert.Single(await GuestCardCodesAsync(db, workspace));
            Assert.Equal(QrCodeStatus.Active, code.Status);
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.Null(order.ReviewReason);
        }

        [Fact]
        public async Task SharedKey_FirstOrderFails_SlotStillHeld_CaptureActivateDenied()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            var first = await InsertOrderAsync(workspace, GuestCards);
            var second = await InsertOrderAsync(workspace, GuestCards);
            var firstRevolut = await PayAsync(workspace, first);
            await PayAsync(workspace, second);

            await SendWebhookAsync("ORDER_FAILED", firstRevolut);

            await using var db = OpenDb();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Released,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == first)).Status
            );
            Assert.Equal(
                ShopQrSlotReservationStatuses.Open,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == second)).Status
            );
            var capture = await CaptureCreateAsync(workspace);
            Assert.NotEqual(QrLifecycleResultKind.Ok, capture.Kind);
        }

        [Fact]
        public async Task SharedKey_FirstConverts_SecondLaterConvertsToSameCode()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            var first = await InsertOrderAsync(workspace, GuestCards);
            var second = await InsertOrderAsync(workspace, GuestCards);
            var firstRevolut = await PayAsync(workspace, first);
            var secondRevolut = await PayAsync(workspace, second);

            await CompleteViaApplierAsync(firstRevolut);
            await CompleteViaApplierAsync(secondRevolut);

            await using var db = OpenDb();
            var code = Assert.Single(await GuestCardCodesAsync(db, workspace));
            var reservations = await db.Context.ShopQrSlotReservations
                .Where(row => row.RestaurantLocationId == workspace.LocationId)
                .ToListAsync();
            Assert.Equal(2, reservations.Count);
            Assert.All(reservations, row =>
            {
                Assert.Equal(ShopQrSlotReservationStatuses.Converted, row.Status);
                Assert.Equal(code.Id, row.ConvertedQrCodeId);
            });
        }

        [Fact]
        public async Task OrderCompleted_DeliveredTwice_OneCodeOneConversion()
        {
            var workspace = await SeedAsync(freeSlots: 2);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);

            await CompleteViaWebhookAsync(revolutOrderId);
            DateTime convertedAt;
            await using (var db = OpenDb())
            {
                convertedAt = (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == orderId)).UpdatedAtUtc;
            }

            var replay = await SendWebhookAsync("ORDER_COMPLETED", revolutOrderId);
            Assert.True(replay.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
            // The applier on its own is idempotent too (sweeper and webhook race).
            await CompleteViaApplierAsync(revolutOrderId);

            await using var verify = OpenDb();
            Assert.Single(await GuestCardCodesAsync(verify, workspace));
            var reservation = await verify.Context.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.Equal(convertedAt, reservation.UpdatedAtUtc);
            Assert.Equal(1, await verify.Context.TummlyVatInvoices
                .CountAsync(row => row.RevolutOrderId == revolutOrderId));
        }

        [Fact]
        public async Task OrderCancelled_AfterCompleted_ReservationStaysConverted()
        {
            var workspace = await SeedAsync(freeSlots: 2);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);
            await CompleteViaWebhookAsync(revolutOrderId);

            var cancelled = await SendWebhookAsync("ORDER_CANCELLED", revolutOrderId);
            Assert.True(cancelled.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);

            await using var db = OpenDb();
            var reservation = await db.Context.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.NotNull(reservation.ConvertedQrCodeId);
            Assert.Equal(
                ShopPaymentStatuses.Paid,
                (await db.Context.ShopOrders.SingleAsync(row => row.Id == orderId))
                    .PaymentStatus
            );
        }

        [Fact]
        public async Task OrderFailed_ReleasesOpenReservation()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);

            await SendWebhookAsync("ORDER_FAILED", revolutOrderId);

            await using var db = OpenDb();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Released,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == orderId)).Status
            );
            Assert.Equal(
                ShopPaymentStatuses.PaymentFailed,
                (await db.Context.ShopOrders.SingleAsync(row => row.Id == orderId))
                    .PaymentStatus
            );
        }

        [Theory]
        [InlineData("ORDER_PAYMENT_DECLINED")]
        [InlineData("ORDER_PAYMENT_FAILED")]
        [InlineData("ORDER_AUTHORISED")]
        public async Task NonTerminalOrderEvents_LeaveReservationOpen(string eventName)
        {
            // The customer can retry or the payment can still capture, so only a
            // confirmed ORDER_FAILED or ORDER_CANCELLED gives the slot back.
            var workspace = await SeedAsync(freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);

            await SendWebhookAsync(eventName, revolutOrderId);

            await using var db = OpenDb();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Open,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == orderId)).Status
            );
        }

        [Fact]
        public async Task OrderCancelled_ReleasesOpenReservation()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);

            await SendWebhookAsync("ORDER_CANCELLED", revolutOrderId);

            await using var db = OpenDb();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Released,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == orderId)).Status
            );
        }

        [Fact]
        public async Task OrderCompleted_AfterRelease_CapacityFull_OrderReviewNoRefund()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);
            await SendWebhookAsync("ORDER_FAILED", revolutOrderId);
            // Capture takes the slot the release freed.
            await using (var fill = OpenDb())
            {
                await ShopCapacityTestSeed.LeaveFreeSlotsAsync(
                    fill.Context,
                    workspace,
                    freeSlots: 0
                );
            }

            var refundsBefore = _factory.Merchant.RefundOrderCallCount;
            await CompleteViaWebhookAsync(revolutOrderId);

            await using var db = OpenDb();
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.Equal(
                ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment,
                order.ReviewReason
            );
            Assert.Empty(await GuestCardCodesAsync(db, workspace));
            Assert.Equal(refundsBefore, _factory.Merchant.RefundOrderCallCount);
        }

        [Fact]
        public async Task OrderCompleted_AfterRelease_CapacityFree_MintsCode()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            var revolutOrderId = await PayAsync(workspace, orderId);
            await SendWebhookAsync("ORDER_FAILED", revolutOrderId);

            await CompleteViaWebhookAsync(revolutOrderId);

            await using var db = OpenDb();
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == orderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.Null(order.ReviewReason);
            var code = Assert.Single(await GuestCardCodesAsync(db, workspace));
            Assert.Equal(
                code.Id,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == orderId)).ConvertedQrCodeId
            );
        }

        [Fact]
        public async Task Cancel_UnpaidOrder_ReleasesReservation()
        {
            // CancelAsync only accepts paid orders, so the release for an unpaid
            // order runs through the service seam that cancel and failure share.
            var workspace = await SeedAsync(freeSlots: 1);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            await PayAsync(workspace, orderId);

            using (var scope = _factory.Services.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IShopQrSlotReservationService>()
                    .ReleaseForOrderAsync(orderId, CancellationToken.None);
            }

            await using var db = OpenDb();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Released,
                (await db.Context.ShopQrSlotReservations
                    .SingleAsync(row => row.ShopOrderId == orderId)).Status
            );
        }

        [Fact]
        public async Task Cancel_PaidOrder_ReleasesStrayOpenReservation_AndKeepsConverted()
        {
            var workspace = await SeedAsync(freeSlots: 2);
            var orderId = await InsertOrderAsync(workspace, GuestCards);
            await CompleteViaWebhookAsync(await PayAsync(workspace, orderId));
            await using (var seed = OpenDb())
            {
                // A stray open row for another type on the paid order.
                seed.Context.ShopQrSlotReservations.Add(new ShopQrSlotReservation
                {
                    Id = Guid.NewGuid(),
                    ShopOrderId = orderId,
                    RestaurantLocationId = workspace.LocationId,
                    QrType = QrType.TableTalker,
                    Status = ShopQrSlotReservationStatuses.Open,
                    ReconcileAfterUtc = DateTime.UtcNow.AddMinutes(30),
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                });
                await seed.Context.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var cancel = scope.ServiceProvider
                    .GetRequiredService<IShopOrderCancelReorderService>();
                var result = await cancel.CancelAsync(
                    workspace.RestaurantId,
                    workspace.OwnerUserId,
                    orderId,
                    workspace.LocationId,
                    ShopCancelReasons.OrderedByMistake
                );
                Assert.NotNull(result.Order);
            }

            await using var db = OpenDb();
            var rows = await db.Context.ShopQrSlotReservations
                .Where(row => row.ShopOrderId == orderId)
                .ToListAsync();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Converted,
                rows.Single(row => row.QrType == QrType.GuestCard).Status
            );
            Assert.Equal(
                ShopQrSlotReservationStatuses.Released,
                rows.Single(row => row.QrType == QrType.TableTalker).Status
            );
        }

        // ---- arrange / act helpers ----

        private async Task<ShopCapacityWorkspace> SeedAsync(int freeSlots)
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

        /// <summary>
        /// Pays over HTTP (writes the reservation and the Revolut intent) and
        /// returns the Revolut order id.
        /// </summary>
        private async Task<string> PayAsync(
            ShopCapacityWorkspace workspace,
            Guid orderId
        )
        {
            var response = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await using var db = OpenDb();
            return await db.Context.RevolutOrderIntents
                .Where(row => row.ShopOrderId == orderId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Select(row => row.OrderId)
                .FirstAsync();
        }

        private async Task CompleteViaApplierAsync(string revolutOrderId)
        {
            using var scope = _factory.Services.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<IRevolutOrderCompletedApplier>()
                .ApplyAsync(
                    new RevolutOrderCompletedApplyRequest(
                        OrderId: revolutOrderId,
                        OrderState: "completed",
                        BillingReason: null,
                        SubscriptionId: null,
                        RawWebhookBody: "{}",
                        RawOrderBody: "{}"
                    )
                );
        }

        private async Task CompleteViaWebhookAsync(string revolutOrderId)
        {
            _factory.Merchant.Orders[revolutOrderId] =
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: revolutOrderId,
                    State: "completed",
                    AmountMinor: 1200,
                    CheckoutUrl: FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
                    PaymentMethodSummary: "Paid"
                );
            var response = await SendWebhookAsync("ORDER_COMPLETED", revolutOrderId);
            Assert.True(
                response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent
            );
        }

        private async Task<HttpResponseMessage> SendWebhookAsync(
            string eventName,
            string revolutOrderId
        )
        {
            var body =
                $$"""{"event":"{{eventName}}","order_id":"{{revolutOrderId}}"}""";
            const string timestamp = "1710000000";
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/webhooks/revolut"
            )
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(
                "Revolut-Request-Timestamp",
                timestamp
            );
            request.Headers.TryAddWithoutValidation(
                "Revolut-Signature",
                RevolutWebhookSignature.SignForTests(SigningSecret, timestamp, body)
            );
            var response = await _client.SendAsync(request);
            await _factory.Services
                .GetRequiredService<IPrintReadyQrMaterialsWork>()
                .DrainAsync();
            return response;
        }

        private async Task<QrLifecycleResult> CaptureCreateAsync(
            ShopCapacityWorkspace workspace
        )
        {
            using var scope = _factory.Services.CreateScope();
            return await scope.ServiceProvider
                .GetRequiredService<ICaptureQrLifecycleService>()
                .CreateDigitalGuestLinkAsync(
                    new CreateDigitalGuestLinkCommand
                    {
                        UserId = workspace.OwnerUserId,
                        LocationId = workspace.LocationId,
                        LinkName = $"Link {Guid.NewGuid():N}"[..20],
                        Channel = "Website",
                        Status = "Active",
                    }
                );
        }

        private async Task<QrCode> AddGuestCardCodeAsync(
            ShopCapacityWorkspace workspace,
            QrCodeStatus status
        )
        {
            await using var db = OpenDb();
            var code = new QrCode
            {
                RestaurantLocationId = workspace.LocationId,
                QrType = QrType.GuestCard,
                Token = $"guest-{Guid.NewGuid():N}",
                Status = status,
                ArchivedAt = status == QrCodeStatus.Archived
                    ? DateTime.UtcNow
                    : null,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
            };
            db.Context.QrCodes.Add(code);
            await db.Context.SaveChangesAsync();
            return code;
        }

        private async Task SetPlanAsync(
            ShopCapacityWorkspace workspace,
            string plan
        )
        {
            await using var db = OpenDb();
            var account = await db.Context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == workspace.RestaurantId);
            account.SubscriptionPlan = plan;
            await db.Context.SaveChangesAsync();
        }

        private static Task<List<QrCode>> GuestCardCodesAsync(
            ScopedDb db,
            ShopCapacityWorkspace workspace
        ) => db.Context.QrCodes
            .AsNoTracking()
            .Where(row =>
                row.RestaurantLocationId == workspace.LocationId
                && row.QrType == QrType.GuestCard
            )
            .ToListAsync();

        private ScopedDb OpenDb() => new(_factory.Services);

        private sealed class ScopedDb : IAsyncDisposable
        {
            private readonly IServiceScope _scope;

            public ScopedDb(IServiceProvider services)
            {
                _scope = services.CreateScope();
                Context = _scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
            }

            public ApplicationDbContext Context { get; }

            public ValueTask DisposeAsync()
            {
                _scope.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
