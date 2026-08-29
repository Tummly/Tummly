using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class PlanChangeService : IPlanChangeService
    {
        private const int ExtraUsersPerLocation = 2;

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;
        private readonly TimeProvider _clock;

        public PlanChangeService(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            TimeProvider clock
        )
        {
            _context = context;
            _pricebook = pricebook;
            _clock = clock;
        }

        public async Task ApplyImmediateSameCadenceUpgradeAsync(
            int restaurantId,
            string targetPlan,
            CancellationToken cancellationToken = default
        )
        {
            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                throw new InvalidOperationException("billing_account_missing");
            }

            if (
                !string.Equals(
                    billingAccount.BillingStatus,
                    BillingStatuses.Active,
                    StringComparison.Ordinal
                )
            )
            {
                throw new InvalidOperationException("billing_status_not_active");
            }

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var oldPlan = billingAccount.SubscriptionPlan;
            var oldExtras = billingAccount.PaidExtraLocationCount;
            var contractedBook = _pricebook.GetRequired(
                billingAccount.ContractedPricebookId
            );
            var currentBook = _pricebook.GetRequired(_pricebook.CurrentPricebookId);

            billingAccount.SubscriptionPlan = NormalizePlanDisplay(targetPlan);
            billingAccount.ContractedPricebookId = _pricebook.CurrentPricebookId;
            ClearScheduledChange(billingAccount);

            var openPeriod = await ResolveOpenIncludedPeriodAsync(
                restaurantId,
                nowUtc,
                cancellationToken
            );

            if (openPeriod != null)
            {
                var ratio = PlanMigrationMath.RemainingPeriodRatio(
                    openPeriod.Value.PeriodStartUtc,
                    openPeriod.Value.ExpiresAtUtc,
                    nowUtc
                );
                WritePlanMigrationGrants(
                    billingAccount,
                    contractedBook,
                    currentBook,
                    oldPlan,
                    oldExtras,
                    billingAccount.SubscriptionPlan,
                    billingAccount.PaidExtraLocationCount,
                    openPeriod.Value.PeriodStartUtc,
                    openPeriod.Value.ExpiresAtUtc,
                    ratio,
                    nowUtc
                );
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ScheduledChangeApplyResult> ApplyScheduledChangeOnRenewalAsync(
            BillingAccount billingAccount,
            CancellationToken cancellationToken = default
        )
        {
            if (!HasScheduledChange(billingAccount))
            {
                return ScheduledChangeApplyResult.Empty;
            }

            if (billingAccount.ScheduledCancelPlan)
            {
                return ScheduledChangeApplyResult.CancelDeferred;
            }

            var targetPlan = billingAccount.ScheduledTargetSubscriptionPlan
                ?? billingAccount.SubscriptionPlan;
            var targetCycle = billingAccount.ScheduledTargetBillingCycle
                ?? billingAccount.BillingCycle;
            var targetExtras = ResolveScheduledExtraCount(
                targetPlan,
                billingAccount.ScheduledTargetExtraLocationCount
            );

            try
            {
                await EnsureEntitlementGateAsync(
                    targetPlan,
                    targetExtras,
                    billingAccount.RestaurantId,
                    cancellationToken
                );
            }
            catch (InvalidOperationException)
            {
                return ScheduledChangeApplyResult.GateFailed;
            }

            billingAccount.SubscriptionPlan = NormalizePlanDisplay(targetPlan);
            if (!string.IsNullOrWhiteSpace(targetCycle))
            {
                billingAccount.BillingCycle = targetCycle;
            }

            billingAccount.PaidExtraLocationCount = targetExtras;
            billingAccount.ContractedPricebookId = _pricebook.CurrentPricebookId;
            ClearScheduledChange(billingAccount);
            return ScheduledChangeApplyResult.Applied;
        }

        public bool HasScheduledChange(BillingAccount billingAccount)
        {
            return billingAccount.HasScheduledChange;
        }

        public void ClearScheduledChange(BillingAccount billingAccount)
        {
            billingAccount.ClearScheduledChangeSlot();
        }

        public void SetScheduledChange(
            BillingAccount billingAccount,
            string targetPlan,
            string targetBillingCycle,
            int targetPaidExtraLocationCount
        )
        {
            billingAccount.HasScheduledChange = true;
            billingAccount.ScheduledTargetSubscriptionPlan = NormalizePlanDisplay(targetPlan);
            billingAccount.ScheduledTargetBillingCycle = targetBillingCycle;
            billingAccount.ScheduledTargetExtraLocationCount = ResolveScheduledExtraCount(
                targetPlan,
                targetPaidExtraLocationCount
            );
            billingAccount.ScheduledCancelPlan = false;
        }

        public string FormatScheduledChangeLine(
            BillingAccount billingAccount,
            string renewalDateLabel
        )
        {
            if (!HasScheduledChange(billingAccount))
            {
                return string.Empty;
            }

            if (billingAccount.ScheduledCancelPlan)
            {
                return $"Cancels on {renewalDateLabel}";
            }

            var date = renewalDateLabel.StartsWith("Renews ", StringComparison.Ordinal)
                ? renewalDateLabel["Renews ".Length..]
                : renewalDateLabel;

            var livePlan = billingAccount.SubscriptionPlan;
            var targetPlan = billingAccount.ScheduledTargetSubscriptionPlan ?? livePlan;
            var liveCycle = billingAccount.BillingCycle;
            var targetCycle = billingAccount.ScheduledTargetBillingCycle ?? liveCycle;

            if (
                string.Equals(livePlan, targetPlan, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(liveCycle, targetCycle, StringComparison.OrdinalIgnoreCase)
            )
            {
                var cadenceLabel = string.Equals(
                    targetCycle,
                    BillingCycles.Annual,
                    StringComparison.Ordinal
                )
                    ? "Annual"
                    : "Monthly";
                return $"Changes to {cadenceLabel} on {date}";
            }

            return $"Changes to {NormalizePlanDisplay(targetPlan)} on {date}";
        }

        public async Task EnsureEntitlementGateAsync(
            string targetPlan,
            int targetPaidExtraLocationCount,
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var extras = ResolveScheduledExtraCount(targetPlan, targetPaidExtraLocationCount);
            var book = _pricebook.GetRequired(_pricebook.CurrentPricebookId);
            if (
                !LocationCap.TryResolve(
                    book,
                    targetPlan,
                    extras,
                    out var entitledLocations
                )
            )
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            var activeLocations = await _context.RestaurantLocations
                .CountAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (activeLocations > entitledLocations)
            {
                throw new InvalidOperationException("location_cap_reached");
            }

            if (!TryResolveTeamCap(book, targetPlan, extras, out var teamCap))
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var activeMembers = await _context.RestaurantMemberships
                .CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.Status == MembershipStatus.Active,
                    cancellationToken
                );
            var pendingInvites = await _context.TeamInvitations
                .CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.ExpiresAt > now,
                    cancellationToken
                );
            if (activeMembers + pendingInvites > teamCap)
            {
                throw new InvalidOperationException("team_member_cap_reached");
            }
        }

        private void WritePlanMigrationGrants(
            BillingAccount billingAccount,
            PricebookSnapshot oldBook,
            PricebookSnapshot newBook,
            string oldPlan,
            int oldExtras,
            string newPlan,
            int newExtras,
            DateTime periodStartUtc,
            DateTime expiresAtUtc,
            decimal ratio,
            DateTime nowUtc
        )
        {
            if (ratio <= 0m)
            {
                return;
            }

            var oldQty = ResolveMonthlyQuantities(oldBook, oldPlan, oldExtras);
            var newQty = ResolveMonthlyQuantities(newBook, newPlan, newExtras);
            var pricebookVersion = billingAccount.ContractedPricebookId;

            foreach (var channel in CreditChannels.All)
            {
                var increment = PlanMigrationMath.PositiveIncrement(
                    newQty[channel],
                    oldQty[channel]
                );
                var grant = PlanMigrationMath.FloorGrant(increment, ratio);
                if (grant <= 0)
                {
                    continue;
                }

                _context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = billingAccount.RestaurantId,
                        Channel = channel,
                        EntryType = CreditLedgerEntryTypes.PlanMigration,
                        Quantity = grant,
                        PricebookVersion = pricebookVersion,
                        PeriodStartUtc = periodStartUtc,
                        ExpiresAtUtc = expiresAtUtc,
                        CreatedAtUtc = nowUtc,
                    }
                );
            }
        }

        private static Dictionary<string, int> ResolveMonthlyQuantities(
            PricebookSnapshot book,
            string subscriptionPlan,
            int paidExtraLocationCount
        )
        {
            var key = subscriptionPlan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(key, out var plan) || plan.CreditsMonthly == null)
            {
                return new Dictionary<string, int>
                {
                    [CreditChannels.Ai] = 0,
                    [CreditChannels.Email] = 0,
                    [CreditChannels.Sms] = 0,
                };
            }

            var extras = paidExtraLocationCount > 0
                && string.Equals(key, "group", StringComparison.Ordinal)
                && book.ExtraLocationCreditsMonthly != null
                ? book.ExtraLocationCreditsMonthly
                : null;
            var extraCount = extras == null ? 0 : paidExtraLocationCount;

            return new Dictionary<string, int>
            {
                [CreditChannels.Ai] =
                    plan.CreditsMonthly.Ai + (extras?.Ai ?? 0) * extraCount,
                [CreditChannels.Email] =
                    plan.CreditsMonthly.Email + (extras?.Email ?? 0) * extraCount,
                [CreditChannels.Sms] =
                    plan.CreditsMonthly.Sms + (extras?.Sms ?? 0) * extraCount,
            };
        }

        private async Task<(DateTime PeriodStartUtc, DateTime ExpiresAtUtc)?>
            ResolveOpenIncludedPeriodAsync(
                int restaurantId,
                DateTime nowUtc,
                CancellationToken cancellationToken
            )
        {
            var open = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.PeriodStartUtc != null
                    && row.ExpiresAtUtc != null
                    && row.PeriodStartUtc.Value <= nowUtc
                    && row.ExpiresAtUtc.Value > nowUtc
                    && (
                        row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                        || row.EntryType == CreditLedgerEntryTypes.PlanMigration
                    )
                )
                .OrderByDescending(row => row.PeriodStartUtc)
                .Select(row => new { row.PeriodStartUtc, row.ExpiresAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

            if (open?.PeriodStartUtc == null || open.ExpiresAtUtc == null)
            {
                // Ratio-0 path: ended open window still applies plan with no grant.
                var ended = await _context.CreditLedgerEntries
                    .AsNoTracking()
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.PeriodStartUtc != null
                        && row.ExpiresAtUtc != null
                        && row.ExpiresAtUtc.Value <= nowUtc
                        && (
                            row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                            || row.EntryType == CreditLedgerEntryTypes.PlanMigration
                        )
                    )
                    .OrderByDescending(row => row.ExpiresAtUtc)
                    .Select(row => new { row.PeriodStartUtc, row.ExpiresAtUtc })
                    .FirstOrDefaultAsync(cancellationToken);

                if (ended?.PeriodStartUtc == null || ended.ExpiresAtUtc == null)
                {
                    return null;
                }

                return (ended.PeriodStartUtc.Value, ended.ExpiresAtUtc.Value);
            }

            return (open.PeriodStartUtc.Value, open.ExpiresAtUtc.Value);
        }

        private static int ResolveScheduledExtraCount(
            string targetPlan,
            int? scheduledExtras
        )
        {
            if (
                !string.Equals(
                    targetPlan.Trim(),
                    BillingSubscriptionPlans.Group,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return 0;
            }

            return Math.Max(0, scheduledExtras ?? 0);
        }

        private static bool TryResolveTeamCap(
            PricebookSnapshot book,
            string subscriptionPlan,
            int paidExtraLocationCount,
            out int cap
        )
        {
            cap = 0;
            var key = subscriptionPlan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(key, out var plan))
            {
                return false;
            }

            var extras = string.Equals(key, "group", StringComparison.Ordinal)
                ? Math.Max(0, paidExtraLocationCount)
                : 0;
            cap = plan.IncludedTeamMembers + (extras * ExtraUsersPerLocation);
            return true;
        }

        private static string NormalizePlanDisplay(string plan)
        {
            return plan.Trim().ToLowerInvariant() switch
            {
                "pilot" => BillingSubscriptionPlans.Pilot,
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                _ => plan.Trim(),
            };
        }
    }
}
