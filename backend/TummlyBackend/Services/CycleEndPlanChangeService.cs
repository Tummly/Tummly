using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

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

        public async Task ApplyRevolutChangePlanIfNeededAsync(
            int restaurantId,
            string targetPlan,
            string targetCadenceApi,
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

            var lookupKey = RevolutPlanVariationKeys.ForPlanCadence(
                targetPlan,
                targetCadenceApi
            );
            if (string.IsNullOrWhiteSpace(lookupKey))
            {
                throw new InvalidOperationException("invalid_plan_target");
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
