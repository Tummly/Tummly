using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class IncludedPeriodMintService : IIncludedPeriodMintService
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> AccountLocks
            = new();

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;
        private readonly TimeProvider _clock;
        private readonly ICreditThresholdEvaluator _thresholdEvaluator;
        private readonly IPlanChangeService _planChange;

        public IncludedPeriodMintService(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            TimeProvider clock,
            ICreditThresholdEvaluator? thresholdEvaluator = null,
            IPlanChangeService? planChange = null
        )
        {
            _context = context;
            _pricebook = pricebook;
            _clock = clock;
            _thresholdEvaluator =
                thresholdEvaluator ?? NullCreditThresholdEvaluator.Instance;
            _planChange = planChange ?? NullPlanChangeService.Instance;
        }

        public Task<IncludedPeriodMintResult> MintOnOrderCompletedAsync(
            IncludedPeriodOrderCompletedRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (!request.PaymentCompleted)
            {
                return Task.FromResult(
                    IncludedPeriodMintResult.Skipped("payment_not_completed")
                );
            }

            return RunLockedAsync(
                request.RestaurantId,
                async (billingAccount, entries, nowUtc, session) =>
                {
                    if (!CanMintIncluded(billingAccount))
                    {
                        return await AbortOwnedAsync(
                            session,
                            IncludedPeriodMintResult.Skipped(
                                "billing_status_not_active"
                            ),
                            cancellationToken
                        );
                    }

                    var scheduleResult =
                        await _planChange.ApplyScheduledChangeOnRenewalAsync(
                            billingAccount,
                            cancellationToken
                        );
                    if (scheduleResult == ScheduledChangeApplyResult.GateFailed)
                    {
                        return await AbortOwnedAsync(
                            session,
                            IncludedPeriodMintResult.Skipped(
                                "scheduled_change_gate_failed"
                            ),
                            cancellationToken
                        );
                    }

                    var period = ResolveOrderCompletedPeriod(
                        billingAccount,
                        request
                    );
                    if (period == null)
                    {
                        return await AbortOwnedAsync(
                            session,
                            IncludedPeriodMintResult.Skipped("invalid_period"),
                            cancellationToken
                        );
                    }

                    return await MintPeriodAsync(
                        billingAccount,
                        entries,
                        nowUtc,
                        period.Value.PeriodStartUtc,
                        period.Value.ExpiresAtUtc,
                        session,
                        cancellationToken
                    );
                },
                cancellationToken
            );
        }

        public Task<IncludedPeriodMintResult> ProcessJobForRestaurantAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return ProcessJobForRestaurantAsync(
                restaurantId,
                nowUtc: null,
                cancellationToken
            );
        }

        public Task<IncludedPeriodMintResult> ProcessJobForRestaurantAsync(
            int restaurantId,
            DateTime? nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            return RunLockedAsync(
                restaurantId,
                async (billingAccount, entries, clockNowUtc, session) =>
                {
                    var effectiveNow = nowUtc ?? clockNowUtc;
                    var expiryRowsWritten = WriteEndedIncludedExpiries(
                        entries,
                        effectiveNow
                    );

                    if (
                        billingAccount.ScheduledCancelPlan
                        && billingAccount.RenewalDateUtc != null
                        && effectiveNow >= billingAccount.RenewalDateUtc.Value
                    )
                    {
                        billingAccount.ClearScheduledChangeSlot();
                        return await FinishCancelApplyAsync(
                            expiryRowsWritten,
                            session,
                            cancellationToken
                        );
                    }

                    if (
                        !string.Equals(
                            billingAccount.BillingCycle,
                            BillingCycles.Annual,
                            StringComparison.Ordinal
                        )
                        || !string.Equals(
                            billingAccount.BillingStatus,
                            BillingStatuses.Active,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        return await FinishWithoutMintAsync(
                            expiryRowsWritten,
                            session,
                            cancellationToken
                        );
                    }

                    var periodStarts = entries
                        .Where(row =>
                            row.EntryType
                                == CreditLedgerEntryTypes.IncludedAllocation
                            && row.PeriodStartUtc != null
                        )
                        .Select(row => row.PeriodStartUtc!.Value);
                    var yearStartUtc = IncludedPeriodCalculator.InferAnnualYearStart(
                        periodStarts,
                        effectiveNow
                    );
                    if (yearStartUtc == null)
                    {
                        return await FinishWithoutMintAsync(
                            expiryRowsWritten,
                            session,
                            cancellationToken
                        );
                    }

                    var yearEndUtc = ResolveAnnualYearEndUtc(
                        yearStartUtc.Value,
                        entries
                    );
                    var sliceIndex = IncludedPeriodCalculator.CurrentAnnualSliceIndex(
                        yearStartUtc.Value,
                        yearEndUtc,
                        effectiveNow
                    );
                    if (sliceIndex is null or 0)
                    {
                        return await FinishWithoutMintAsync(
                            expiryRowsWritten,
                            session,
                            cancellationToken
                        );
                    }

                    var slice = IncludedPeriodCalculator.ResolveAnnualSlice(
                        yearStartUtc.Value,
                        yearEndUtc,
                        sliceIndex.Value
                    );
                    if (slice == null)
                    {
                        return await FinishWithoutMintAsync(
                            expiryRowsWritten,
                            session,
                            cancellationToken
                        );
                    }

                    return await MintPeriodAsync(
                        billingAccount,
                        entries,
                        effectiveNow,
                        slice.Value.PeriodStartUtc,
                        slice.Value.ExpiresAtUtc,
                        session,
                        cancellationToken,
                        expiryRowsWritten
                    );
                },
                cancellationToken
            );
        }

        private async Task<IncludedPeriodMintResult> RunLockedAsync(
            int restaurantId,
            Func<
                BillingAccount,
                List<CreditLedgerEntry>,
                DateTime,
                LockSession,
                Task<IncludedPeriodMintResult>
            > action,
            CancellationToken cancellationToken
        )
        {
            if (!_context.Database.IsSqlServer())
            {
                var gate = AccountLocks.GetOrAdd(
                    restaurantId,
                    _ => new SemaphoreSlim(1, 1)
                );
                await gate.WaitAsync(cancellationToken);
                try
                {
                    return await RunLockedCoreAsync(
                        restaurantId,
                        action,
                        cancellationToken
                    );
                }
                finally
                {
                    gate.Release();
                }
            }

            return await RunLockedCoreAsync(
                restaurantId,
                action,
                cancellationToken
            );
        }

        private async Task<IncludedPeriodMintResult> RunLockedCoreAsync(
            int restaurantId,
            Func<
                BillingAccount,
                List<CreditLedgerEntry>,
                DateTime,
                LockSession,
                Task<IncludedPeriodMintResult>
            > action,
            CancellationToken cancellationToken
        )
        {
            var ambient = _context.Database.CurrentTransaction;
            var ownsTransaction = ambient == null;
            IDbContextTransaction? owned = null;
            if (ownsTransaction)
            {
                owned = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken
                );
            }

            var session = new LockSession(ownsTransaction, owned ?? ambient!);

            try
            {
                var billingAccount = await LockBillingAccountAsync(
                    restaurantId,
                    cancellationToken
                );
                if (billingAccount == null)
                {
                    return await AbortOwnedAsync(
                        session,
                        IncludedPeriodMintResult.Skipped("billing_account_missing"),
                        cancellationToken
                    );
                }

                var nowUtc = _clock.GetUtcNow().UtcDateTime;
                var entries = await _context.CreditLedgerEntries
                    .Where(row => row.RestaurantId == restaurantId)
                    .ToListAsync(cancellationToken);

                return await action(
                    billingAccount,
                    entries,
                    nowUtc,
                    session
                );
            }
            catch
            {
                if (ownsTransaction && owned != null)
                {
                    await owned.RollbackAsync(cancellationToken);
                    await owned.DisposeAsync();
                }

                throw;
            }
        }

        private async Task<IncludedPeriodMintResult> MintPeriodAsync(
            BillingAccount billingAccount,
            List<CreditLedgerEntry> entries,
            DateTime nowUtc,
            DateTime periodStartUtc,
            DateTime expiresAtUtc,
            LockSession session,
            CancellationToken cancellationToken,
            int expiryRowsWritten = 0
        )
        {
            if (expiresAtUtc <= nowUtc)
            {
                return await AbortOwnedAsync(
                    session,
                    IncludedPeriodMintResult.Skipped("period_already_ended"),
                    cancellationToken
                );
            }

            if (periodStartUtc > nowUtc)
            {
                return await AbortOwnedAsync(
                    session,
                    IncludedPeriodMintResult.Skipped("future_period"),
                    cancellationToken
                );
            }

            expiryRowsWritten += WriteEndedIncludedExpiries(entries, nowUtc);

            var pricebookId = ResolveMintPricebookId(billingAccount, entries);
            var pricebook = _pricebook.GetRequired(pricebookId);
            var planKey = SubscriptionPlanKey(billingAccount.SubscriptionPlan);
            if (!pricebook.Plans.TryGetValue(planKey, out var plan))
            {
                return await AbortOwnedAsync(
                    session,
                    IncludedPeriodMintResult.Skipped("plan_not_in_pricebook"),
                    cancellationToken
                );
            }

            if (plan.CreditsMonthly == null)
            {
                return await AbortOwnedAsync(
                    session,
                    IncludedPeriodMintResult.Skipped("plan_has_no_monthly_credits"),
                    cancellationToken
                );
            }

            var quantities = ResolveChannelQuantities(
                plan.CreditsMonthly,
                pricebook.ExtraLocationCreditsMonthly,
                billingAccount.PaidExtraLocationCount
            );

            var insertedIds = new List<Guid>();
            foreach (var (channel, quantity) in quantities)
            {
                if (quantity <= 0)
                {
                    continue;
                }

                if (
                    HasBaseGrantForPeriod(
                        entries,
                        channel,
                        expiresAtUtc
                    )
                )
                {
                    continue;
                }

                var row = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = billingAccount.RestaurantId,
                    Channel = channel,
                    EntryType = CreditLedgerEntryTypes.IncludedAllocation,
                    Quantity = quantity,
                    PricebookVersion = pricebookId,
                    PeriodStartUtc = periodStartUtc,
                    ExpiresAtUtc = expiresAtUtc,
                    CreatedAtUtc = nowUtc,
                };
                insertedIds.Add(row.Id);
                entries.Add(row);
                _context.CreditLedgerEntries.Add(row);
            }

            var postState = CreditLedgerCalculator.Project(entries, nowUtc);
            if (!CreditLedgerCalculator.InvariantsHold(postState))
            {
                return await AbortOwnedAsync(
                    session,
                    IncludedPeriodMintResult.Skipped("ledger_invariants_failed"),
                    cancellationToken
                );
            }

            if (insertedIds.Count == 0 && expiryRowsWritten == 0)
            {
                return await AbortOwnedAsync(
                    session,
                    IncludedPeriodMintResult.Skipped("grant_already_exists"),
                    cancellationToken
                );
            }

            if (insertedIds.Count > 0)
            {
                await _thresholdEvaluator.ResetBandsForIncludedPeriodMintAsync(
                    billingAccount.RestaurantId,
                    cancellationToken
                );
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (session.OwnsTransaction)
            {
                await session.Transaction.CommitAsync(cancellationToken);
            }

            return IncludedPeriodMintResult.Ok(insertedIds, expiryRowsWritten);
        }

        private static bool CanMintIncluded(BillingAccount billingAccount)
        {
            if (
                string.Equals(
                    billingAccount.SubscriptionPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            return string.Equals(
                billingAccount.BillingStatus,
                BillingStatuses.Active,
                StringComparison.Ordinal
            );
        }

        private static (DateTime PeriodStartUtc, DateTime ExpiresAtUtc)? ResolveOrderCompletedPeriod(
            BillingAccount billingAccount,
            IncludedPeriodOrderCompletedRequest request
        )
        {
            if (
                string.Equals(
                    billingAccount.BillingCycle,
                    BillingCycles.Annual,
                    StringComparison.Ordinal
                )
            )
            {
                return IncludedPeriodCalculator.ResolveAnnualSlice(
                    request.CycleStartUtc,
                    ResolveYearEndUtc(request.CycleStartUtc, request.CycleEndUtc),
                    sliceIndex: 0
                );
            }

            return IncludedPeriodCalculator.ResolveMonthlyPeriod(
                request.CycleStartUtc,
                request.NextCycleStartUtc,
                request.CycleEndUtc
            );
        }

        private static DateTime ResolveYearEndUtc(
            DateTime yearStartUtc,
            DateTime? cycleEndUtc
        )
        {
            return cycleEndUtc ?? yearStartUtc.AddMonths(12);
        }

        private static DateTime ResolveAnnualYearEndUtc(
            DateTime yearStartUtc,
            IReadOnlyList<CreditLedgerEntry> entries
        )
        {
            var sliceElevenStart = yearStartUtc.AddMonths(11);
            var sliceElevenEnd = entries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.PeriodStartUtc == sliceElevenStart
                )
                .Select(row => row.ExpiresAtUtc)
                .FirstOrDefault();
            return sliceElevenEnd ?? yearStartUtc.AddMonths(12);
        }

        private int WriteEndedIncludedExpiries(
            List<CreditLedgerEntry> entries,
            DateTime nowUtc
        )
        {
            var states = CreditLedgerCalculator.Project(entries, nowUtc);
            var written = 0;

            foreach (var state in states.Where(row =>
                CreditLedgerCalculator.IsIncludedClass(row.EntryType)
                && row.ExpiresAtUtc != null
                && row.ExpiresAtUtc.Value <= nowUtc
            ))
            {
                var unheldLeftover = state.Remaining - state.Held;
                if (unheldLeftover <= 0)
                {
                    continue;
                }

                var grant = entries.First(row => row.Id == state.Id);
                var expiry = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = grant.RestaurantId,
                    Channel = grant.Channel,
                    EntryType = CreditLedgerEntryTypes.Expiry,
                    Quantity = unheldLeftover,
                    AllocationId = grant.Id,
                    CreatedAtUtc = nowUtc,
                };
                entries.Add(expiry);
                _context.CreditLedgerEntries.Add(expiry);
                written++;
            }

            return written;
        }

        private static bool HasBaseGrantForPeriod(
            IEnumerable<CreditLedgerEntry> entries,
            string channel,
            DateTime expiresAtUtc
        )
        {
            return entries.Any(row =>
                row.Channel == channel
                && row.ExpiresAtUtc == expiresAtUtc
                && row.EntryType
                    is CreditLedgerEntryTypes.IncludedAllocation
                        or CreditLedgerEntryTypes.PlanMigration
            );
        }

        /// <summary>
        /// First paid included mint uses Current Pricebook. Later mints use Contracted.
        /// Pilot grants are <c>pilot_allocation</c>, so they do not count as prior included.
        /// </summary>
        private string ResolveMintPricebookId(
            BillingAccount billingAccount,
            IReadOnlyList<CreditLedgerEntry> entries
        )
        {
            var hasPriorIncluded = entries.Any(row =>
                row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
            );
            return hasPriorIncluded
                ? billingAccount.ContractedPricebookId
                : _pricebook.CurrentPricebookId;
        }

        private static IEnumerable<(string Channel, int Quantity)> ResolveChannelQuantities(
            PricebookChannelCredits planMonthly,
            PricebookChannelCredits? extraMonthly,
            int paidExtraLocationCount
        )
        {
            var extra = paidExtraLocationCount > 0 && extraMonthly != null
                ? extraMonthly
                : null;

            yield return (
                CreditChannels.Ai,
                planMonthly.Ai + (extra?.Ai ?? 0) * paidExtraLocationCount
            );
            yield return (
                CreditChannels.Email,
                planMonthly.Email + (extra?.Email ?? 0) * paidExtraLocationCount
            );
            yield return (
                CreditChannels.Sms,
                planMonthly.Sms + (extra?.Sms ?? 0) * paidExtraLocationCount
            );
        }

        private static string SubscriptionPlanKey(string subscriptionPlan)
        {
            return subscriptionPlan.Trim().ToLowerInvariant() switch
            {
                BillingSubscriptionPlans.Pilot => "pilot",
                BillingSubscriptionPlans.Starter => "starter",
                BillingSubscriptionPlans.Growth => "growth",
                BillingSubscriptionPlans.Group => "group",
                _ => subscriptionPlan.Trim().ToLowerInvariant(),
            };
        }

        private async Task<BillingAccount?> LockBillingAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM [BillingAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [RestaurantId] = {restaurantId}",
                    cancellationToken
                );
            }

            return await _context.BillingAccounts.FirstOrDefaultAsync(
                row => row.RestaurantId == restaurantId,
                cancellationToken
            );
        }

        private async Task<IncludedPeriodMintResult> FinishWithoutMintAsync(
            int expiryRowsWritten,
            LockSession session,
            CancellationToken cancellationToken
        )
        {
            if (expiryRowsWritten == 0)
            {
                return await AbortOwnedAsync(
                    session,
                    IncludedPeriodMintResult.Ok([], 0),
                    cancellationToken
                );
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (session.OwnsTransaction)
            {
                await session.Transaction.CommitAsync(cancellationToken);
            }

            return IncludedPeriodMintResult.Ok([], expiryRowsWritten);
        }

        private async Task<IncludedPeriodMintResult> FinishCancelApplyAsync(
            int expiryRowsWritten,
            LockSession session,
            CancellationToken cancellationToken
        )
        {
            _ = expiryRowsWritten;
            await _context.SaveChangesAsync(cancellationToken);
            if (session.OwnsTransaction)
            {
                await session.Transaction.CommitAsync(cancellationToken);
            }

            return IncludedPeriodMintResult.Skipped("cancel_applied");
        }

        private static async Task<IncludedPeriodMintResult> AbortOwnedAsync(
            LockSession session,
            IncludedPeriodMintResult result,
            CancellationToken cancellationToken
        )
        {
            if (session.OwnsTransaction)
            {
                await session.Transaction.RollbackAsync(cancellationToken);
            }

            return result;
        }

        private sealed record LockSession(
            bool OwnsTransaction,
            IDbContextTransaction Transaction
        );
    }

    internal sealed class NullPlanChangeService : IPlanChangeService
    {
        public static readonly NullPlanChangeService Instance = new();

        public Task ApplyImmediateSameCadenceUpgradeAsync(
            int restaurantId,
            string targetPlan,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }

        public Task<ScheduledChangeApplyResult> ApplyScheduledChangeOnRenewalAsync(
            BillingAccount billingAccount,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(ScheduledChangeApplyResult.Empty);
        }

        public bool HasScheduledChange(BillingAccount billingAccount) => false;

        public void ClearScheduledChange(BillingAccount billingAccount)
        {
        }

        public void SetScheduledChange(
            BillingAccount billingAccount,
            string targetPlan,
            string targetBillingCycle,
            int targetPaidExtraLocationCount
        )
        {
        }

        public string FormatScheduledChangeLine(
            BillingAccount billingAccount,
            string renewalDateLabel
        ) => string.Empty;

        public Task EnsureEntitlementGateAsync(
            string targetPlan,
            int targetPaidExtraLocationCount,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }
    }
}
