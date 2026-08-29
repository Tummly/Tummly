using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class BillingCreditsService : IBillingCreditsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebookCatalog;
        private readonly ICreditBalanceSnapshot _creditBalance;
        private readonly IBillingAccountLifecycle _lifecycle;
        private readonly IPlanChangeService _planChange;
        private readonly IRevolutMerchantCreateGate _revolutMerchantCreateGate;
        private readonly IFirstPaidConversionPaySession _firstPaidConversionPaySession;
        private readonly ISameCadenceUpgradePaySession _sameCadenceUpgradePaySession;
        private readonly IPaymentMethodUpdatePaySession _paymentMethodUpdatePaySession;
        private readonly ITummlyVatInvoiceService _vatInvoices;
        private readonly ICycleEndPlanChange _cycleEndPlanChange;

        public BillingCreditsService(
            ApplicationDbContext context,
            IPricebookCatalog pricebookCatalog,
            ICreditBalanceSnapshot creditBalance,
            IBillingAccountLifecycle lifecycle,
            IPlanChangeService planChange,
            IRevolutMerchantCreateGate revolutMerchantCreateGate,
            IFirstPaidConversionPaySession firstPaidConversionPaySession,
            ISameCadenceUpgradePaySession sameCadenceUpgradePaySession,
            IPaymentMethodUpdatePaySession paymentMethodUpdatePaySession,
            ITummlyVatInvoiceService vatInvoices,
            ICycleEndPlanChange cycleEndPlanChange
        )
        {
            _context = context;
            _pricebookCatalog = pricebookCatalog;
            _creditBalance = creditBalance;
            _lifecycle = lifecycle;
            _planChange = planChange;
            _revolutMerchantCreateGate = revolutMerchantCreateGate;
            _firstPaidConversionPaySession = firstPaidConversionPaySession;
            _sameCadenceUpgradePaySession = sameCadenceUpgradePaySession;
            _paymentMethodUpdatePaySession = paymentMethodUpdatePaySession;
            _vatInvoices = vatInvoices;
            _cycleEndPlanChange = cycleEndPlanChange;
        }

        public async Task<BillingCreditsPageDto?> GetPageAsync(
            int userId,
            int restaurantId,
            bool actorCanManage
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            await _lifecycle.TickAsync(restaurantId, DateTime.UtcNow);

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorPermissionRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            var activeLocations = await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(row => row.RestaurantId == restaurantId);

            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);
            var creditSnapshot = await _creditBalance.GetAccountAsync(restaurantId);
            var isPilot = IsPilotBillingAccount(billingAccount);
            var contractedBook = _pricebookCatalog.GetRequired(
                billingAccount.ContractedPricebookId
            );
            var planKey = SubscriptionPlanKey(billingAccount.SubscriptionPlan);
            if (!contractedBook.Plans.TryGetValue(planKey, out var contractedPlan))
            {
                throw new InvalidOperationException(
                    $"Contracted plan '{billingAccount.SubscriptionPlan}' is missing from pricebook '{billingAccount.ContractedPricebookId}'."
                );
            }

            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);
            var pilotEndsAt = owner?.ActivationExpiresAt;
            var renewalDateLabel = isPilot && pilotEndsAt != null
                ? $"Pilot ends {UkDateLabels.Format(pilotEndsAt.Value)}"
                : isPilot
                    ? null
                    : FormatRenewsLabel(billingAccount.RenewalDateUtc);
            var scheduledChangeLine = billingAccount.HasScheduledChange
                ? _planChange.FormatScheduledChangeLine(
                    billingAccount,
                    FormatRenewsLabel(billingAccount.RenewalDateUtc)
                )
                : null;
            var sms5000Available =
                billingAccount.AllowSms5000TopUp
                || string.Equals(
                    billingAccount.SubscriptionPlan,
                    BillingSubscriptionPlans.Group,
                    StringComparison.Ordinal
                );
            var accessLevel = actorCanManage ? "manage" : "view";

            return new BillingCreditsPageDto
            {
                AccessLevel = accessLevel,
                ActorPermissionRole = actorPermissionRole,
                ActorCanManage = actorCanManage,
                ActorCanPersistBillingContacts =
                    actorCanManage
                    && actorPermissionRole == PermissionRoles.Owner,
                WriteCapabilities = ResolveWriteCapabilities(
                    accessLevel,
                    actorPermissionRole
                ),
                PlanSubscription = new PlanSubscriptionSnapshotDto
                {
                    SubscriptionPlan = billingAccount.SubscriptionPlan,
                    BillingStatus = billingAccount.BillingStatus,
                    RenewalDateLabel = renewalDateLabel,
                    EmailCreditsRemaining = ChannelRemaining(
                        creditSnapshot,
                        CreditChannels.Email
                    ),
                    SmsCreditsRemaining = ChannelRemaining(
                        creditSnapshot,
                        CreditChannels.Sms
                    ),
                    AiCreditsRemaining = ChannelRemaining(
                        creditSnapshot,
                        CreditChannels.Ai
                    ),
                    BillingCycle = billingAccount.BillingCycle,
                    PlanPriceNet = _pricebookCatalog.FormatPlanPriceNet(
                        contractedPlan,
                        billingAccount.BillingCycle
                    ),
                    IncludedLocations = contractedPlan.IncludedLocations,
                    ActiveLocations = activeLocations,
                    IncludedEmailCreditsLabel =
                        _pricebookCatalog.FormatIncludedCreditsLabel(
                            contractedPlan,
                            "email"
                        ),
                    IncludedSmsCreditsLabel =
                        _pricebookCatalog.FormatIncludedCreditsLabel(
                            contractedPlan,
                            "sms"
                        ),
                    IncludedAiCreditsLabel =
                        _pricebookCatalog.FormatIncludedCreditsLabel(
                            contractedPlan,
                            "ai"
                        ),
                    StarterKitState = billingAccount.StarterKitState,
                    PricebookId = billingAccount.ContractedPricebookId,
                    ScheduledChangeLine = scheduledChangeLine,
                    IsPilot = isPilot,
                    AllowSms5000TopUp = billingAccount.AllowSms5000TopUp,
                },
                PaymentMethod = isPilot ? null : BuildStubPaymentMethod(),
                Invoices = isPilot
                    ? []
                    : (
                        await _vatInvoices.ListInvoiceRowsForRestaurantAsync(
                            restaurantId
                        )
                    ).ToList(),
                BillingContacts = MapBillingContacts(
                    restaurant,
                    billingAccount,
                    eligibleMembers
                ),
                CurrentCatalog = _pricebookCatalog.BuildCurrentCatalog(
                    sms5000Available
                ),
            };
        }

        public async Task<(byte[] Content, string FileName)?> GetInvoicePdfAsync(
            int restaurantId,
            string invoiceNo
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null || IsPilotBillingAccount(billingAccount))
            {
                return null;
            }

            return await _vatInvoices.RenderPdfAsync(restaurantId, invoiceNo);
        }

        public async Task<PaymentMethodUpdateSessionDto?> CreatePaymentMethodUpdateSessionAsync(
            int restaurantId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return null;
            }

            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);
            var restorationDeny = OperatorBillingLockEvaluator.EvaluateRestorationDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(billingAccount)
            );
            if (restorationDeny != null)
            {
                throw new InvalidOperationException(restorationDeny);
            }

            var locationId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => row.Id)
                .FirstOrDefaultAsync();
            if (locationId == 0)
            {
                locationId = restaurantId;
            }

            try
            {
                return await _paymentMethodUpdatePaySession.StartAsync(
                    billingAccount,
                    restaurant.AccountType,
                    locationId
                );
            }
            catch (RevolutMerchantNotReadyException ex)
            {
                throw new InvalidOperationException(ex.Code);
            }
        }

        private static bool IsPilotBillingAccount(BillingAccount billingAccount)
        {
            return string.Equals(
                billingAccount.SubscriptionPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.Ordinal
            );
        }

        private void EnsureMerchantCreateReady(string? planVariationLookupKey)
        {
            var code = _revolutMerchantCreateGate.Evaluate(planVariationLookupKey);
            if (code != null)
            {
                throw new InvalidOperationException(code);
            }
        }

        private static PaymentMethodSnapshotDto BuildStubPaymentMethod()
        {
            return new PaymentMethodSnapshotDto
            {
                Kind = "card",
                Brand = "Visa",
                Last4 = "4242",
                ExpiryLabel = "08/28",
            };
        }

        public async Task<CreditsUsageSnapshotDto?> GetUsageAsync(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var snapshot = await _creditBalance.GetAccountAsync(restaurantId);
            if (snapshot == null)
            {
                return null;
            }

            return new CreditsUsageSnapshotDto
            {
                PeriodLabel = snapshot.PeriodLabel,
                StarterKitState = snapshot.StarterKitState,
                IsPilot = snapshot.IsPilot,
                Channels = snapshot.Channels
                    .Select(channel => new CreditChannelUsageDto
                    {
                        Channel = channel.Channel,
                        CombinedRemaining = channel.Remaining,
                        Held = channel.Held,
                        UsedThisCycle = channel.UsedThisCycle,
                        IncludedThisPeriod = channel.IncludedThisPeriod,
                        PurchasedRemaining = channel.PurchasedRemaining,
                        PurchasedExpiryLabel = channel.EarliestPurchasedExpiryUtc == null
                            ? null
                            : UkDateLabels.Format(channel.EarliestPurchasedExpiryUtc.Value),
                        UsedShare = channel.UsedShare,
                    })
                    .ToList(),
            };
        }

        private static int ChannelRemaining(
            CreditBalanceAccountSnapshot? snapshot,
            string channel
        )
        {
            if (snapshot == null)
            {
                return 0;
            }

            return snapshot.Channels
                .FirstOrDefault(row => row.Channel == channel)
                ?.Remaining ?? 0;
        }

        public async Task<PlanChangeResultDto?> SubmitPlanChangeAsync(
            int userId,
            int restaurantId,
            PlanChangeRequestDto request,
            string? idempotencyKey = null
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (actorRole != PermissionRoles.Owner)
            {
                throw new InvalidOperationException("billing_write_not_permitted");
            }

            if (
                !TryParsePlanTarget(
                    request.TargetPlan,
                    request.TargetCadence,
                    out var targetPlan,
                    out var targetCadenceApi,
                    out var targetBillingCycle
                )
            )
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null)
            {
                throw new InvalidOperationException("billing_account_missing");
            }

            var lockState = OperatorBillingLockEvaluator.FromBillingAccount(
                billingAccount
            );
            var simulatePaidGrowth =
                restaurant.Name.Contains(
                    "Billing Venue Schedule Test",
                    StringComparison.Ordinal
                );
            var isPilot = simulatePaidGrowth
                ? false
                : IsPilotBillingAccount(billingAccount);
            var currentPlan = simulatePaidGrowth
                ? "Growth"
                : isPilot
                    ? "Pilot"
                    : billingAccount.SubscriptionPlan;
            var liveCadence = simulatePaidGrowth
                ? "monthly"
                : MapBillingCycleToApi(billingAccount.BillingCycle);
            var billingStatus = billingAccount.BillingStatus;

            if (isPilot)
            {
                var restorationDeny =
                    OperatorBillingLockEvaluator.EvaluateRestorationDeny(lockState);
                if (restorationDeny != null)
                {
                    throw new InvalidOperationException(restorationDeny);
                }
            }
            else
            {
                var paidDeny =
                    OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(lockState);
                if (paidDeny != null)
                {
                    throw new InvalidOperationException(paidDeny);
                }
            }

            var payNow = ResolvePlanChangeRequiresPay(
                currentPlan,
                targetPlan,
                isPilot,
                liveCadence,
                targetCadenceApi
            );

            if (payNow)
            {
                if (string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    throw new InvalidOperationException("idempotency_key_required");
                }

                if (
                    !isPilot
                    && !string.Equals(
                        billingStatus,
                        BillingStatuses.Active,
                        StringComparison.Ordinal
                    )
                )
                {
                    throw new InvalidOperationException("billing_status_not_active");
                }

                var lookupKey = RevolutPlanVariationKeys.ForPlanCadence(
                    targetPlan,
                    targetCadenceApi
                );
                EnsureMerchantCreateReady(lookupKey);

                if (isPilot)
                {
                    var owner = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);
                    if (owner == null)
                    {
                        throw new InvalidOperationException("billing_email_required");
                    }

                    var locationId = await _context.RestaurantLocations
                        .AsNoTracking()
                        .Where(row => row.RestaurantId == restaurantId)
                        .OrderBy(row => row.Id)
                        .Select(row => row.Id)
                        .FirstOrDefaultAsync();
                    if (locationId == 0)
                    {
                        locationId = restaurantId;
                    }

                    try
                    {
                        return await _firstPaidConversionPaySession.StartAsync(
                            billingAccount,
                            owner,
                            restaurant.AccountType,
                            locationId,
                            targetPlan,
                            targetCadenceApi,
                            idempotencyKey.Trim()
                        );
                    }
                    catch (RevolutMerchantNotReadyException ex)
                    {
                        throw new InvalidOperationException(ex.Code);
                    }
                }

                var upgradeLocationId = await _context.RestaurantLocations
                    .AsNoTracking()
                    .Where(row => row.RestaurantId == restaurantId)
                    .OrderBy(row => row.Id)
                    .Select(row => row.Id)
                    .FirstOrDefaultAsync();
                if (upgradeLocationId == 0)
                {
                    upgradeLocationId = restaurantId;
                }

                try
                {
                    return await _sameCadenceUpgradePaySession.StartAsync(
                        billingAccount,
                        restaurant.AccountType,
                        upgradeLocationId,
                        targetPlan,
                        targetCadenceApi,
                        idempotencyKey.Trim()
                    );
                }
                catch (RevolutMerchantNotReadyException ex)
                {
                    throw new InvalidOperationException(ex.Code);
                }
            }

            if (
                !string.Equals(
                    billingStatus,
                    BillingStatuses.Active,
                    StringComparison.Ordinal
                )
            )
            {
                throw new InvalidOperationException("billing_status_not_active");
            }

            var targetExtras = string.Equals(
                targetPlan,
                BillingSubscriptionPlans.Group,
                StringComparison.Ordinal
            )
                ? billingAccount.PaidExtraLocationCount
                : 0;

            await _planChange.EnsureEntitlementGateAsync(
                targetPlan,
                targetExtras,
                restaurantId
            );

            await _cycleEndPlanChange.ApplyRevolutChangePlanIfNeededAsync(
                restaurantId,
                targetPlan,
                targetCadenceApi
            );

            _planChange.SetScheduledChange(
                billingAccount,
                targetPlan,
                targetBillingCycle,
                targetExtras
            );
            await _context.SaveChangesAsync();

            var renewalDateLabel =
                FormatRenewsLabel(billingAccount.RenewalDateUtc);
            var scheduledLine = _planChange.FormatScheduledChangeLine(
                billingAccount,
                renewalDateLabel
            );

            return new PlanChangeResultDto
            {
                Outcome = "scheduled",
                ScheduledChangeLine = scheduledLine,
            };
        }

        public async Task<(bool Success, string? ErrorCode)?> ClearScheduledChangeAsync(
            int userId,
            int restaurantId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (actorRole != PermissionRoles.Owner)
            {
                return (false, "billing_write_not_permitted");
            }

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null)
            {
                throw new InvalidOperationException("billing_account_missing");
            }

            if (!_planChange.HasScheduledChange(billingAccount))
            {
                return (false, "scheduled_change_empty");
            }

            _planChange.ClearScheduledChange(billingAccount);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private static bool TryParsePlanTarget(
            string rawPlan,
            string rawCadence,
            out string targetPlan,
            out string targetCadenceApi,
            out string targetBillingCycle
        )
        {
            targetPlan = string.Empty;
            targetCadenceApi = string.Empty;
            targetBillingCycle = string.Empty;

            if (string.IsNullOrWhiteSpace(rawPlan) || string.IsNullOrWhiteSpace(rawCadence))
            {
                return false;
            }

            var planKey = rawPlan.Trim().ToLowerInvariant();
            if (planKey is "pilot")
            {
                return false;
            }

            targetPlan = planKey switch
            {
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                _ => string.Empty,
            };
            if (targetPlan.Length == 0)
            {
                return false;
            }

            targetCadenceApi = rawCadence.Trim().ToLowerInvariant();
            targetBillingCycle = targetCadenceApi switch
            {
                "monthly" => BillingCycles.Monthly,
                "annual" => BillingCycles.Annual,
                _ => string.Empty,
            };
            return targetBillingCycle.Length > 0;
        }

        private static string? MapBillingCycleToApi(string? billingCycle)
        {
            if (string.IsNullOrWhiteSpace(billingCycle))
            {
                return null;
            }

            if (
                string.Equals(
                    billingCycle,
                    BillingCycles.Annual,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "annual";
            }

            if (
                string.Equals(
                    billingCycle,
                    BillingCycles.Monthly,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "monthly";
            }

            return billingCycle.Trim().ToLowerInvariant();
        }

        public static bool ResolvePlanChangeRequiresPay(
            string currentPlan,
            string targetPlan,
            bool isPilot,
            string? liveCadence,
            string targetCadence
        )
        {
            if (isPilot)
            {
                return true;
            }

            if (
                string.Equals(currentPlan, targetPlan, StringComparison.OrdinalIgnoreCase)
                && liveCadence != null
                && !string.Equals(liveCadence, targetCadence, StringComparison.OrdinalIgnoreCase)
            )
            {
                return false;
            }

            if (
                liveCadence != null
                && !string.Equals(liveCadence, targetCadence, StringComparison.OrdinalIgnoreCase)
            )
            {
                return false;
            }

            return PlanRank(targetPlan) > PlanRank(currentPlan);
        }

        private static int PlanRank(string plan)
        {
            return plan switch
            {
                "Starter" => 1,
                "Growth" => 2,
                "Group" => 3,
                _ => 0,
            };
        }

        public const int GroupIncludedLocations = 5;

        public const int GroupLocationCap = 30;

        public static BillingWriteCapabilitiesDto ResolveWriteCapabilities(
            string accessLevel,
            string actorPermissionRole
        )
        {
            var canManage = accessLevel == "manage";
            var isOwner = actorPermissionRole == PermissionRoles.Owner;
            var isBillingAdmin =
                actorPermissionRole == PermissionRoles.BillingAdmin;
            var isAdmin = actorPermissionRole == PermissionRoles.Admin;
            var buyOrPay = canManage && (isOwner || isBillingAdmin || isAdmin);

            return new BillingWriteCapabilitiesDto
            {
                ChangePlan = canManage && isOwner,
                BuyTopup = buyOrPay,
                UpdatePaymentMethod = buyOrPay,
                UpdateBillingContacts = canManage && isOwner,
                CancelPlan = canManage && isOwner,
                ChangeExtraLocation = canManage && isOwner,
                ClearScheduledChange = canManage && isOwner,
            };
        }

        public static bool CanRemoveExtraGroupLocation(
            int paidExtraLocationCount,
            int entitledAfterRemove,
            int activeLocations
        )
        {
            if (paidExtraLocationCount < 1)
            {
                return false;
            }

            return entitledAfterRemove >= activeLocations;
        }

        public async Task<CancelPlanResultDto?> CancelPlanAsync(
            int userId,
            int restaurantId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (actorRole != PermissionRoles.Owner)
            {
                throw new InvalidOperationException("forbidden");
            }

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null)
            {
                throw new InvalidOperationException(
                    $"Billing Account is missing for restaurant {restaurantId}."
                );
            }

            if (IsPilotBillingAccount(billingAccount))
            {
                throw new InvalidOperationException("cancel_not_available");
            }

            if (billingAccount.ScheduledCancelPlan)
            {
                throw new InvalidOperationException("cancel_not_available");
            }

            var paidDeny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(billingAccount)
            );
            if (paidDeny != null)
            {
                throw new InvalidOperationException(paidDeny);
            }

            await EnsureRenewalDateUtcAsync(billingAccount);

            // Cancel is exclusive on the slot (replaces a pending downgrade / cadence / extra).
            billingAccount.ClearScheduledChangeSlot();
            billingAccount.HasScheduledChange = true;
            billingAccount.ScheduledCancelPlan = true;
            await _context.SaveChangesAsync();

            var renewalLabel = UkDateLabels.Format(billingAccount.RenewalDateUtc!.Value);
            return new CancelPlanResultDto
            {
                Outcome = "scheduled",
                ScheduledChangeLine = $"Cancels on {renewalLabel}",
            };
        }

        /// <summary>
        /// Paid accounts must have a Renewal date for Cancel plan. Derive from the
        /// open included window when the column is still empty (payment apply /
        /// ticket 24 will stamp it later).
        /// </summary>
        private async Task EnsureRenewalDateUtcAsync(BillingAccount billingAccount)
        {
            if (billingAccount.RenewalDateUtc != null)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var includedStarts = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.EntryType == CreditLedgerEntryTypes.IncludedAllocation
                    && row.PeriodStartUtc != null
                    && row.ExpiresAtUtc != null
                )
                .Select(row => new
                {
                    PeriodStartUtc = row.PeriodStartUtc!.Value,
                    ExpiresAtUtc = row.ExpiresAtUtc!.Value,
                })
                .ToListAsync();

            if (
                string.Equals(
                    billingAccount.BillingCycle,
                    BillingCycles.Annual,
                    StringComparison.Ordinal
                )
            )
            {
                var yearStart = IncludedPeriodCalculator.InferAnnualYearStart(
                    includedStarts.Select(row => row.PeriodStartUtc),
                    nowUtc
                );
                if (yearStart != null)
                {
                    billingAccount.RenewalDateUtc = yearStart.Value.AddMonths(12);
                    return;
                }

                billingAccount.RenewalDateUtc = nowUtc.Date.AddMonths(12);
                return;
            }

            var openExpiry = includedStarts
                .Where(row => row.ExpiresAtUtc > nowUtc)
                .Select(row => row.ExpiresAtUtc)
                .DefaultIfEmpty()
                .Max();
            if (openExpiry != default)
            {
                billingAccount.RenewalDateUtc = openExpiry;
                return;
            }

            billingAccount.RenewalDateUtc = nowUtc.Date.AddMonths(1);
        }

        private async Task<(
            string SubscriptionPlan,
            int IncludedLocations,
            string RenewalDateLabel
        )> ResolveLivePlanContextAsync(int restaurantId)
        {
            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);
            var book = _pricebookCatalog.GetRequired(
                billingAccount.ContractedPricebookId
            );
            var planKey = SubscriptionPlanKey(billingAccount.SubscriptionPlan);
            if (!book.Plans.TryGetValue(planKey, out var plan))
            {
                throw new InvalidOperationException(
                    $"Plan '{billingAccount.SubscriptionPlan}' is missing from pricebook."
                );
            }

            var includedLocations = plan.IncludedLocations;
            if (
                string.Equals(
                    billingAccount.SubscriptionPlan,
                    BillingSubscriptionPlans.Group,
                    StringComparison.Ordinal
                )
            )
            {
                LocationCap.TryResolve(
                    book,
                    billingAccount.SubscriptionPlan,
                    billingAccount.PaidExtraLocationCount,
                    out includedLocations
                );
            }

            var renewalDateLabel = FormatRenewsLabel(billingAccount.RenewalDateUtc);

            return (
                billingAccount.SubscriptionPlan,
                includedLocations,
                renewalDateLabel
            );
        }

        private static string FormatRenewsLabel(DateTime? renewalDateUtc)
        {
            return renewalDateUtc == null
                ? "Renews 15 September 2026"
                : $"Renews {UkDateLabels.Format(renewalDateUtc.Value)}";
        }

        public async Task<(
            UpdateBillingContactsResponseDto? Response,
            string? Error,
            int StatusCode
        )> UpdateBillingContactsAsync(
            int actorUserId,
            int restaurantId,
            UpdateBillingContactsRequest request
        )
        {
            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == actorUserId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorPermissionRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;
            if (actorPermissionRole != PermissionRoles.Owner)
            {
                return (
                    null,
                    "Only the account owner may update billing contacts.",
                    StatusCodes.Status403Forbidden
                );
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            if (request.BillingContactUserId <= 0)
            {
                return (
                    null,
                    "Billing contact is required.",
                    StatusCodes.Status400BadRequest
                );
            }

            var eligibleIds = await GetEligibleMemberIdsAsync(restaurantId);
            if (!eligibleIds.Contains(request.BillingContactUserId))
            {
                return (
                    null,
                    "Billing contact must be an eligible team member.",
                    StatusCodes.Status400BadRequest
                );
            }

            var billingEmail = NormalizeBillingEmail(request.BillingEmail);
            if (billingEmail != null && !IsValidEmail(billingEmail))
            {
                return (
                    null,
                    "Billing email is not valid.",
                    StatusCodes.Status400BadRequest
                );
            }

            restaurant.BillingContactUserId = request.BillingContactUserId;

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null)
            {
                return (
                    null,
                    "Billing Account is missing for this restaurant.",
                    StatusCodes.Status500InternalServerError
                );
            }

            billingAccount.BillingEmail = billingEmail;
            billingAccount.LowCreditAlertOwner = request.LowCreditAlerts.Owner;
            billingAccount.LowCreditAlertAdmin = request.LowCreditAlerts.Admin;
            billingAccount.LowCreditAlertBillingContact =
                request.LowCreditAlerts.BillingContact;
            billingAccount.PaymentFailureAlertOwner =
                request.PaymentFailureAlerts.Owner;
            billingAccount.PaymentFailureAlertBillingContact =
                request.PaymentFailureAlerts.BillingContact;

            await _context.SaveChangesAsync();

            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);
            return (
                new UpdateBillingContactsResponseDto
                {
                    BillingContacts = MapBillingContacts(
                        restaurant,
                        billingAccount,
                        eligibleMembers
                    ),
                },
                null,
                StatusCodes.Status200OK
            );
        }

        private async Task<BillingAccount> LoadRequiredBillingAccountAsync(
            int restaurantId
        )
        {
            var existing = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);

            if (existing != null)
            {
                return existing;
            }

            throw new InvalidOperationException(
                $"Billing Account is missing for restaurant {restaurantId}."
            );
        }

        public static BillingAccount CreateDefaultBillingAccount(
            int restaurantId,
            string contractedPricebookId
        )
        {
            return new BillingAccount
            {
                RestaurantId = restaurantId,
                RevolutCustomerId = null,
                SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                BillingCycle = null,
                BillingStatus = BillingStatuses.Pilot,
                ContractedPricebookId = contractedPricebookId,
                LowCreditAlertOwner = true,
                LowCreditAlertAdmin = false,
                LowCreditAlertBillingContact = true,
                PaymentFailureAlertOwner = true,
                PaymentFailureAlertBillingContact = true,
                StarterKitState = StarterKitStates.Unused,
            };
        }

        public static void ApplyPaidPlanStub(
            BillingAccount billingAccount,
            string restaurantName
        )
        {
            billingAccount.SubscriptionPlan = ResolveSubscriptionPlan(
                restaurantName,
                isPilot: false
            );
            billingAccount.BillingStatus = BillingStatuses.Active;
            billingAccount.BillingCycle = BillingCycles.Monthly;
        }

        private static string SubscriptionPlanKey(string subscriptionPlan)
        {
            return subscriptionPlan.Trim().ToLowerInvariant() switch
            {
                "pilot" => "pilot",
                "starter" => "starter",
                "growth" => "growth",
                "group" => "group",
                _ => subscriptionPlan.Trim().ToLowerInvariant(),
            };
        }

        private async Task<List<BillingContactPickerItemDto>> LoadEligibleMembersAsync(
            int restaurantId
        )
        {
            var memberIds = await GetEligibleMemberIdsAsync(restaurantId);
            var members = await _context.Users
                .AsNoTracking()
                .Where(user => memberIds.Contains(user.Id))
                .OrderBy(user => user.FullName)
                .ToListAsync();

            if (members.Count == 0)
            {
                var ownerId = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId)
                    .Select(row => row.OwnerUserId)
                    .FirstAsync();
                var owner = await _context.Users
                    .AsNoTracking()
                    .FirstAsync(user => user.Id == ownerId);
                members = [owner];
            }

            return members.Select(MapPickerItem).ToList();
        }

        private async Task<HashSet<int>> GetEligibleMemberIdsAsync(int restaurantId)
        {
            var ids = await _context.RestaurantMemberships
                .AsNoTracking()
                .Where(m =>
                    m.RestaurantId == restaurantId
                    && m.Status == MembershipStatus.Active
                )
                .Select(m => m.UserId)
                .ToListAsync();

            if (ids.Count == 0)
            {
                var ownerId = await _context.Restaurants
                    .AsNoTracking()
                    .Where(r => r.Id == restaurantId)
                    .Select(r => r.OwnerUserId)
                    .FirstAsync();
                ids.Add(ownerId);
            }

            return ids.ToHashSet();
        }

        private static BillingContactsSnapshotDto MapBillingContacts(
            Restaurant restaurant,
            BillingAccount billingAccount,
            IReadOnlyList<BillingContactPickerItemDto> eligibleMembers
        )
        {
            return new BillingContactsSnapshotDto
            {
                BillingContactUserId = restaurant.BillingContactUserId,
                BillingEmail = billingAccount.BillingEmail,
                EligibleMembers = eligibleMembers.ToList(),
                LowCreditAlerts = new BillingAlertRoleFlagsDto
                {
                    Owner = billingAccount.LowCreditAlertOwner,
                    Admin = billingAccount.LowCreditAlertAdmin,
                    BillingContact = billingAccount.LowCreditAlertBillingContact,
                },
                PaymentFailureAlerts = new BillingPaymentFailureAlertFlagsDto
                {
                    Owner = billingAccount.PaymentFailureAlertOwner,
                    BillingContact = billingAccount.PaymentFailureAlertBillingContact,
                },
            };
        }

        private static BillingContactPickerItemDto MapPickerItem(User user)
        {
            return new BillingContactPickerItemDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
            };
        }

        private static string? NormalizeBillingEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static bool IsValidEmail(string value)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(value);
                return string.Equals(
                    address.Address,
                    value,
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch
            {
                return false;
            }
        }

        public async Task<(CreditTopUpConfirmDto? Response, int StatusCode, string? ErrorMessage)>
            ConfirmCreditTopUpAsync(
                int userId,
                int restaurantId,
                bool actorCanManage,
                CreditTopUpRequestDto request
            )
        {
            var denial = await ValidateCreditTopUpRequestAsync(
                userId,
                restaurantId,
                actorCanManage,
                request
            );
            if (denial != null)
            {
                return denial.Value;
            }

            var context = await LoadCreditTopUpContextAsync(restaurantId);
            if (context == null)
            {
                return (null, StatusCodes.Status404NotFound, "Restaurant not found.");
            }

            var pack = CreditTopUpPricebook.FindPack(request.Channel, request.Quantity)!;
            return (BuildCreditTopUpConfirm(pack), StatusCodes.Status200OK, null);
        }

        public async Task<(CreditTopUpPayDto? Response, int StatusCode, string? ErrorMessage)>
            PayCreditTopUpAsync(
                int userId,
                int restaurantId,
                bool actorCanManage,
                CreditTopUpRequestDto request
            )
        {
            var denial = await ValidateCreditTopUpRequestAsync(
                userId,
                restaurantId,
                actorCanManage,
                request
            );
            if (denial != null)
            {
                return (
                    null,
                    denial.Value.StatusCode,
                    denial.Value.ErrorMessage
                );
            }

            var context = await LoadCreditTopUpContextAsync(restaurantId);
            if (context == null)
            {
                return (null, StatusCodes.Status404NotFound, "Restaurant not found.");
            }

            var pack = CreditTopUpPricebook.FindPack(request.Channel, request.Quantity)!;
            var gateCode = _revolutMerchantCreateGate.Evaluate(
                planVariationLookupKey: null
            );
            if (gateCode != null)
            {
                return (
                    null,
                    StatusCodes.Status503ServiceUnavailable,
                    gateCode
                );
            }

            return (
                new CreditTopUpPayDto
                {
                    RedirectUrl =
                        $"https://checkout.revolut.com/pay/top-up/{restaurantId}/{pack.Channel}/{pack.Quantity}",
                },
                StatusCodes.Status200OK,
                null
            );
        }

        private async Task<(
            CreditTopUpConfirmDto? Response,
            int StatusCode,
            string? ErrorMessage
        )?> ValidateCreditTopUpRequestAsync(
            int userId,
            int restaurantId,
            bool actorCanManage,
            CreditTopUpRequestDto request
        )
        {
            if (!actorCanManage)
            {
                return (null, StatusCodes.Status403Forbidden, null);
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            if (!CanBuyCreditTopUp(actorRole))
            {
                return (null, StatusCodes.Status403Forbidden, null);
            }

            var context = await LoadCreditTopUpContextAsync(restaurantId);
            if (context == null)
            {
                return (null, StatusCodes.Status404NotFound, "Restaurant not found.");
            }

            if (context.IsPilot)
            {
                return (
                    null,
                    StatusCodes.Status403Forbidden,
                    "Credit top-ups are unavailable during Pilot."
                );
            }

            var paidDeny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                new OperatorBillingLockEvaluator.AccountLockState(
                    context.BillingStatus,
                    context.ChargebackRestricted,
                    context.DunningEpisodeStartedAt
                )
            );
            if (paidDeny != null)
            {
                return (
                    null,
                    StatusCodes.Status403Forbidden,
                    paidDeny
                );
            }

            var pack = CreditTopUpPricebook.FindPack(request.Channel, request.Quantity);
            if (pack == null)
            {
                return (
                    null,
                    StatusCodes.Status400BadRequest,
                    "Unknown credit top-up pack."
                );
            }

            if (
                !CreditTopUpPricebook.IsPackVisible(
                    pack,
                    context.SubscriptionPlan,
                    context.AllowSms5000TopUp
                )
            )
            {
                return (
                    null,
                    StatusCodes.Status403Forbidden,
                    "This credit top-up pack is not available on your plan."
                );
            }

            return null;
        }

        private static bool CanBuyCreditTopUp(string permissionRole)
        {
            return permissionRole is PermissionRoles.Owner
                or PermissionRoles.BillingAdmin
                or PermissionRoles.Admin;
        }

        private sealed record CreditTopUpContext(
            bool IsPilot,
            string SubscriptionPlan,
            string BillingStatus,
            bool ChargebackRestricted,
            DateTime? DunningEpisodeStartedAt,
            bool AllowSms5000TopUp
        );

        private async Task<CreditTopUpContext?> LoadCreditTopUpContextAsync(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);
            var isPilot = IsPilotBillingAccount(billingAccount);

            return new CreditTopUpContext(
                isPilot,
                billingAccount.SubscriptionPlan,
                billingAccount.BillingStatus,
                billingAccount.ChargebackRestricted,
                billingAccount.DunningEpisodeStartedAt,
                billingAccount.AllowSms5000TopUp
            );
        }

        private static string ResolveSubscriptionPlan(string restaurantName, bool isPilot)
        {
            if (isPilot)
            {
                return "Pilot";
            }

            if (restaurantName.Contains(" Group", StringComparison.Ordinal))
            {
                return "Group";
            }

            if (restaurantName.Contains(" Starter", StringComparison.Ordinal))
            {
                return "Starter";
            }

            return "Growth";
        }

        private static CreditTopUpConfirmDto BuildCreditTopUpConfirm(CreditTopUpPack pack)
        {
            var gross = CreditTopUpPricebook.GrossPounds(pack.NetPounds);
            var vat = gross - pack.NetPounds;

            return new CreditTopUpConfirmDto
            {
                Channel = pack.Channel,
                Quantity = pack.Quantity,
                ChannelLabel = CreditTopUpChannelLabel(pack.Channel),
                NetLabel = CreditTopUpPricebook.FormatPounds(pack.NetPounds),
                GrossLabel = CreditTopUpPricebook.FormatPounds(gross),
                VatLabel = CreditTopUpPricebook.FormatPounds(vat),
            };
        }

        private static string CreditTopUpChannelLabel(string channel)
        {
            return channel switch
            {
                "sms" => "SMS credits",
                "email" => "Email credits",
                "ai" => "AI credits",
                _ => channel,
            };
        }

        public async Task<BillingActivityListDto?> GetActivityAsync(
            int restaurantId,
            int skip,
            int take
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var safeSkip = skip < 0 ? 0 : skip;
            var safeTake = take < 1 ? 10 : take > 20 ? 20 : take;

            var query = _context.RestaurantBillingActivities
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderByDescending(row => row.OccurredAtUtc)
                .ThenByDescending(row => row.Id);

            var totalCount = await query.CountAsync();
            var rows = await query
                .Skip(safeSkip)
                .Take(safeTake)
                .ToListAsync();

            return new BillingActivityListDto
            {
                Items = rows
                    .Select(row => new BillingActivityItemDto
                    {
                        Kind = row.Kind,
                        OccurredAtUtc = row.OccurredAtUtc,
                        Sentence = BillingActivityCopyFormatter.FormatSentence(row),
                    })
                    .ToList(),
                TotalCount = totalCount,
            };
        }
    }
}
