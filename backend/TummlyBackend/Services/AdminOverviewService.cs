using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Operational counts for the staff Overview tab. Counts only, never guest
    /// rows. Each count is read on its own, so one failed query shows as
    /// unavailable and the other counts still load.
    /// </summary>
    public sealed class AdminOverviewService : IAdminOverviewService
    {
        private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(7);

        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;

        public AdminOverviewService(
            ApplicationDbContext context,
            TimeProvider clock
        )
        {
            _context = context;
            _clock = clock;
        }

        public async Task<AdminOverviewDto> GetAsync(
            CancellationToken cancellationToken = default
        )
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            var recentStart = now - RecentWindow;
            var pilotEndsBefore = now + RecentWindow;

            // Queries run one after another: a DbContext cannot run them in parallel.
            return new AdminOverviewDto
            {
                GeneratedAtUtc = now,
                WorkspacesByBillingStatus = await ReadMetricAsync(
                    () => WorkspacesByBillingStatusAsync(cancellationToken),
                    cancellationToken
                ),
                ShopOrdersPaidNotDispatched = await ReadCountAsync(
                    () => _context.ShopOrders.AsNoTracking().CountAsync(order =>
                        order.PaymentStatus == ShopPaymentStatuses.Paid
                        && order.DispatchedAtUtc == null
                        && (order.FulfilmentStatus == null
                            || order.FulfilmentStatus != ShopFulfilmentStatuses.Cancelled),
                        cancellationToken
                    ),
                    cancellationToken
                ),
                ShopOrdersFlaggedForReview = await ReadCountAsync(
                    () => _context.ShopOrders.AsNoTracking().CountAsync(order =>
                        order.ReviewReason != null
                        && (order.FulfilmentStatus == null
                            || order.FulfilmentStatus != ShopFulfilmentStatuses.Cancelled),
                        cancellationToken
                    ),
                    cancellationToken
                ),
                PrintAssetsFailed = await ReadCountAsync(
                    () => _context.PrintReadyQrAssets.AsNoTracking().CountAsync(
                        asset => asset.Status == PrintReadyQrAssetStatus.Failed,
                        cancellationToken
                    ),
                    cancellationToken
                ),
                PilotsEndingWithin7Days = await ReadCountAsync(
                    () => _context.BillingAccounts.AsNoTracking().CountAsync(account =>
                        account.SubscriptionPlan == BillingSubscriptionPlans.Pilot
                        && account.PilotPeriodEnd != null
                        && account.PilotPeriodEnd >= now
                        && account.PilotPeriodEnd <= pilotEndsBefore,
                        cancellationToken
                    ),
                    cancellationToken
                ),
                OpenHelpCentreQueries = await ReadCountAsync(
                    () => _context.HelpCentreQueries.AsNoTracking().CountAsync(query =>
                        query.Status == HelpCentreQueryStatus.New
                        || query.Status == HelpCentreQueryStatus.InProgress
                        || query.Status == HelpCentreQueryStatus.WaitingOnCustomer
                        || query.Status == HelpCentreQueryStatus.EscalatedToAdmin,
                        cancellationToken
                    ),
                    cancellationToken
                ),
                FailedAuditEventsLast7Days = await ReadCountAsync(
                    () => _context.AdminAuditEvents.AsNoTracking().CountAsync(entry =>
                        !entry.Succeeded && entry.OccurredAtUtc >= recentStart,
                        cancellationToken
                    ),
                    cancellationToken
                ),
            };
        }

        public static Task<AdminOverviewMetricDto<int?>> ReadCountAsync(
            Func<Task<int>> read,
            CancellationToken cancellationToken
        ) => ReadMetricAsync<int?>(async () => await read(), cancellationToken);

        /// <summary>
        /// Runs one count. A failed read returns Available = false with no
        /// value. Cancellation is not a failed read, so it propagates.
        /// </summary>
        public static async Task<AdminOverviewMetricDto<T>> ReadMetricAsync<T>(
            Func<Task<T>> read,
            CancellationToken cancellationToken
        )
        {
            try
            {
                return new AdminOverviewMetricDto<T>
                {
                    Available = true,
                    Value = await read(),
                };
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return new AdminOverviewMetricDto<T> { Available = false };
            }
        }

        private async Task<IReadOnlyList<AdminOverviewBillingStatusCountDto>> WorkspacesByBillingStatusAsync(
            CancellationToken cancellationToken
        )
        {
            return await _context.BillingAccounts
                .AsNoTracking()
                .GroupBy(account => account.BillingStatus)
                .Select(group => new AdminOverviewBillingStatusCountDto
                {
                    BillingStatus = group.Key,
                    Count = group.Count(),
                })
                .ToListAsync(cancellationToken);
        }
    }
}
