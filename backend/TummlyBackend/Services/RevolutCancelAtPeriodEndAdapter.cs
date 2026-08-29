using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Calls Revolut <c>POST …/subscriptions/{id}/cancel</c> when the
    /// included-period job applies ScheduledCancelPlan (ticket 23 / lock 03).
    /// </summary>
    public sealed class RevolutCancelAtPeriodEndAdapter : IRevolutCancelAtPeriodEndAdapter
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;

        public RevolutCancelAtPeriodEndAdapter(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            _context = context;
            _merchant = merchant;
        }

        public async Task CancelNativeSubscriptionAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var subscriptionId =
                await RevolutSubscriptionCorrelation.ResolveLatestSubscriptionIdAsync(
                    _context,
                    restaurantId,
                    cancellationToken
                );
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return;
            }

            var cancel = await _merchant.CancelSubscriptionAsync(
                subscriptionId,
                cancellationToken
            );
            if (!cancel.Succeeded)
            {
                throw new InvalidOperationException(
                    cancel.ErrorCode ?? "revolut_http_error"
                );
            }

            await SyncPendingSessionsAsync(
                restaurantId,
                subscriptionId,
                cancellationToken
            );
        }

        private async Task SyncPendingSessionsAsync(
            int restaurantId,
            string subscriptionId,
            CancellationToken cancellationToken
        )
        {
            var openRows = await _context.RevolutPendingPaySessions
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.RevolutSubscriptionId == subscriptionId
                    && row.IsOpen
                )
                .ToListAsync(cancellationToken);

            if (openRows.Count == 0)
            {
                return;
            }

            foreach (var row in openRows)
            {
                row.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    internal sealed class NullRevolutCancelAtPeriodEndAdapter
        : IRevolutCancelAtPeriodEndAdapter
    {
        public static readonly NullRevolutCancelAtPeriodEndAdapter Instance = new();

        public Task CancelNativeSubscriptionAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }
    }
}
