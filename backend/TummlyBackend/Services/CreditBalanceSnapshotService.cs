using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class CreditBalanceSnapshotService : ICreditBalanceSnapshot
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;

        public CreditBalanceSnapshotService(
            ApplicationDbContext context,
            TimeProvider clock
        )
        {
            _context = context;
            _clock = clock;
        }

        public async Task<CreditBalanceAccountSnapshot?> GetAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var restaurantExists = await _context.Restaurants.AnyAsync(
                row => row.Id == restaurantId,
                cancellationToken
            );
            if (!restaurantExists)
            {
                return null;
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                throw new InvalidOperationException(
                    $"Billing Account is missing for restaurant {restaurantId}."
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var entries = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .ToListAsync(cancellationToken);

            var isPilot = string.Equals(
                billingAccount.SubscriptionPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.Ordinal
            );

            return new CreditBalanceAccountSnapshot
            {
                IsPilot = isPilot,
                StarterKitState = billingAccount.StarterKitState,
                PeriodLabel = BuildPeriodLabel(isPilot, entries, now),
                Channels = CreditChannels.All
                    .Select(channel => MapChannel(
                        channel,
                        entries.Where(row => row.Channel == channel).ToList(),
                        now,
                        isPilot
                    ))
                    .ToList(),
            };
        }

        private static CreditBalanceChannelSnapshot MapChannel(
            string channel,
            IReadOnlyList<CreditLedgerEntry> entries,
            DateTime nowUtc,
            bool isPilot
        )
        {
            var states = CreditLedgerCalculator.Project(entries, nowUtc);
            var remaining = CreditLedgerCalculator.PoolAvailable(states);
            var held = states.Sum(row => row.Held);
            var used = UsedThisCycle(entries, states, nowUtc, isPilot);
            var included = IncludedThisPeriod(states, nowUtc, isPilot);
            var purchased = states
                .Where(row => CreditLedgerCalculator.IsTopUpClass(row.EntryType))
                .ToList();
            var purchasedRemaining = purchased.Sum(row => Math.Max(row.Bindable, 0));
            var livePurchased = purchased
                .Where(row => row.Bindable > 0 && row.ExpiresAtUtc != null)
                .ToList();
            DateTime? earliestExpiry = livePurchased.Count == 0
                ? null
                : livePurchased.Min(row => row.ExpiresAtUtc);
            var usedShare = remaining <= 0
                ? 1m
                : (decimal)used / (used + remaining);

            return new CreditBalanceChannelSnapshot
            {
                Channel = channel,
                Remaining = remaining,
                Held = held,
                UsedThisCycle = used,
                IncludedThisPeriod = included,
                PurchasedRemaining = purchasedRemaining,
                EarliestPurchasedExpiryUtc = earliestExpiry,
                UsedShare = usedShare,
            };
        }

        private static int UsedThisCycle(
            IReadOnlyList<CreditLedgerEntry> entries,
            IReadOnlyList<CreditAllocationState> states,
            DateTime nowUtc,
            bool isPilot
        )
        {
            var consumptions = entries.Where(row =>
                row.EntryType == CreditLedgerEntryTypes.Consumption
            );
            if (isPilot)
            {
                var pilotStart = entries
                    .Where(row =>
                        row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                    )
                    .Select(row => (DateTime?)row.CreatedAtUtc)
                    .Min();
                if (pilotStart == null)
                {
                    return 0;
                }

                return consumptions
                    .Where(row => row.CreatedAtUtc >= pilotStart.Value)
                    .Sum(row => row.Quantity);
            }

            var period = OpenIncludedPeriod(states, nowUtc);
            if (period == null)
            {
                return 0;
            }

            return consumptions
                .Where(row =>
                    row.CreatedAtUtc >= period.Value.Start
                    && row.CreatedAtUtc < period.Value.End
                )
                .Sum(row => row.Quantity);
        }

        private static int IncludedThisPeriod(
            IReadOnlyList<CreditAllocationState> states,
            DateTime nowUtc,
            bool isPilot
        )
        {
            if (isPilot)
            {
                return states
                    .Where(row =>
                        row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                    )
                    .Sum(row => row.GrantQuantity);
            }

            var period = OpenIncludedPeriod(states, nowUtc);
            if (period == null)
            {
                return 0;
            }

            return states
                .Where(row =>
                    CreditLedgerCalculator.IsIncludedClass(row.EntryType)
                    && row.PeriodStartUtc == period.Value.Start
                    && row.ExpiresAtUtc == period.Value.End
                )
                .Sum(row => row.GrantQuantity);
        }

        private static (DateTime Start, DateTime End)? OpenIncludedPeriod(
            IReadOnlyList<CreditAllocationState> states,
            DateTime nowUtc
        )
        {
            var open = states
                .Where(row =>
                    CreditLedgerCalculator.IsIncludedClass(row.EntryType)
                    && row.PeriodStartUtc != null
                    && row.ExpiresAtUtc != null
                    && row.PeriodStartUtc.Value <= nowUtc
                    && nowUtc < row.ExpiresAtUtc.Value
                )
                .OrderByDescending(row => row.PeriodStartUtc)
                .FirstOrDefault();

            if (open.PeriodStartUtc == null || open.ExpiresAtUtc == null)
            {
                return null;
            }

            return (open.PeriodStartUtc.Value, open.ExpiresAtUtc.Value);
        }

        private static string BuildPeriodLabel(
            bool isPilot,
            IReadOnlyList<CreditLedgerEntry> entries,
            DateTime nowUtc
        )
        {
            if (isPilot)
            {
                return "Account · Pilot allowance";
            }

            var states = CreditLedgerCalculator.Project(entries, nowUtc);
            var period = OpenIncludedPeriod(states, nowUtc);
            if (period == null)
            {
                return "Current period";
            }

            return $"{UkDateLabels.Format(period.Value.Start)} – {UkDateLabels.Format(period.Value.End)}";
        }
    }
}
