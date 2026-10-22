using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Schedules Revolut subscription cancel at cycle end when the operator
    /// confirms cancel plan (ticket 23).
    /// </summary>
    public sealed class CycleEndPlanCancelService : ICycleEndPlanCancel
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;

        public CycleEndPlanCancelService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant
        )
        {
            _context = context;
            _merchant = merchant;
        }

        public async Task ApplyRevolutCancelAtCycleEndIfNeededAsync(
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

            try
            {
                var result = await _merchant.ScheduleSubscriptionCancelAtCycleEndAsync(
                    subscriptionId,
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
