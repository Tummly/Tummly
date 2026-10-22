using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Schedules Revolut plan variation moves at cycle end for downgrade /
    /// cadence changes (ticket 21). Does not open HPP or PATCH amounts.
    /// </summary>
    public sealed class CycleEndPlanChangeService : ICycleEndPlanChange
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;

        public CycleEndPlanChangeService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            _context = context;
            _merchant = merchant;
        }

        public Task ApplyRevolutChangePlanIfNeededAsync(
            int restaurantId,
            string targetPlan,
            string targetCadenceApi,
            CancellationToken cancellationToken = default
        )
        {
            return ChangePlanAtCycleEndAsync(
                restaurantId,
                RevolutPlanVariationKeys.ForPlanCadence(targetPlan, targetCadenceApi),
                cancellationToken
            );
        }

        public Task RestoreLiveRevolutPlanIfNeededAsync(
            int restaurantId,
            string livePlan,
            string liveCadenceApi,
            int paidExtraLocationCount,
            CancellationToken cancellationToken = default
        )
        {
            var lookupKey =
                string.Equals(
                    livePlan,
                    BillingSubscriptionPlans.Group,
                    StringComparison.OrdinalIgnoreCase
                )
                && paidExtraLocationCount > 0
                    ? RevolutPlanVariationKeys.ForExtraLocation(liveCadenceApi)
                    : RevolutPlanVariationKeys.ForPlanCadence(livePlan, liveCadenceApi);
            return ChangePlanAtCycleEndAsync(
                restaurantId,
                lookupKey,
                cancellationToken,
                skipWhenNothingScheduled: true
            );
        }

        private async Task ChangePlanAtCycleEndAsync(
            int restaurantId,
            string? lookupKey,
            CancellationToken cancellationToken,
            bool skipWhenNothingScheduled = false
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

            if (string.IsNullOrWhiteSpace(lookupKey))
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            if (skipWhenNothingScheduled)
            {
                // No pending Revolut action: the subscription already bills the
                // live plan, so a restore change-plan is not needed. A failed
                // read falls through to the change-plan call.
                var live = await _merchant.GetSubscriptionAsync(
                    subscriptionId,
                    cancellationToken
                );
                if (live.Succeeded && live.HasScheduledAction == false)
                {
                    return;
                }
            }

            try
            {
                var result = await _merchant.ChangeSubscriptionPlanAsync(
                    subscriptionId,
                    lookupKey,
                    cancellationToken
                );
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        result.ErrorCode ?? "revolut_http_error"
                    );
                }
            }
            catch (RevolutMerchantNotReadyException ex)
            {
                throw new InvalidOperationException(ex.Code);
            }
        }
    }
}
