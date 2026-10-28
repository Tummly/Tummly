using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AdminOverviewServiceTests
    {
        private static readonly DateTime Now = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task ReadMetricAsync_WhenQueryThrows_ReturnsUnavailableWithNoValue()
        {
            var metric = await AdminOverviewService.ReadMetricAsync<int?>(
                () => throw new InvalidOperationException("database unavailable"),
                CancellationToken.None
            );

            Assert.False(metric.Available);
            Assert.Null(metric.Value);
        }

        [Fact]
        public async Task ReadMetricAsync_WhenQuerySucceeds_ReturnsAvailableValue()
        {
            var metric = await AdminOverviewService.ReadMetricAsync<int?>(
                () => Task.FromResult<int?>(3),
                CancellationToken.None
            );

            Assert.True(metric.Available);
            Assert.Equal(3, metric.Value);
        }

        [Fact]
        public async Task ReadMetricAsync_WhenCancelled_RethrowsInsteadOfMarkingUnavailable()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                AdminOverviewService.ReadMetricAsync<int?>(
                    () => throw new OperationCanceledException(cts.Token),
                    cts.Token
                )
            );
        }

        [Fact]
        public async Task GetAsync_CountsEachQueue_AndMarksEveryMetricAvailable()
        {
            var db = NewContext();
            SeedOperationalRows(db);
            await db.SaveChangesAsync();

            var overview = await CreateService(db).GetAsync();

            Assert.Equal(Now, overview.GeneratedAtUtc);

            Assert.True(overview.WorkspacesByBillingStatus.Available);
            Assert.Equal(
                4,
                overview.WorkspacesByBillingStatus.Value!
                    .Single(row => row.BillingStatus == BillingStatuses.Free)
                    .Count
            );

            Assert.True(overview.ShopOrdersPaidNotDispatched.Available);
            Assert.Equal(3, overview.ShopOrdersPaidNotDispatched.Value);

            Assert.True(overview.ShopOrdersFlaggedForReview.Available);
            Assert.Equal(1, overview.ShopOrdersFlaggedForReview.Value);

            Assert.True(overview.PrintAssetsFailed.Available);
            Assert.Equal(2, overview.PrintAssetsFailed.Value);

            Assert.True(overview.PilotsEndingWithin7Days.Available);
            Assert.Equal(1, overview.PilotsEndingWithin7Days.Value);

            Assert.True(overview.OpenHelpCentreQueries.Available);
            Assert.Equal(2, overview.OpenHelpCentreQueries.Value);

            Assert.True(overview.FailedAuditEventsLast7Days.Available);
            Assert.Equal(1, overview.FailedAuditEventsLast7Days.Value);
        }

        private static void SeedOperationalRows(ApplicationDbContext db)
        {
            db.BillingAccounts.AddRange(
                Billing(1, BillingSubscriptionPlans.Pilot, Now.AddDays(3)),
                Billing(2, BillingSubscriptionPlans.Pilot, Now.AddDays(10)),
                Billing(3, BillingSubscriptionPlans.Starter, Now.AddDays(3)),
                Billing(4, BillingSubscriptionPlans.Pilot, Now.AddDays(-2))
            );

            db.ShopOrders.AddRange(
                Order(ShopPaymentStatuses.Paid, dispatched: false, fulfilment: ShopFulfilmentStatuses.Processing),
                Order(ShopPaymentStatuses.Paid, dispatched: true, fulfilment: ShopFulfilmentStatuses.InTransit),
                Order(ShopPaymentStatuses.Paid, dispatched: false, fulfilment: ShopFulfilmentStatuses.Cancelled),
                Order(ShopPaymentStatuses.Paid, dispatched: false, fulfilment: null),
                Order(ShopPaymentStatuses.Paid, dispatched: false, fulfilment: ShopFulfilmentStatuses.Processing, reviewReason: ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment),
                Order(ShopPaymentStatuses.Paid, dispatched: false, fulfilment: ShopFulfilmentStatuses.Cancelled, reviewReason: ShopOrderReviewReasons.QrCapacityUnavailable),
                // Resolved review orders: delivered, or refunded. Not counted.
                Order(ShopPaymentStatuses.Paid, dispatched: true, fulfilment: ShopFulfilmentStatuses.Delivered, reviewReason: ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment),
                Order(ShopPaymentStatuses.Refunded, dispatched: false, fulfilment: ShopFulfilmentStatuses.Processing, reviewReason: ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment)
            );

            db.PrintReadyQrAssets.AddRange(
                Asset(PrintReadyQrAssetStatus.Failed),
                Asset(PrintReadyQrAssetStatus.Failed),
                Asset(PrintReadyQrAssetStatus.Ready)
            );

            db.HelpCentreQueries.AddRange(
                Query(HelpCentreQueryStatus.New),
                Query(HelpCentreQueryStatus.InProgress),
                Query(HelpCentreQueryStatus.Resolved)
            );

            db.AdminAuditEvents.AddRange(
                Audit(succeeded: false, Now.AddDays(-1)),
                Audit(succeeded: false, Now.AddDays(-8)),
                Audit(succeeded: true, Now.AddDays(-1))
            );
        }

        private static AdminOverviewService CreateService(ApplicationDbContext db) =>
            new(db, new FixedTimeProvider(Now));

        private static ApplicationDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private static BillingAccount Billing(int restaurantId, string plan, DateTime pilotEnd) =>
            new()
            {
                RestaurantId = restaurantId,
                SubscriptionPlan = plan,
                PilotPeriodEnd = pilotEnd,
            };

        private static ShopOrder Order(
            string paymentStatus,
            bool dispatched,
            string? fulfilment,
            string? reviewReason = null
        ) =>
            new()
            {
                Id = Guid.NewGuid(),
                PaymentStatus = paymentStatus,
                DispatchedAtUtc = dispatched ? Now.AddDays(-1) : null,
                FulfilmentStatus = fulfilment,
                ReviewReason = reviewReason,
            };

        private static PrintReadyQrAsset Asset(PrintReadyQrAssetStatus status) =>
            new()
            {
                RestaurantLocationId = 1,
                QrType = QrType.CounterCard,
                Status = status,
            };

        private static HelpCentreQuery Query(HelpCentreQueryStatus status) =>
            new()
            {
                SubmitterName = "Owner",
                SubmitterEmail = "owner@example.com",
                BusinessName = "Cafe",
                Status = status,
            };

        private static AdminAuditEvent Audit(bool succeeded, DateTime occurredAt) =>
            new()
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = occurredAt,
                Action = AdminAuditActions.PaymentRefund,
                ActorIdentity = "admin@tummly.com",
                TargetType = AdminAuditTargetTypes.PaymentOrder,
                TargetId = "order-1",
                Succeeded = succeeded,
            };

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTime _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = utcNow;
            }

            public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
        }
    }
}
