using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Reconciliation of stale Shop QR slot reservations with Revolut
    /// (spec 4.4.4, tests 6.5).
    /// </summary>
    public class ShopQrSlotReservationSweeperTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string GuestCards = "qr-guest-cards";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopQrSlotReservationSweeperTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Sweep_Pending_CancelSucceeds_OrderFailed_Released()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "pending");
            _factory.Merchant.CancelResults[held.RevolutOrderId] = Revolut(held, "cancelled");
            var cancelsBefore = _factory.Merchant.CancelOrderCallCount;
            var refundsBefore = _factory.Merchant.RefundOrderCallCount;
            var clock = ClockAfterDeadline(held);

            await SweepAsync(clock);

            Assert.Equal(cancelsBefore + 1, _factory.Merchant.CancelOrderCallCount);
            Assert.Equal(refundsBefore, _factory.Merchant.RefundOrderCallCount);
            await using var db = OpenDb();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Released,
                (await ReservationAsync(db, held)).Status
            );
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId);
            Assert.Equal(ShopPaymentStatuses.PaymentFailed, order.PaymentStatus);
            var intent = await db.Context.RevolutOrderIntents
                .SingleAsync(row => row.ShopOrderId == held.OrderId);
            Assert.False(intent.IsOpen);
        }

        [Fact]
        public async Task Sweep_Pending_CancelFails_StaysOpen_AttemptsIncrement_RetryIn5Min()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "pending");
            _factory.Merchant.CancelResults[held.RevolutOrderId] =
                new RevolutOrderRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error"
                );
            var clock = ClockAfterDeadline(held);

            await SweepAsync(clock);

            await using var db = OpenDb();
            var reservation = await ReservationAsync(db, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
            Assert.Equal(1, reservation.ReconcileAttempts);
            Assert.Equal(clock.GetUtcNow().UtcDateTime, reservation.LastReconcileAtUtc);
            Assert.Equal(
                clock.GetUtcNow().UtcDateTime.AddMinutes(5),
                reservation.ReconcileAfterUtc
            );
            Assert.Contains("cancel_failed", reservation.LastReconcileError);
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId);
            Assert.Equal(ShopPaymentStatuses.AwaitingPayment, order.PaymentStatus);
        }

        [Fact]
        public async Task Sweep_Processing_StaysOpen_ThenCompleted_Converts()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "processing");
            var clock = ClockAfterDeadline(held);

            await SweepAsync(clock);

            await using (var db = OpenDb())
            {
                var reservation = await ReservationAsync(db, held);
                Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
                Assert.Equal(1, reservation.ReconcileAttempts);
                Assert.Equal(
                    clock.GetUtcNow().UtcDateTime.AddMinutes(5),
                    reservation.ReconcileAfterUtc
                );
            }

            // Before the retry time nothing is touched.
            clock.Advance(TimeSpan.FromMinutes(4));
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "completed");
            await SweepAsync(clock);
            await using (var db = OpenDb())
            {
                Assert.Equal(
                    ShopQrSlotReservationStatuses.Open,
                    (await ReservationAsync(db, held)).Status
                );
            }

            clock.Advance(TimeSpan.FromMinutes(1));
            await SweepAsync(clock);

            await using var verify = OpenDb();
            var converted = await ReservationAsync(verify, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, converted.Status);
            var code = await verify.Context.QrCodes.SingleAsync(row =>
                row.RestaurantLocationId == held.Workspace.LocationId
                && row.QrType == QrType.GuestCard
            );
            Assert.Equal(QrCodeStatus.Active, code.Status);
            Assert.Equal(code.Id, converted.ConvertedQrCodeId);
        }

        [Fact]
        public async Task Sweep_Completed_NoWebhookYet_ConvertsAndMarksPaid()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "completed");
            var refundsBefore = _factory.Merchant.RefundOrderCallCount;

            await SweepAsync(ClockAfterDeadline(held));

            Assert.Equal(refundsBefore, _factory.Merchant.RefundOrderCallCount);
            await using var db = OpenDb();
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.NotNull(order.PaidAtUtc);
            var reservation = await ReservationAsync(db, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.NotNull(reservation.ConvertedQrCodeId);
            Assert.False((await db.Context.RevolutOrderIntents
                .SingleAsync(row => row.ShopOrderId == held.OrderId)).IsOpen);
            Assert.Equal(1, await db.Context.TummlyVatInvoices
                .CountAsync(row => row.RevolutOrderId == held.RevolutOrderId));
        }

        [Fact]
        public async Task Sweep_ErrorFor24h_MovesToReview_StillCountsAsHeld()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] =
                new RevolutOrderRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error"
                );
            var logger = new CapturingLogger();
            var clock = new AdjustableTimeProvider(
                new DateTimeOffset(held.CreatedAtUtc.AddHours(25), TimeSpan.Zero)
            );

            await SweepAsync(clock, logger);

            await using (var db = OpenDb())
            {
                var reservation = await ReservationAsync(db, held);
                Assert.Equal(ShopQrSlotReservationStatuses.Review, reservation.Status);
                Assert.Equal(1, reservation.ReconcileAttempts);
                Assert.Equal("revolut_http_error", reservation.LastReconcileError);
                // Review still holds the slot: the location stays at its cap.
                var capacity = db.Services.GetRequiredService<IActiveQrCapacity>();
                Assert.Equal(
                    await ShopCapacityTestSeed.CapAsync(db.Context, held.Workspace),
                    await capacity.CountUsedAsync(
                        held.Workspace.LocationId,
                        CancellationToken.None
                    )
                );
            }

            Assert.Contains(
                logger.Entries,
                entry => entry.Level == LogLevel.Warning
                    && entry.Message.Contains(held.OrderId.ToString())
                    && entry.Message.Contains("review")
                    && entry.EventName == "ShopQrReservationReview"
            );

            // A review reservation is not swept again.
            clock.Advance(TimeSpan.FromHours(2));
            await SweepAsync(clock, logger);
            await using var verify = OpenDb();
            var after = await ReservationAsync(verify, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Review, after.Status);
            Assert.Equal(1, after.ReconcileAttempts);
        }

        [Fact]
        public async Task Sweep_AfterPaymentDeclinedClosedIntent_StillAsksRevolutBeforeRelease()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            // A declined payment closes the intent but the Revolut order can
            // still be paid, so the sweeper must read it before releasing.
            var declined = await SendAsync(WebhookRequest(
                "ORDER_PAYMENT_DECLINED",
                held.RevolutOrderId
            ));
            Assert.True(declined.IsSuccessStatusCode);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "completed");

            await SweepAsync(ClockAfterDeadline(held));

            await using var db = OpenDb();
            var reservation = await ReservationAsync(db, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.Equal(
                ShopPaymentStatuses.Paid,
                (await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId))
                    .PaymentStatus
            );
        }

        [Fact]
        public async Task Sweep_OlderIntentPending_NewestFailed_CancelsOlderFirst_ReleasesOnlyWhenConfirmed()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            var newest = await AddSecondIntentAsync(held);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "pending");
            _factory.Merchant.Orders[newest] = Revolut(held, "failed", newest);
            // The first cancel does not take.
            _factory.Merchant.CancelResults[held.RevolutOrderId] =
                new RevolutOrderRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "revolut_http_error"
                );
            _factory.Merchant.CancelledOrderIds.Clear();
            var clock = ClockAfterDeadline(held);

            await SweepAsync(clock);

            // The newest intent is terminal, but the older one can still be paid.
            Assert.Equal(
                new[] { held.RevolutOrderId },
                _factory.Merchant.CancelledOrderIds
            );
            await using (var db = OpenDb())
            {
                var reservation = await ReservationAsync(db, held);
                Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
                Assert.Equal(1, reservation.ReconcileAttempts);
                Assert.Contains("cancel_failed", reservation.LastReconcileError);
            }

            // Now the cancel takes: both attempts are confirmed, so the slot goes.
            _factory.Merchant.CancelResults[held.RevolutOrderId] =
                Revolut(held, "cancelled");
            clock.Advance(TimeSpan.FromMinutes(5));
            await SweepAsync(clock);

            await using var verify = OpenDb();
            Assert.Equal(
                ShopQrSlotReservationStatuses.Released,
                (await ReservationAsync(verify, held)).Status
            );
            Assert.Equal(
                ShopPaymentStatuses.PaymentFailed,
                (await verify.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId))
                    .PaymentStatus
            );
            Assert.False(await verify.Context.RevolutOrderIntents
                .AnyAsync(row => row.ShopOrderId == held.OrderId && row.IsOpen));
        }

        [Fact]
        public async Task Sweep_OlderIntentCompleted_NewestFailed_AppliesPaidAndConverts()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            var newest = await AddSecondIntentAsync(held);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "completed");
            _factory.Merchant.Orders[newest] = Revolut(held, "failed", newest);
            var refundsBefore = _factory.Merchant.RefundOrderCallCount;

            await SweepAsync(ClockAfterDeadline(held));

            Assert.Equal(refundsBefore, _factory.Merchant.RefundOrderCallCount);
            await using var db = OpenDb();
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            Assert.Equal(held.RevolutOrderId, order.RevolutOrderId);
            var reservation = await ReservationAsync(db, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.NotNull(reservation.ConvertedQrCodeId);
        }

        [Fact]
        public async Task Sweep_CompletedAttempt_OrderAlreadyPaidByWebhook_LeftoverOpenRowConvertsNotReleased()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "completed");
            // The webhook pays the order after the sweeper read it, but its row
            // is still open when the sweeper's paid path returns early.
            _factory.Merchant.OnGetOrderAsync = async _ =>
            {
                _factory.Merchant.OnGetOrderAsync = null;
                await MarkPaidWithoutConvertingAsync(held);
            };

            try
            {
                await SweepAsync(ClockAfterDeadline(held));
            }
            finally
            {
                _factory.Merchant.OnGetOrderAsync = null;
            }

            await using var db = OpenDb();
            var reservation = await ReservationAsync(db, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.NotNull(reservation.ConvertedQrCodeId);
            Assert.Equal(
                ShopPaymentStatuses.Paid,
                (await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId))
                    .PaymentStatus
            );
        }

        [Fact]
        public async Task Sweep_OrderBecomesPaidBetweenRevolutReadAndFail_StaysPaid_NotReleased()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "failed");
            _factory.Merchant.OnGetOrderAsync = async _ =>
            {
                _factory.Merchant.OnGetOrderAsync = null;
                await MarkPaidWithoutConvertingAsync(held);
            };

            try
            {
                await SweepAsync(ClockAfterDeadline(held));
            }
            finally
            {
                _factory.Merchant.OnGetOrderAsync = null;
            }

            await using var db = OpenDb();
            var order = await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId);
            Assert.Equal(ShopPaymentStatuses.Paid, order.PaymentStatus);
            var reservation = await ReservationAsync(db, held);
            Assert.NotEqual(ShopQrSlotReservationStatuses.Released, reservation.Status);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
        }

        [Fact]
        public async Task Sweep_CompletedAttempt_ApplierRefuses_RowStaysOpenAndBacksOff()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "completed");
            // The intent is closed but the order is still awaiting payment, a
            // state the applier refuses. The row must not be released.
            await using (var seed = OpenDb())
            {
                (await seed.Context.RevolutOrderIntents
                    .SingleAsync(row => row.ShopOrderId == held.OrderId)).IsOpen = false;
                await seed.Context.SaveChangesAsync();
            }

            var clock = ClockAfterDeadline(held);
            await SweepAsync(clock);

            await using var db = OpenDb();
            var reservation = await ReservationAsync(db, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
            Assert.Equal(1, reservation.ReconcileAttempts);
            Assert.Equal(
                clock.GetUtcNow().UtcDateTime.AddMinutes(5),
                reservation.ReconcileAfterUtc
            );
        }

        [Theory]
        [InlineData("processing", 1)]
        [InlineData("processing", 25)]
        [InlineData("cancelled", 1)]
        public async Task Sweep_ReservationConvertedWhileRevolutIsRead_StaysConverted(
            string revolutState,
            int hoursAfterCreation
        )
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, revolutState);
            var clock = new AdjustableTimeProvider(
                new DateTimeOffset(held.CreatedAtUtc.AddHours(hoursAfterCreation), TimeSpan.Zero)
            );
            // A concurrent apply converts the row after the sweeper read it.
            _factory.Merchant.OnGetOrderAsync = async _ =>
            {
                _factory.Merchant.OnGetOrderAsync = null;
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var row = await db.ShopQrSlotReservations
                    .SingleAsync(r => r.ShopOrderId == held.OrderId);
                row.Status = ShopQrSlotReservationStatuses.Converted;
                row.ConvertedQrCodeId = 4242;
                await db.SaveChangesAsync();
            };

            try
            {
                await SweepAsync(clock);
            }
            finally
            {
                _factory.Merchant.OnGetOrderAsync = null;
            }

            await using var verify = OpenDb();
            var reservation = await ReservationAsync(verify, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Converted, reservation.Status);
            Assert.Equal(4242, reservation.ConvertedQrCodeId);
            Assert.Equal(0, reservation.ReconcileAttempts);
            Assert.Null(reservation.LastReconcileError);
        }

        [Fact]
        public async Task Sweep_NoIntent_Releases()
        {
            var workspace = await SeedAsync(freeSlots: 1);
            Guid orderId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                orderId = await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                    context,
                    workspace,
                    GuestCards
                );
                var created = DateTime.UtcNow.AddMinutes(-31);
                context.ShopQrSlotReservations.Add(new ShopQrSlotReservation
                {
                    Id = Guid.NewGuid(),
                    ShopOrderId = orderId,
                    RestaurantLocationId = workspace.LocationId,
                    QrType = QrType.GuestCard,
                    Status = ShopQrSlotReservationStatuses.Open,
                    CreatedAtUtc = created,
                    UpdatedAtUtc = created,
                    ReconcileAfterUtc = created.AddMinutes(30),
                });
                await context.SaveChangesAsync();
            }

            await SweepAsync(new AdjustableTimeProvider(DateTimeOffset.UtcNow));

            await using var db = OpenDb();
            var reservation = await db.Context.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Released, reservation.Status);
            // No payment can exist, so the order is left awaiting payment.
            Assert.Equal(
                ShopPaymentStatuses.AwaitingPayment,
                (await db.Context.ShopOrders.SingleAsync(row => row.Id == orderId))
                    .PaymentStatus
            );
        }

        [Fact]
        public async Task Sweep_BackoffSequence_5_15_60_60()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "processing");
            var clock = ClockAfterDeadline(held);
            var observed = new List<TimeSpan>();

            for (var attempt = 1; attempt <= 4; attempt++)
            {
                await SweepAsync(clock);
                await using var db = OpenDb();
                var reservation = await ReservationAsync(db, held);
                Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
                Assert.Equal(attempt, reservation.ReconcileAttempts);
                var wait = reservation.ReconcileAfterUtc - clock.GetUtcNow().UtcDateTime;
                observed.Add(wait);
                clock.Advance(wait);
            }

            Assert.Equal(
                new[]
                {
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(15),
                    TimeSpan.FromMinutes(60),
                    TimeSpan.FromMinutes(60),
                },
                observed
            );
        }

        [Fact]
        public async Task Sweep_NotYetDue_IsLeftAlone()
        {
            var held = await PayGuestCardOrderAsync(freeSlots: 1);
            var callsBefore = _factory.Merchant.CancelOrderCallCount;
            _factory.Merchant.Orders[held.RevolutOrderId] = Revolut(held, "pending");

            await SweepAsync(new AdjustableTimeProvider(
                new DateTimeOffset(held.CreatedAtUtc.AddMinutes(10), TimeSpan.Zero)
            ));

            Assert.Equal(callsBefore, _factory.Merchant.CancelOrderCallCount);
            await using var db = OpenDb();
            var reservation = await ReservationAsync(db, held);
            Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
            Assert.Equal(0, reservation.ReconcileAttempts);
        }

        // ---- helpers ----

        private sealed record HeldReservation(
            ShopCapacityWorkspace Workspace,
            Guid OrderId,
            string RevolutOrderId,
            DateTime CreatedAtUtc
        );

        private async Task<ShopCapacityWorkspace> SeedAsync(int freeSlots)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var workspace = await ShopCapacityTestSeed.SeedWorkspaceAsync(
                context,
                scope.ServiceProvider.GetRequiredService<IJwtService>()
            );
            await ShopCapacityTestSeed.MakeOrderableAsync(context, workspace, GuestCards);
            await ShopCapacityTestSeed.LeaveFreeSlotsAsync(context, workspace, freeSlots);
            return workspace;
        }

        /// <summary>
        /// Places and pays a Guest Card order over HTTP: one open reservation,
        /// one open Revolut intent.
        /// </summary>
        private async Task<HeldReservation> PayGuestCardOrderAsync(int freeSlots)
        {
            var workspace = await SeedAsync(freeSlots);
            Guid orderId;
            using (var scope = _factory.Services.CreateScope())
            {
                orderId = await ShopCapacityTestSeed.InsertAwaitingOrderAsync(
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                    workspace,
                    GuestCards
                );
            }

            var response = await _client.SendAsync(
                ShopCapacityTestSeed.PayRequest(workspace, orderId)
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await using var db = OpenDb();
            var revolutOrderId = await db.Context.RevolutOrderIntents
                .Where(row => row.ShopOrderId == orderId)
                .Select(row => row.OrderId)
                .SingleAsync();
            var reservation = await db.Context.ShopQrSlotReservations
                .SingleAsync(row => row.ShopOrderId == orderId);
            Assert.Equal(ShopQrSlotReservationStatuses.Open, reservation.Status);
            return new HeldReservation(
                workspace,
                orderId,
                revolutOrderId,
                reservation.CreatedAtUtc
            );
        }

        /// <summary>
        /// Adds a newer Revolut attempt for the order, as the /pay reuse path
        /// does: the first intent closes and the order is payment_failed.
        /// </summary>
        private async Task<string> AddSecondIntentAsync(HeldReservation held)
        {
            var newest = $"ord_second_{Guid.NewGuid():N}";
            await using var db = OpenDb();
            var first = await db.Context.RevolutOrderIntents
                .SingleAsync(row => row.ShopOrderId == held.OrderId);
            first.IsOpen = false;
            (await db.Context.ShopOrders.SingleAsync(row => row.Id == held.OrderId))
                .PaymentStatus = ShopPaymentStatuses.PaymentFailed;
            db.Context.RevolutOrderIntents.Add(new RevolutOrderIntent
            {
                Id = Guid.NewGuid(),
                OrderId = newest,
                RestaurantId = first.RestaurantId,
                ShopOrderId = held.OrderId,
                Purpose = RevolutOrderIntentPurposes.ShopMaterialsOrder,
                TargetPlan = string.Empty,
                TargetCadence = string.Empty,
                RevolutSubscriptionId = string.Empty,
                CheckoutUrl = FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
                IdempotencyKey = Guid.NewGuid().ToString("D"),
                IsOpen = true,
                NetAmountMinor = first.NetAmountMinor,
                VatAmountMinor = first.VatAmountMinor,
                GrossAmountMinor = first.GrossAmountMinor,
                CreatedAtUtc = first.CreatedAtUtc.AddMinutes(5),
            });
            await db.Context.SaveChangesAsync();
            return newest;
        }

        /// <summary>
        /// A webhook that wrote Paid but whose conversion has not run for this row.
        /// </summary>
        private async Task MarkPaidWithoutConvertingAsync(HeldReservation held)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await db.ShopOrders.SingleAsync(row => row.Id == held.OrderId);
            order.PaymentStatus = ShopPaymentStatuses.Paid;
            order.PaidAtUtc = DateTime.UtcNow;
            order.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
            (await db.RevolutOrderIntents.SingleAsync(row => row.ShopOrderId == held.OrderId))
                .IsOpen = false;
            await db.SaveChangesAsync();
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
            await _client.SendAsync(request);

        private static HttpRequestMessage WebhookRequest(
            string eventName,
            string revolutOrderId
        )
        {
            var body =
                $$"""{"event":"{{eventName}}","order_id":"{{revolutOrderId}}"}""";
            const string timestamp = "1710000000";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/webhooks/revolut"
            )
            {
                Content = new StringContent(
                    body,
                    System.Text.Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.TryAddWithoutValidation(
                "Revolut-Request-Timestamp",
                timestamp
            );
            request.Headers.TryAddWithoutValidation(
                "Revolut-Signature",
                RevolutWebhookSignature.SignForTests("whsec_placeholder", timestamp, body)
            );
            return request;
        }

        private static AdjustableTimeProvider ClockAfterDeadline(HeldReservation held) =>
            new(new DateTimeOffset(held.CreatedAtUtc.AddMinutes(31), TimeSpan.Zero));

        private static RevolutOrderRetrieveResult Revolut(
            HeldReservation held,
            string state,
            string? revolutOrderId = null
        ) => new(
            Succeeded: true,
            Id: revolutOrderId ?? held.RevolutOrderId,
            State: state,
            AmountMinor: 1200,
            CheckoutUrl: FakeFirstPaidRevolutMerchantClient.CheckoutUrl,
            PaymentMethodSummary: "Paid"
        );

        private async Task SweepAsync(
            AdjustableTimeProvider clock,
            ILogger<ShopQrSlotReservationSweeperBackgroundService>? logger = null
        )
        {
            var sweeper = new ShopQrSlotReservationSweeperBackgroundService(
                _factory.Services.GetRequiredService<IServiceScopeFactory>(),
                clock,
                logger ?? new CapturingLogger()
            );
            await sweeper.SweepAsync(CancellationToken.None);
        }

        private static Task<ShopQrSlotReservation> ReservationAsync(
            ScopedDb db,
            HeldReservation held
        ) => db.Context.ShopQrSlotReservations
            .AsNoTracking()
            .SingleAsync(row => row.ShopOrderId == held.OrderId);

        private ScopedDb OpenDb() => new(_factory.Services);

        private sealed class ScopedDb : IAsyncDisposable
        {
            private readonly IServiceScope _scope;

            public ScopedDb(IServiceProvider services)
            {
                _scope = services.CreateScope();
                Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            }

            public ApplicationDbContext Context { get; }

            public IServiceProvider Services => _scope.ServiceProvider;

            public ValueTask DisposeAsync()
            {
                _scope.Dispose();
                return ValueTask.CompletedTask;
            }
        }

        private sealed class CapturingLogger
            : ILogger<ShopQrSlotReservationSweeperBackgroundService>
        {
            public List<(LogLevel Level, string Message, string? EventName)> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            ) => Entries.Add((logLevel, formatter(state, exception), eventId.Name));
        }
    }
}
