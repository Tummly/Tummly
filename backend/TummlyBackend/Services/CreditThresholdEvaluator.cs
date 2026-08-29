using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class CreditThresholdEvaluator : ICreditThresholdEvaluator
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;
        private readonly IBillingAccountNoticeNotifier _notifier;
        private readonly ILogger<CreditThresholdEvaluator> _logger;

        public CreditThresholdEvaluator(
            ApplicationDbContext context,
            TimeProvider clock,
            IBillingAccountNoticeNotifier notifier,
            ILogger<CreditThresholdEvaluator>? logger = null
        )
        {
            _context = context;
            _clock = clock;
            _notifier = notifier;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    CreditThresholdEvaluator
                >.Instance;
        }

        public async Task<CreditThresholdApplyResult> ApplyInTransactionAsync(
            int restaurantId,
            string channel,
            IReadOnlyList<CreditLedgerEntry> channelEntriesIncludingPending,
            CancellationToken cancellationToken = default
        )
        {
            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                return new CreditThresholdApplyResult();
            }

            var isPilot = string.Equals(
                billingAccount.SubscriptionPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.Ordinal
            );
            var now = _clock.GetUtcNow().UtcDateTime;
            var states = CreditLedgerCalculator.Project(
                channelEntriesIncludingPending,
                now
            );
            var remaining = CreditLedgerCalculator.PoolAvailable(states);
            var used = UsedThisCycle(
                channelEntriesIncludingPending,
                states,
                now,
                isPilot
            );
            var usedShare = CreditThresholdMath.UsedShare(used, remaining);
            var targetBand = CreditThresholdMath.TargetBand(usedShare, remaining);
            var periodKey = ResolvePeriodKey(
                channelEntriesIncludingPending,
                states,
                now,
                isPilot
            );

            var watermark = await _context.CreditWarningStates
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.Channel == channel,
                    cancellationToken
                );
            if (watermark == null)
            {
                watermark = new CreditWarningState
                {
                    RestaurantId = restaurantId,
                    Channel = channel,
                    HighestBandThisPeriod = CreditThresholdBands.None,
                };
                _context.CreditWarningStates.Add(watermark);
            }

            var bandsToEmit = CreditThresholdMath.BandsToEmit(
                watermark.HighestBandThisPeriod,
                targetBand,
                remaining,
                used
            );
            watermark.HighestBandThisPeriod = Math.Max(
                watermark.HighestBandThisPeriod,
                targetBand
            );

            var suppressNotify = BillingAccountNoticeNotifier.IsCreditAlertSuppressed(
                billingAccount.BillingStatus
            );
            var pending = bandsToEmit
                .Select(band => new CreditThresholdPendingNotification
                {
                    RestaurantId = restaurantId,
                    Channel = channel,
                    Band = band,
                    PeriodKey = periodKey,
                    BillingStatus = billingAccount.BillingStatus,
                    IsPilot = isPilot,
                    SuppressNotify = suppressNotify,
                    UsedShare = usedShare,
                    Remaining = remaining,
                    Used = used,
                })
                .ToList();

            return new CreditThresholdApplyResult { Pending = pending };
        }

        public async Task NotifyAfterCommitAsync(
            CreditThresholdApplyResult applyResult,
            CancellationToken cancellationToken = default
        )
        {
            foreach (var pending in applyResult.Pending)
            {
                if (pending.SuppressNotify)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await _notifier.NotifyCreditThresholdCrossedAsync(
                        pending.RestaurantId,
                        pending.Channel,
                        pending.Band,
                        pending.PeriodKey,
                        pending.BillingStatus,
                        pending.IsPilot,
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to notify credit threshold {Band} for restaurant {RestaurantId} channel {Channel}",
                        pending.Band,
                        pending.RestaurantId,
                        pending.Channel
                    );
                }
            }
        }

        public async Task ResetBandsForIncludedPeriodMintAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.CreditWarningStates
                .Where(row => row.RestaurantId == restaurantId)
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                row.HighestBandThisPeriod = CreditThresholdBands.None;
            }
        }

        internal static string ResolvePeriodKey(
            IReadOnlyList<CreditLedgerEntry> entries,
            IReadOnlyList<CreditAllocationState> states,
            DateTime nowUtc,
            bool isPilot
        )
        {
            if (isPilot)
            {
                return "pilot-once";
            }

            var period = OpenIncludedPeriod(states, nowUtc);
            if (period == null)
            {
                return "included-none";
            }

            return $"{period.Value.Start:yyyyMMdd}-{period.Value.End:yyyyMMdd}";
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
    }

    internal sealed class NullCreditThresholdEvaluator : ICreditThresholdEvaluator
    {
        public static readonly NullCreditThresholdEvaluator Instance = new();

        public Task<CreditThresholdApplyResult> ApplyInTransactionAsync(
            int restaurantId,
            string channel,
            IReadOnlyList<CreditLedgerEntry> channelEntriesIncludingPending,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(new CreditThresholdApplyResult());
        }

        public Task NotifyAfterCommitAsync(
            CreditThresholdApplyResult applyResult,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }

        public Task ResetBandsForIncludedPeriodMintAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }
    }
}
