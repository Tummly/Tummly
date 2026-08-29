using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
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

        public const string RevolutCustomerRequiredCode = "revolut_customer_required";

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;
        private readonly IRevolutMerchantCreateGate _revolutMerchantCreateGate;
        private readonly IRevolutMerchantClient _merchant;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _clock;

        public ExtraGroupLocationService(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            IRevolutMerchantCreateGate revolutMerchantCreateGate,
            IRevolutMerchantClient merchant,
            IConfiguration configuration,
            TimeProvider clock
        )
        {
            _context = context;
            _pricebook = pricebook;
            _revolutMerchantCreateGate = revolutMerchantCreateGate;
            _merchant = merchant;
            _configuration = configuration;
            _clock = clock;
        }

        public async Task<ExtraLocationResultDto?> SubmitAsync(
            int userId,
            int restaurantId,
            string action,
            string? idempotencyKey = null
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
                return await SubmitAddAsync(account, restaurant, idempotencyKey);
            }

            return await SubmitRemoveAsync(account, restaurantId);
        }

        public async Task<ExtraGroupLocationApplyResult> ApplyAddOnOrderCompletedAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            return await RunWithOptionalTransactionAsync(
                async () =>
                {
                    var account = await LockBillingAccountAsync(
                        restaurantId,
                        cancellationToken
                    );
                    if (account == null)
                    {
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
                        return ExtraGroupLocationApplyResult.Fail(
                            ExtraLocationNotGroupCode
                        );
                    }

                    if (
                        !string.Equals(
                            account.BillingStatus,
                            BillingStatuses.Active,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        return ExtraGroupLocationApplyResult.Fail(
                            BillingStatusNotActiveCode
                        );
                    }

                    if (!CanRaiseSelfServeCap(account.PaidExtraLocationCount))
                    {
                        return ExtraGroupLocationApplyResult.Fail(
                            GroupSelfServeMaxReachedCode
                        );
                    }

                    var currentBook = _pricebook.GetRequired(_pricebook.CurrentPricebookId);
                    var extrasMonthly = currentBook.ExtraLocationCreditsMonthly;
                    if (extrasMonthly == null)
                    {
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
                    return ExtraGroupLocationApplyResult.Ok(inserted);
                },
                cancellationToken
            );
        }

        public async Task<ExtraGroupLocationApplyResult> ApplyScheduledRemoveAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            string? subscriptionId = null;
            string? cadenceApi = null;

            var result = await RunWithOptionalTransactionAsync(
                async () =>
                {
                    var account = await LockBillingAccountAsync(
                        restaurantId,
                        cancellationToken
                    );
                    if (account == null)
                    {
                        return ExtraGroupLocationApplyResult.Fail("restaurant_not_found");
                    }

                    if (
                        !account.HasScheduledChange
                        || account.ScheduledCancelPlan
                        || account.ScheduledTargetExtraLocationCount == null
                    )
                    {
                        return ExtraGroupLocationApplyResult.Fail("scheduled_change_empty");
                    }

                    var targetExtra = account.ScheduledTargetExtraLocationCount.Value;
                    if (targetExtra < 0 || targetExtra >= account.PaidExtraLocationCount)
                    {
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
                        return ExtraGroupLocationApplyResult.Fail("apply_gate_failed");
                    }

                    // Extra remove: decrement by 1. No credit clawback this period.
                    account.PaidExtraLocationCount = Math.Max(
                        0,
                        account.PaidExtraLocationCount - 1
                    );
                    account.ClearScheduledChangeSlot();

                    if (
                        account.PaidExtraLocationCount == 0
                    )
                    {
                        subscriptionId =
                            await RevolutSubscriptionCorrelation.ResolveLatestSubscriptionIdAsync(
                                _context,
                                restaurantId,
                                cancellationToken
                            );
                        cadenceApi = CadenceApi(account.BillingCycle);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    return ExtraGroupLocationApplyResult.Ok();
                },
                cancellationToken
            );

            if (
                result.Succeeded
                && !string.IsNullOrWhiteSpace(subscriptionId)
                && cadenceApi != null
            )
            {
                await TryChangePlanAtCycleEndAsync(
                    subscriptionId,
                    RevolutPlanVariationKeys.ForPlanCadence(
                        BillingSubscriptionPlans.Group,
                        cadenceApi
                    )!,
                    cancellationToken
                );
            }

            return result;
        }

        private async Task<ExtraLocationResultDto> SubmitAddAsync(
            BillingAccount account,
            Restaurant restaurant,
            string? idempotencyKey
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

            if (string.IsNullOrWhiteSpace(account.RevolutCustomerId))
            {
                throw new ExtraGroupLocationException(RevolutCustomerRequiredCode);
            }

            var cadenceApi = CadenceApi(account.BillingCycle);
            var lookupKey = RevolutPlanVariationKeys.ForExtraLocation(cadenceApi);
            var gateCode = _revolutMerchantCreateGate.Evaluate(lookupKey);
            if (gateCode != null)
            {
                throw new ExtraGroupLocationException(gateCode);
            }

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var amounts = await ComputeProratedAmountsAsync(
                account,
                cadenceApi,
                nowUtc
            );

            var locationId = await ResolveLocationIdAsync(restaurant.Id);
            var successRedirectUrl = BuildPlanSubscriptionRedirectUrl(
                restaurant.AccountType,
                locationId
            );

            var subscriptionId =
                await RevolutSubscriptionCorrelation.ResolveLatestSubscriptionIdAsync(
                    _context,
                    restaurant.Id
                ) ?? string.Empty;

            RevolutMerchantCreateResult created;
            try
            {
                created = await _merchant.CreateOrderAsync(
                    new RevolutCreateOrderRequest(
                        AmountMinor: amounts.GrossAmountMinor,
                        Currency: "GBP",
                        PlanVariationLookupKey: lookupKey,
                        CustomerId: account.RevolutCustomerId,
                        RedirectUrl: successRedirectUrl,
                        Description: "Additional Group Location",
                        LineItems:
                        [
                            new RevolutOrderLineItem(
                                Name: "Additional Group Location",
                                UnitPriceAmount: amounts.NetAmountMinor,
                                Quantity: 1,
                                TotalAmount: amounts.GrossAmountMinor,
                                Taxes:
                                [
                                    new RevolutOrderLineItemTax(
                                        Name: "VAT",
                                        Percentage: "20.00",
                                        Amount: amounts.VatAmountMinor
                                    ),
                                ]
                            ),
                        ]
                    )
                );
            }
            catch (RevolutMerchantNotReadyException ex)
            {
                throw new ExtraGroupLocationException(ex.Code);
            }

            if (!created.Succeeded || string.IsNullOrWhiteSpace(created.Id))
            {
                throw new ExtraGroupLocationException(
                    created.ErrorCode ?? "revolut_http_error"
                );
            }

            var checkoutUrl = created.CheckoutUrl;
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                var order = await _merchant.GetOrderAsync(created.Id);
                if (
                    !order.Succeeded
                    || string.IsNullOrWhiteSpace(order.CheckoutUrl)
                )
                {
                    throw new ExtraGroupLocationException(
                        order.ErrorCode ?? "revolut_http_error"
                    );
                }

                checkoutUrl = order.CheckoutUrl;
            }

            _context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = created.Id.Trim(),
                    RestaurantId = restaurant.Id,
                    Purpose = RevolutOrderIntentPurposes.ExtraLocation,
                    TargetPlan = BillingSubscriptionPlans.Group,
                    TargetCadence = cadenceApi,
                    RevolutSubscriptionId = subscriptionId,
                    CheckoutUrl = checkoutUrl,
                    IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
                        ? Guid.NewGuid().ToString("D")
                        : idempotencyKey.Trim(),
                    IsOpen = true,
                    NetAmountMinor = amounts.NetAmountMinor,
                    VatAmountMinor = amounts.VatAmountMinor,
                    GrossAmountMinor = amounts.GrossAmountMinor,
                    TargetPaidExtraLocationCount =
                        account.PaidExtraLocationCount + 1,
                    CreatedAtUtc = nowUtc,
                }
            );
            await _context.SaveChangesAsync();

            return new ExtraLocationResultDto
            {
                Outcome = "pay",
                RedirectUrl = checkoutUrl,
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

            // When the scheduled remove lands at 0 extras, tell Revolut to drop
            // the add-on SKU at cycle end (binary Group vs GroupLocation map).
            if (targetExtra == 0)
            {
                var subscriptionId =
                    await RevolutSubscriptionCorrelation.ResolveLatestSubscriptionIdAsync(
                        _context,
                        restaurantId
                    );
                var cadenceApi = CadenceApi(account.BillingCycle);
                var baseKey = RevolutPlanVariationKeys.ForPlanCadence(
                    BillingSubscriptionPlans.Group,
                    cadenceApi
                );
                if (
                    !string.IsNullOrWhiteSpace(subscriptionId)
                    && baseKey != null
                )
                {
                    await TryChangePlanAtCycleEndAsync(subscriptionId, baseKey);
                }
            }

            var renewalLabel = FormatRenewalLabel(account.RenewalDateUtc);
            return new ExtraLocationResultDto
            {
                Outcome = "scheduled",
                ScheduledChangeLine =
                    $"Removes 1 Additional Group Location on {renewalLabel}",
            };
        }

        private async Task<(
            int NetAmountMinor,
            int VatAmountMinor,
            int GrossAmountMinor
        )> ComputeProratedAmountsAsync(
            BillingAccount account,
            string cadenceApi,
            DateTime nowUtc
        )
        {
            var book = _pricebook.GetRequired(account.ContractedPricebookId);
            var annual = string.Equals(
                cadenceApi,
                "annual",
                StringComparison.OrdinalIgnoreCase
            );
            var periodNet = annual
                ? book.ExtraGroupLocationAnnualNetPence
                    ?? book.ExtraGroupLocationMonthlyNetPence
                : book.ExtraGroupLocationMonthlyNetPence;
            if (periodNet == null || periodNet.Value < 0)
            {
                throw new ExtraGroupLocationException("extra_location_price_missing");
            }

            var ratio = 1m;
            var openPeriod = await FindOpenIncludedPeriodAsync(
                account.RestaurantId,
                nowUtc
            );
            if (openPeriod != null)
            {
                ratio = PlanMigrationMath.RemainingPeriodRatio(
                    openPeriod.Value.Start,
                    openPeriod.Value.End,
                    nowUtc
                );
            }
            else if (
                account.RenewalDateUtc is DateTime renewal
                && renewal > nowUtc
            )
            {
                var periodStart = annual
                    ? renewal.AddMonths(-12)
                    : renewal.AddMonths(-1);
                if (periodStart < nowUtc)
                {
                    ratio = PlanMigrationMath.RemainingPeriodRatio(
                        periodStart,
                        renewal,
                        nowUtc
                    );
                }
            }

            var net = (int)
                decimal.Round(
                    periodNet.Value * ratio,
                    0,
                    MidpointRounding.AwayFromZero
                );
            var gross = TummlyVatMath.GrossMinorFromNetPence(net, book.VatRateBps);
            return (net, gross - net, gross);
        }

        private async Task TryChangePlanAtCycleEndAsync(
            string subscriptionId,
            string planVariationLookupKey,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                await _merchant.ChangeSubscriptionPlanAsync(
                    subscriptionId,
                    planVariationLookupKey,
                    cancellationToken
                );
            }
            catch (RevolutMerchantNotReadyException)
            {
                // Schedule / ledger already committed; gate miss is non-fatal here.
            }
        }

        private string BuildPlanSubscriptionRedirectUrl(
            string restaurantAccountType,
            int locationId
        )
        {
            var baseUrl = _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl is not configured."
                );
            }

            var root = string.Equals(
                restaurantAccountType,
                "Multi",
                StringComparison.Ordinal
            )
                ? "/multi-dashboard"
                : "/single-dashboard";

            return $"{baseUrl}{root}/settings/billing-credits?location={locationId}&tab=plan-subscription";
        }

        private async Task<int> ResolveLocationIdAsync(int restaurantId)
        {
            var locationId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => row.Id)
                .FirstOrDefaultAsync();
            return locationId == 0 ? restaurantId : locationId;
        }

        private static string CadenceApi(string? billingCycle)
        {
            return string.Equals(
                billingCycle,
                BillingCycles.Annual,
                StringComparison.OrdinalIgnoreCase
            )
                ? "annual"
                : "monthly";
        }

        private static string FormatRenewalLabel(DateTime? renewalDateUtc)
        {
            if (renewalDateUtc == null)
            {
                return "renewal";
            }

            var utc = renewalDateUtc.Value.Kind == DateTimeKind.Utc
                ? renewalDateUtc.Value
                : DateTime.SpecifyKind(renewalDateUtc.Value, DateTimeKind.Utc);
            return utc.ToString("d MMMM yyyy");
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
            var now = _clock.GetUtcNow().UtcDateTime;
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
            CancellationToken cancellationToken = default
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

        private async Task<ExtraGroupLocationApplyResult> RunWithOptionalTransactionAsync(
            Func<Task<ExtraGroupLocationApplyResult>> action,
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

            try
            {
                var result = await action();
                if (ownsTransaction && owned != null)
                {
                    if (result.Succeeded)
                    {
                        await owned.CommitAsync(cancellationToken);
                    }
                    else
                    {
                        await owned.RollbackAsync(cancellationToken);
                    }
                }

                return result;
            }
            catch
            {
                if (ownsTransaction && owned != null)
                {
                    await owned.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                if (owned != null)
                {
                    await owned.DisposeAsync();
                }
            }
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
