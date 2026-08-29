using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ExtraGroupLocationService : IExtraGroupLocationService
    {
        public const string ExtraLocationNotGroupCode = "extra_location_not_group";

        public const string GroupSelfServeMaxReachedCode =
            "group_location_self_serve_max_reached";

        public const string BillingStatusNotActiveCode = "billing_status_not_active";

        public const string BillingWriteNotPermittedCode = "billing_write_not_permitted";

        public const string InvalidActionCode = "invalid_extra_location_action";

        public const string RemoveBelowFloorCode = "remove_below_floor";

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;
        private readonly IRevolutMerchantCreateGate _revolutMerchantCreateGate;

        public ExtraGroupLocationService(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            IRevolutMerchantCreateGate revolutMerchantCreateGate
        )
        {
            _context = context;
            _pricebook = pricebook;
            _revolutMerchantCreateGate = revolutMerchantCreateGate;
        }

        public async Task<ExtraLocationResultDto?> SubmitAsync(
            int userId,
            int restaurantId,
            string action
        )
        {
            var normalized = (action ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized is not ("add" or "remove"))
            {
                throw new ExtraGroupLocationException(InvalidActionCode);
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return null;
            }

            await EnsureOwnerAsync(userId, restaurantId);

            var account = await _context.BillingAccounts
                .AsTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (account == null)
            {
                throw new InvalidOperationException(
                    $"Billing Account is missing for restaurant {restaurantId}."
                );
            }

            if (
                !string.Equals(
                    account.SubscriptionPlan,
                    BillingSubscriptionPlans.Group,
                    StringComparison.Ordinal
                )
            )
            {
                throw new ExtraGroupLocationException(ExtraLocationNotGroupCode);
            }

            if (normalized == "add")
            {
                return SubmitAdd(account, restaurantId);
            }

            return await SubmitRemoveAsync(account, restaurantId);
        }

        public async Task<ExtraGroupLocationApplyResult> ApplyAddOnOrderCompletedAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var account = await LockBillingAccountAsync(
                    restaurantId,
                    cancellationToken
                );
                if (account == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail("restaurant_not_found");
                }

                if (
                    !string.Equals(
                        account.SubscriptionPlan,
                        BillingSubscriptionPlans.Group,
                        StringComparison.Ordinal
                    )
                )
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail(ExtraLocationNotGroupCode);
                }

                if (
                    !string.Equals(
                        account.BillingStatus,
                        BillingStatuses.Active,
                        StringComparison.Ordinal
                    )
                )
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail(BillingStatusNotActiveCode);
                }

                if (!CanRaiseSelfServeCap(account.PaidExtraLocationCount))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail(
                        GroupSelfServeMaxReachedCode
                    );
                }

                var currentBook = _pricebook.GetRequired(_pricebook.CurrentPricebookId);
                var extrasMonthly = currentBook.ExtraLocationCreditsMonthly;
                if (extrasMonthly == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail("extra_credits_missing");
                }

                var openPeriod = await FindOpenIncludedPeriodAsync(
                    restaurantId,
                    nowUtc,
                    cancellationToken
                );

                account.PaidExtraLocationCount += 1;
                account.ContractedPricebookId = _pricebook.CurrentPricebookId;
                account.ClearScheduledChangeSlot();

                var inserted = new List<Guid>();
                if (openPeriod != null)
                {
                    var ratio = PlanMigrationMath.RemainingPeriodRatio(
                        openPeriod.Value.Start,
                        openPeriod.Value.End,
                        nowUtc
                    );
                    inserted.AddRange(
                        InsertPlanMigrationGrants(
                            restaurantId,
                            extrasMonthly,
                            ratio,
                            openPeriod.Value.Start,
                            openPeriod.Value.End,
                            _pricebook.CurrentPricebookId,
                            nowUtc
                        )
                    );
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return ExtraGroupLocationApplyResult.Ok(inserted);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<ExtraGroupLocationApplyResult> ApplyScheduledRemoveAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var account = await LockBillingAccountAsync(
                    restaurantId,
                    cancellationToken
                );
                if (account == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail("restaurant_not_found");
                }

                if (
                    !account.HasScheduledChange
                    || account.ScheduledCancelPlan
                    || account.ScheduledTargetExtraLocationCount == null
                )
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail("scheduled_change_empty");
                }

                var targetExtra = account.ScheduledTargetExtraLocationCount.Value;
                if (targetExtra < 0 || targetExtra >= account.PaidExtraLocationCount)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail("invalid_scheduled_extra");
                }

                if (
                    !await ReducingChangeGatesPassAsync(
                        account,
                        targetExtra,
                        cancellationToken
                    )
                )
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Fail("apply_gate_failed");
                }

                // Extra remove: decrement by 1. No credit clawback this period.
                account.PaidExtraLocationCount = Math.Max(
                    0,
                    account.PaidExtraLocationCount - 1
                );
                account.ClearScheduledChangeSlot();

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return ExtraGroupLocationApplyResult.Ok();
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private ExtraLocationResultDto SubmitAdd(
            BillingAccount account,
            int restaurantId
        )
        {
            RequireActiveBillingStatus(account);

            if (!CanRaiseSelfServeCap(account.PaidExtraLocationCount))
            {
                LocationCap.TryResolve(
                    _pricebook.GetRequired(account.ContractedPricebookId),
                    account.SubscriptionPlan,
                    account.PaidExtraLocationCount,
                    out var current
                );
                throw new ExtraGroupLocationException(
                    GroupSelfServeMaxReachedCode,
                    LocationCap.GroupSelfServeMax,
                    current
                );
            }

            var cadenceApi = string.Equals(
                account.BillingCycle,
                "Annual",
                StringComparison.OrdinalIgnoreCase
            )
                ? "annual"
                : "monthly";
            var lookupKey = RevolutPlanVariationKeys.ForExtraLocation(cadenceApi);
            var gateCode = _revolutMerchantCreateGate.Evaluate(lookupKey);
            if (gateCode != null)
            {
                throw new ExtraGroupLocationException(gateCode);
            }

            return new ExtraLocationResultDto
            {
                Outcome = "pay",
                RedirectUrl =
                    $"https://checkout.revolut.com/pay/example/{restaurantId}/extra-location",
            };
        }

        private async Task<ExtraLocationResultDto> SubmitRemoveAsync(
            BillingAccount account,
            int restaurantId
        )
        {
            RequireActiveBillingStatus(account);

            if (account.PaidExtraLocationCount < 1)
            {
                throw new ExtraGroupLocationException(RemoveBelowFloorCode);
            }

            var targetExtra = account.PaidExtraLocationCount - 1;
            if (!await ReducingChangeGatesPassAsync(account, targetExtra))
            {
                throw new ExtraGroupLocationException(RemoveBelowFloorCode);
            }

            account.HasScheduledChange = true;
            account.ScheduledTargetSubscriptionPlan = account.SubscriptionPlan;
            account.ScheduledTargetBillingCycle = account.BillingCycle;
            account.ScheduledTargetExtraLocationCount = targetExtra;
            account.ScheduledCancelPlan = false;
            await _context.SaveChangesAsync();

            var renewalLabel = "15 September 2026";
            return new ExtraLocationResultDto
            {
                Outcome = "scheduled",
                ScheduledChangeLine =
                    $"Removes 1 Additional Group Location on {renewalLabel}",
            };
        }

        private static void RequireActiveBillingStatus(BillingAccount account)
        {
            var paidDeny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(account)
            );
            if (paidDeny != null)
            {
                throw new ExtraGroupLocationException(paidDeny);
            }

            if (
                !string.Equals(
                    account.BillingStatus,
                    BillingStatuses.Active,
                    StringComparison.Ordinal
                )
            )
            {
                throw new ExtraGroupLocationException(BillingStatusNotActiveCode);
            }
        }

        private async Task EnsureOwnerAsync(int userId, int restaurantId)
        {
            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            if (
                actorMembership == null
                || actorMembership.PermissionRole != PermissionRoles.Owner
            )
            {
                throw new ExtraGroupLocationException(BillingWriteNotPermittedCode);
            }
        }

        private async Task<bool> ReducingChangeGatesPassAsync(
            BillingAccount account,
            int targetExtraLocationCount,
            CancellationToken cancellationToken = default
        )
        {
            var book = _pricebook.GetRequired(account.ContractedPricebookId);
            if (
                !LocationCap.TryResolve(
                    book,
                    account.SubscriptionPlan,
                    targetExtraLocationCount,
                    out var entitledLocations
                )
            )
            {
                return false;
            }

            var activeLocations = await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(
                    row => row.RestaurantId == account.RestaurantId,
                    cancellationToken
                );
            if (activeLocations > entitledLocations)
            {
                return false;
            }

            var planKey = account.SubscriptionPlan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(planKey, out var plan))
            {
                return false;
            }

            var teamCap =
                plan.IncludedTeamMembers
                + (Math.Max(0, targetExtraLocationCount) * ExtraUsersPerPaidLocation);
            var teamUsage = await CountTeamMemberUsageAsync(
                account.RestaurantId,
                cancellationToken
            );
            return teamUsage <= teamCap;
        }

        private async Task<int> CountTeamMemberUsageAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;
            var active = await _context.RestaurantMemberships.CountAsync(
                row =>
                    row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active,
                cancellationToken
            );
            var pending = await _context.TeamInvitations.CountAsync(
                row => row.RestaurantId == restaurantId && row.ExpiresAt > now,
                cancellationToken
            );
            return active + pending;
        }

        public static bool CanRaiseSelfServeCap(int paidExtraLocationCount)
        {
            var before = Math.Min(
                BillingCreditsService.GroupIncludedLocations + paidExtraLocationCount,
                LocationCap.GroupSelfServeMax
            );
            var after = Math.Min(
                BillingCreditsService.GroupIncludedLocations
                    + paidExtraLocationCount
                    + 1,
                LocationCap.GroupSelfServeMax
            );
            return after > before;
        }

        private const int ExtraUsersPerPaidLocation = 2;

        private List<Guid> InsertPlanMigrationGrants(
            int restaurantId,
            PricebookChannelCredits monthlyAdded,
            decimal ratio,
            DateTime periodStartUtc,
            DateTime expiresAtUtc,
            string pricebookVersion,
            DateTime nowUtc
        )
        {
            var inserted = new List<Guid>();
            foreach (var (channel, monthly) in ChannelPairs(monthlyAdded))
            {
                var quantity = PlanMigrationMath.FloorGrant(monthly, ratio);
                if (quantity <= 0)
                {
                    continue;
                }

                var id = Guid.NewGuid();
                _context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = id,
                        RestaurantId = restaurantId,
                        Channel = channel,
                        EntryType = CreditLedgerEntryTypes.PlanMigration,
                        Quantity = quantity,
                        PricebookVersion = pricebookVersion,
                        PeriodStartUtc = periodStartUtc,
                        ExpiresAtUtc = expiresAtUtc,
                        CreatedAtUtc = nowUtc,
                    }
                );
                inserted.Add(id);
            }

            return inserted;
        }

        private static IEnumerable<(string Channel, int Monthly)> ChannelPairs(
            PricebookChannelCredits credits
        )
        {
            yield return (CreditChannels.Ai, credits.Ai);
            yield return (CreditChannels.Email, credits.Email);
            yield return (CreditChannels.Sms, credits.Sms);
        }

        private async Task<(DateTime Start, DateTime End)?> FindOpenIncludedPeriodAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken
        )
        {
            var rows = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && (
                        row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                        || row.EntryType == CreditLedgerEntryTypes.PlanMigration
                    )
                    && row.PeriodStartUtc != null
                    && row.ExpiresAtUtc != null
                    && row.PeriodStartUtc <= nowUtc
                    && nowUtc < row.ExpiresAtUtc
                )
                .OrderByDescending(row => row.PeriodStartUtc)
                .Select(row => new { row.PeriodStartUtc, row.ExpiresAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

            if (rows?.PeriodStartUtc == null || rows.ExpiresAtUtc == null)
            {
                return null;
            }

            return (rows.PeriodStartUtc.Value, rows.ExpiresAtUtc.Value);
        }

        private async Task<BillingAccount?> LockBillingAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            if (_context.Database.IsSqlServer())
            {
                return await _context.BillingAccounts
                    .FromSqlInterpolated(
                        $"SELECT * FROM BillingAccounts WITH (UPDLOCK, ROWLOCK) WHERE RestaurantId = {restaurantId}"
                    )
                    .AsTracking()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await _context.BillingAccounts.FirstOrDefaultAsync(
                row => row.RestaurantId == restaurantId,
                cancellationToken
            );
        }
    }

    public sealed class ExtraGroupLocationException : Exception
    {
        public ExtraGroupLocationException(
            string code,
            int? cap = null,
            int? current = null
        )
            : base(code)
        {
            Code = code;
            Cap = cap;
            Current = current;
        }

        public string Code { get; }

        public int? Cap { get; }

        public int? Current { get; }
    }
}
