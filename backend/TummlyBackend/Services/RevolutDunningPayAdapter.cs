using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Day 0 / 3 best-effort merchant Pay on the stored cycle order (ticket 24).
    /// Failed Pay does not write lifecycle day steps.
    /// </summary>
    public sealed class RevolutDunningPayAdapter : IRevolutDunningPayAdapter
    {
        private static readonly HashSet<int> PayDaySteps = [0, 3];

        private static readonly HashSet<string> NonPayableStates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "completed",
                "cancelled",
                "failed",
            };

        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;
        private readonly ILogger<RevolutDunningPayAdapter> _logger;

        public RevolutDunningPayAdapter(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            ILogger<RevolutDunningPayAdapter>? logger = null
        )
        {
            _context = context;
            _merchant = merchant;
            _logger =
                logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RevolutDunningPayAdapter>.Instance;
        }

        public Task HandleDayStepAsync(
            int restaurantId,
            int dayStep,
            CancellationToken cancellationToken = default
        )
        {
            if (!PayDaySteps.Contains(dayStep))
            {
                return Task.CompletedTask;
            }

            return TryPayOutstandingAsync(restaurantId, cancellationToken);
        }

        public async Task TryPayOutstandingAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var account = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account == null || account.DunningEpisodeStartedAt == null)
            {
                return;
            }

            var orderId = account.DunningOutstandingOrderId?.Trim();
            if (string.IsNullOrWhiteSpace(orderId))
            {
                orderId = await RefreshOutstandingOrderIdAsync(
                    account,
                    cancellationToken
                );
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    _logger.LogInformation(
                        "Dunning Pay skipped: no outstanding order for restaurant {RestaurantId}",
                        restaurantId
                    );
                    return;
                }
            }

            var order = await _merchant.GetOrderAsync(orderId, cancellationToken);
            if (!IsPayableCycleOrder(order))
            {
                var refreshed = await RefreshOutstandingOrderIdAsync(
                    account,
                    cancellationToken
                );
                if (
                    string.IsNullOrWhiteSpace(refreshed)
                    || string.Equals(refreshed, orderId, StringComparison.Ordinal)
                )
                {
                    _logger.LogInformation(
                        "Dunning Pay skipped: order {OrderId} not payable for restaurant {RestaurantId}",
                        orderId,
                        restaurantId
                    );
                    return;
                }

                orderId = refreshed;
                order = await _merchant.GetOrderAsync(orderId, cancellationToken);
                if (!IsPayableCycleOrder(order))
                {
                    _logger.LogInformation(
                        "Dunning Pay skipped after refresh: order {OrderId} not payable for restaurant {RestaurantId}",
                        orderId,
                        restaurantId
                    );
                    return;
                }
            }

            var subscriptionId = await ResolveSubscriptionIdAsync(
                restaurantId,
                order.SubscriptionId,
                cancellationToken
            );
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                _logger.LogInformation(
                    "Dunning Pay skipped: no subscription for restaurant {RestaurantId}",
                    restaurantId
                );
                return;
            }

            var subscription = await _merchant.GetSubscriptionAsync(
                subscriptionId,
                cancellationToken
            );
            if (
                !subscription.Succeeded
                || string.IsNullOrWhiteSpace(subscription.PaymentMethodId)
            )
            {
                _logger.LogInformation(
                    "Dunning Pay skipped: no saved payment method for subscription {SubscriptionId}",
                    subscriptionId
                );
                return;
            }

            var pay = await _merchant.PayOrderAsync(
                new RevolutPayOrderRequest(
                    OrderId: orderId,
                    SavedPaymentMethodId: subscription.PaymentMethodId.Trim()
                ),
                cancellationToken
            );
            if (!pay.Succeeded)
            {
                _logger.LogInformation(
                    "Dunning Pay failed for order {OrderId} restaurant {RestaurantId}: {ErrorCode}",
                    orderId,
                    restaurantId,
                    pay.ErrorCode ?? "unknown"
                );
            }
        }

        internal static bool IsPayableCycleOrder(RevolutOrderRetrieveResult order)
        {
            if (!order.Succeeded)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(order.State))
            {
                return true;
            }

            return !NonPayableStates.Contains(order.State.Trim());
        }

        private async Task<string?> RefreshOutstandingOrderIdAsync(
            BillingAccount account,
            CancellationToken cancellationToken
        )
        {
            var subscriptionId = await ResolveSubscriptionIdAsync(
                account.RestaurantId,
                subscriptionIdHint: null,
                cancellationToken
            );
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return null;
            }

            var subscription = await _merchant.GetSubscriptionAsync(
                subscriptionId,
                cancellationToken
            );
            if (
                !subscription.Succeeded
                || string.IsNullOrWhiteSpace(subscription.CurrentCycleId)
            )
            {
                return null;
            }

            var cycle = await _merchant.GetSubscriptionCycleAsync(
                subscriptionId,
                subscription.CurrentCycleId.Trim(),
                cancellationToken
            );
            if (!cycle.Succeeded || string.IsNullOrWhiteSpace(cycle.OrderId))
            {
                return null;
            }

            var orderId = cycle.OrderId.Trim();
            account.DunningOutstandingOrderId = orderId;
            await _context.SaveChangesAsync(cancellationToken);
            return orderId;
        }

        private async Task<string?> ResolveSubscriptionIdAsync(
            int restaurantId,
            string? subscriptionIdHint,
            CancellationToken cancellationToken
        )
        {
            if (!string.IsNullOrWhiteSpace(subscriptionIdHint))
            {
                return subscriptionIdHint.Trim();
            }

            return await _context.RevolutPendingPaySessions
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Select(row => row.RevolutSubscriptionId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
