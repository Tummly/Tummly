using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class IncludedPeriodJob : IIncludedPeriodJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IIncludedPeriodMintService _mint;

        public IncludedPeriodJob(
            ApplicationDbContext context,
            IIncludedPeriodMintService mint
        )
        {
            _context = context;
            _mint = mint;
        }

        public async Task<IncludedPeriodJobResult> ProcessAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            // Paid accounts (any cadence): catch-up expiry. Annual Active also mints
            // the current open slice inside ProcessJobForRestaurantAsync.
            var restaurantIds = await _context.BillingAccounts
                .AsNoTracking()
                .Where(row => row.BillingCycle != null)
                .Select(row => row.RestaurantId)
                .ToListAsync(cancellationToken);

            var minted = 0;
            var failed = 0;
            foreach (var restaurantId in restaurantIds)
            {
                try
                {
                    var result = await _mint.ProcessJobForRestaurantAsync(
                        restaurantId,
                        nowUtc,
                        cancellationToken
                    );
                    if (result.InsertedAllocationIds.Count > 0)
                    {
                        minted++;
                    }
                }
                catch
                {
                    failed++;
                }
            }

            return new IncludedPeriodJobResult
            {
                Processed = restaurantIds.Count,
                Minted = minted,
                Failed = failed,
            };
        }
    }
}
