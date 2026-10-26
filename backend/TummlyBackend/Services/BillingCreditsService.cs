using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
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
        private readonly ICreditTopUpPaySession _creditTopUpPaySession;
        private readonly ITummlyVatInvoiceService _vatInvoices;
        private readonly ICycleEndPlanChange _cycleEndPlanChange;
        private readonly TummlySellerVatSettings _sellerVat;
        private readonly CustomPlanCycleBillingService _customPlanCycleBilling;
        private readonly IConfiguration _configuration;
        private readonly IPilotStartService _pilotStart;

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
            ICreditTopUpPaySession creditTopUpPaySession,
            ITummlyVatInvoiceService vatInvoices,
            ICycleEndPlanChange cycleEndPlanChange,
            IOptions<TummlySellerVatSettings> sellerVat,
            CustomPlanCycleBillingService customPlanCycleBilling,
            IConfiguration configuration,
            IPilotStartService pilotStart
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
            _creditTopUpPaySession = creditTopUpPaySession;
            _vatInvoices = vatInvoices;
            _cycleEndPlanChange = cycleEndPlanChange;
            _sellerVat = sellerVat.Value;
            _customPlanCycleBilling = customPlanCycleBilling;
            _configuration = configuration;
            _pilotStart = pilotStart;
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
            var isCustom = string.Equals(
                billingAccount.SubscriptionPlan,
                BillingSubscriptionPlans.Custom,
                StringComparison.Ordinal
            );
            var contractedBook = _pricebookCatalog.GetRequired(
                billingAccount.ContractedPricebookId
            );
            PricebookPlan? contractedPlan = null;
            if (!isCustom)
            {
                var planKey = SubscriptionPlanKey(billingAccount.SubscriptionPlan);
                if (!contractedBook.Plans.TryGetValue(planKey, out contractedPlan))
                {
                    throw new InvalidOperationException(
                        $"Contracted plan '{billingAccount.SubscriptionPlan}' is missing from pricebook '{billingAccount.ContractedPricebookId}'."
                    );
                }
            }

            var customConfig = isCustom
                ? CustomPlanConfig.TryParse(billingAccount.CustomPlanConfigJson)
                : null;
            var openCustomCycleCheckoutUrl = isCustom
                ? await ResolveOpenCustomCycleCheckoutUrlAsync(restaurantId)
                : null;

            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);
            var pilotEndsAt =
                billingAccount.PilotPeriodEnd ?? owner?.ActivationExpiresAt;
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

            var planSubscription = new PlanSubscriptionSnapshotDto
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
                PlanPriceNet = isCustom
                    ? FormatPoundsFromPence(customConfig?.PeriodNetPence ?? 0)
                    : _pricebookCatalog.FormatPlanPriceNet(
                        contractedPlan!,
                        billingAccount.BillingCycle
                    ),
                IncludedLocations = isCustom
                    ? customConfig?.Quantities.Locations ?? 0
                    : contractedPlan!.IncludedLocations,
                ActiveLocations = activeLocations,
                IncludedEmailCreditsLabel = isCustom
                    ? FormatCreditsCountLabel(customConfig?.Quantities.EmailCredits ?? 0)
                    : _pricebookCatalog.FormatIncludedCreditsLabel(
                        contractedPlan!,
                        "email"
                    ),
                IncludedSmsCreditsLabel = isCustom
                    ? FormatCreditsCountLabel(customConfig?.Quantities.SmsCredits ?? 0)
                    : _pricebookCatalog.FormatIncludedCreditsLabel(
                        contractedPlan!,
                        "sms"
                    ),
                IncludedAiCreditsLabel = isCustom
                    ? FormatCreditsCountLabel(customConfig?.Quantities.AiCredits ?? 0)
                    : _pricebookCatalog.FormatIncludedCreditsLabel(
                        contractedPlan!,
                        "ai"
                    ),
                StarterKitState = billingAccount.StarterKitState,
                PricebookId = billingAccount.ContractedPricebookId,
                ScheduledChangeLine = scheduledChangeLine,
                IsPilot = isPilot,
                AllowSms5000TopUp = billingAccount.AllowSms5000TopUp,
                OpenCustomCycleCheckoutUrl = openCustomCycleCheckoutUrl,
                PendingPayment = billingAccount.PendingPaymentPlan == null
                    ? null
                    : new PendingPaymentSnapshotDto
                    {
                        Plan = billingAccount.PendingPaymentPlan,
                        Cadence = billingAccount.PendingPaymentCadence ?? "monthly",
                        FailedAtUtc = billingAccount.PendingPaymentFailedAtUtc,
                    },
                CustomPlan = customConfig == null
                    ? null
                    : new CustomPlanSnapshotDto
                    {
                        Cadence = customConfig.Cadence,
                        PeriodNetPence = customConfig.PeriodNetPence,
                        Locations = customConfig.Quantities.Locations,
                        TeamUsers = customConfig.Quantities.TeamUsers,
                        QrPlacements = customConfig.Quantities.QrPlacements,
                        Offers = customConfig.Quantities.Offers,
                        EmailCredits = customConfig.Quantities.EmailCredits,
                        SmsCredits = customConfig.Quantities.SmsCredits,
                        AiCredits = customConfig.Quantities.AiCredits,
                    },
            };

            var page = new BillingCreditsPageDto
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
                PlanSubscription = planSubscription,
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
                VatModeActive = _sellerVat.IsActive,
            };
            page.CurrentCatalog.VatRateBps = _sellerVat.EffectiveVatRateBps;
            return page;
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

        private static bool IsFreeBillingAccount(BillingAccount billingAccount)
        {
            return string.Equals(
                billingAccount.SubscriptionPlan,
                BillingSubscriptionPlans.Free,
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
            var isFree = !simulatePaidGrowth && IsFreeBillingAccount(billingAccount);
            var currentPlan = simulatePaidGrowth
                ? "Growth"
                : isPilot
                    ? "Pilot"
                    : billingAccount.SubscriptionPlan;
            var liveCadence = simulatePaidGrowth
                ? "monthly"
                : MapBillingCycleToApi(billingAccount.BillingCycle);
            var billingStatus = billingAccount.BillingStatus;

            if (
                string.Equals(
                    targetPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                )
                && !isFree
            )
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

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

            if (
                isFree
                && string.Equals(
                    targetPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                )
            )
            {
                var pilotStart = await _pilotStart.StartPilotAsync(restaurantId);
                if (pilotStart.RefusedCode != null)
                {
                    throw new InvalidOperationException(pilotStart.RefusedCode);
                }

                await _firstPaidConversionPaySession.AbandonOpenSessionsAsync(
                    restaurantId
                );
                await _pilotStart.NotifyStartedAsync(restaurantId, pilotStart);

                return new PlanChangeResultDto { Outcome = "applied" };
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
                    && !isFree
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

                if (isPilot || isFree)
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

            _planChange.SetScheduledChange(
                billingAccount,
                targetPlan,
                targetBillingCycle,
                targetExtras
            );

            await _cycleEndPlanChange.ApplyRevolutChangePlanIfNeededAsync(
                restaurantId,
                targetPlan,
                targetCadenceApi
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

        public async Task<bool> ContinuePendingPaymentOnFreeAsync(int restaurantId)
        {
            var hadOpen = await _context.RevolutPendingPaySessions.AnyAsync(row =>
                row.RestaurantId == restaurantId && row.IsOpen
            );
            if (!hadOpen)
            {
                return false;
            }

            await _firstPaidConversionPaySession.AbandonOpenSessionsAsync(
                restaurantId
            );
            return true;
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

            // Inside the lead window the job may already have cancelled the
            // Revolut subscription, which cannot be undone.
            if (
                billingAccount.ScheduledCancelPlan
                && billingAccount.RenewalDateUtc != null
                && DateTime.UtcNow
                    >= billingAccount.RenewalDateUtc.Value
                        - IncludedPeriodMintService.RevolutCancelLead
            )
            {
                return (false, "cancel_already_sent");
            }

            // Revolut already holds the scheduled variation (posted on submit).
            // Re-point it at the live plan first; on failure keep the slot so
            // Tummly and Revolut still agree.
            if (ScheduledChangeMovedRevolutPlan(billingAccount))
            {
                var liveCadenceApi =
                    MapBillingCycleToApi(billingAccount.BillingCycle) ?? "monthly";
                try
                {
                    await _cycleEndPlanChange.RestoreLiveRevolutPlanIfNeededAsync(
                        restaurantId,
                        billingAccount.SubscriptionPlan,
                        liveCadenceApi,
                        billingAccount.PaidExtraLocationCount
                    );
                }
                catch (InvalidOperationException ex)
                {
                    return (false, ex.Message);
                }
            }

            _planChange.ClearScheduledChange(billingAccount);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        /// <summary>
        /// True when Revolut may hold a pending change-plan: plan or cadence
        /// change, or a Group extra remove down to 0 extras. A scheduled
        /// cancel sends nothing to Revolut, but it replaces the slot of an
        /// earlier downgrade whose change-plan Revolut may still hold. The
        /// restore skips when Revolut has nothing pending.
        /// </summary>
        private static bool ScheduledChangeMovedRevolutPlan(BillingAccount billingAccount)
        {
            if (billingAccount.ScheduledCancelPlan)
            {
                return true;
            }

            var targetPlan = billingAccount.ScheduledTargetSubscriptionPlan
                ?? billingAccount.SubscriptionPlan;
            var targetCycle = billingAccount.ScheduledTargetBillingCycle
                ?? billingAccount.BillingCycle;
            if (
                !string.Equals(
                    targetPlan,
                    billingAccount.SubscriptionPlan,
                    StringComparison.OrdinalIgnoreCase
                )
                || !string.Equals(
                    targetCycle,
                    billingAccount.BillingCycle,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return true;
            }

            return billingAccount.PaidExtraLocationCount > 0
                && billingAccount.ScheduledTargetExtraLocationCount == 0;
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
            targetPlan = planKey switch
            {
                "pilot" => BillingSubscriptionPlans.Pilot,
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
            if (
                string.Equals(
                    targetPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.Ordinal
                )
            )
            {
                // Pilot has no paid cadence; accept monthly as the API default.
                if (targetCadenceApi is not ("monthly" or "annual"))
                {
                    return false;
                }

                targetBillingCycle = BillingCycles.Monthly;
                return true;
            }

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
                string.Equals(
                    currentPlan,
                    BillingSubscriptionPlans.Free,
                    StringComparison.OrdinalIgnoreCase
                )
                && string.Equals(
                    targetPlan,
                    BillingSubscriptionPlans.Pilot,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return false;
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
                "Free" => 0,
                "Pilot" => 0,
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
            int restaurantId,
            CancelPlanRequestDto request
        )
        {
            if (
                string.IsNullOrWhiteSpace(request.Reason)
                || !CancelPlanReasons.Allowed.Contains(request.Reason)
            )
            {
                throw new InvalidOperationException("cancel_reason_required");
            }

            var additionalNotes = request.AdditionalNotes?.Trim();
            if (additionalNotes?.Length > 500)
            {
                throw new InvalidOperationException("cancel_notes_too_long");
            }
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
            billingAccount.ScheduledCancelReason = request.Reason;
            billingAccount.ScheduledCancelNotes =
                string.IsNullOrWhiteSpace(additionalNotes) ? null : additionalNotes;

            // No Revolut call today (ticket 23): the subscription stays active.
            // The included-period job cancels it natively shortly before
            // renewal, so undoing this cancel needs no Revolut call.

            var renewalLabel = UkDateLabels.Format(billingAccount.RenewalDateUtc!.Value);
            var actorName = await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == userId)
                .Select(row => row.FullName)
                .FirstOrDefaultAsync();
            BillingActivityWriter.TryAppend(
                _context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = restaurantId,
                    Kind = BillingActivityKinds.SubscriptionCancelled,
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorDisplayName = string.IsNullOrWhiteSpace(actorName)
                        ? null
                        : actorName.Trim(),
                    ScheduledDateLabel = renewalLabel,
                    Plan = billingAccount.SubscriptionPlan,
                }
            );

            await _context.SaveChangesAsync();

            return new CancelPlanResultDto
            {
                Outcome = "scheduled",
                ScheduledChangeLine = $"Cancels on {renewalLabel}",
            };
        }

        public async Task<(CustomPlanCyclePayDto? Response, int StatusCode, string? ErrorMessage)>
            PayCustomPlanCycleAsync(
                int userId,
                int restaurantId,
                bool actorCanManage
            )
        {
            if (!actorCanManage)
            {
                return (null, StatusCodes.Status403Forbidden, "forbidden");
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return (null, StatusCodes.Status404NotFound, "restaurant_not_found");
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null)
            {
                return (null, StatusCodes.Status404NotFound, "billing_account_missing");
            }

            if (
                !string.Equals(
                    billingAccount.SubscriptionPlan,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.Ordinal
                )
            )
            {
                return (null, StatusCodes.Status400BadRequest, "not_custom_plan");
            }

            if (billingAccount.ScheduledCancelPlan)
            {
                return (null, StatusCodes.Status400BadRequest, "cancel_scheduled");
            }

            var paidDeny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(billingAccount)
            );
            // SoftLock / PastDue Custom may still resume the outstanding cycle pay.
            if (
                paidDeny != null
                && paidDeny != OperatorBillingLockEvaluator.SoftLock
                && paidDeny != OperatorBillingLockEvaluator.PastDueSendsBlocked
            )
            {
                return (null, StatusCodes.Status403Forbidden, paidDeny);
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

            string redirectUrl;
            try
            {
                redirectUrl = RevolutHostedCheckoutRedirectUrls.BuildBillingCreditsTabUrl(
                    _configuration,
                    restaurant.AccountType,
                    locationId,
                    "plan-subscription",
                    new Dictionary<string, string>
                    {
                        ["customCycleOutcome"] = "success",
                    }
                );
            }
            catch (InvalidOperationException ex)
                when (string.Equals(
                    ex.Message,
                    RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode,
                    StringComparison.Ordinal
                ))
            {
                return (
                    null,
                    StatusCodes.Status400BadRequest,
                    RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode
                );
            }

            var openCheckout = await ResolveOpenCustomCycleCheckoutUrlAsync(
                restaurantId
            );
            if (!string.IsNullOrWhiteSpace(openCheckout))
            {
                return (
                    new CustomPlanCyclePayDto { CheckoutUrl = openCheckout },
                    StatusCodes.Status200OK,
                    null
                );
            }

            var renewalKey = billingAccount.RenewalDateUtc ?? DateTime.UtcNow;
            var idempotencyKey =
                $"custom-cycle-{restaurantId}-{renewalKey:yyyyMMdd}";

            var (checkoutUrl, error) = await _customPlanCycleBilling.StartCyclePayAsync(
                restaurantId,
                redirectUrl,
                idempotencyKey
            );
            if (error != null || string.IsNullOrWhiteSpace(checkoutUrl))
            {
                return (
                    null,
                    StatusCodes.Status502BadGateway,
                    error ?? "custom_cycle_start_failed"
                );
            }

            return (
                new CustomPlanCyclePayDto { CheckoutUrl = checkoutUrl },
                StatusCodes.Status200OK,
                null
            );
        }

        /// <summary>
        /// Retry checkout for a paid plan that did not start at signup or on an
        /// earlier attempt. The account stays on Free until Revolut confirms payment.
        /// </summary>
        public async Task<(PendingPaymentRetryDto? Response, int StatusCode, string? ErrorMessage)>
            RetryPendingPaymentAsync(
                int userId,
                int restaurantId,
                bool actorCanManage
            )
        {
            if (!actorCanManage)
            {
                return (null, StatusCodes.Status403Forbidden, "forbidden");
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return (null, StatusCodes.Status404NotFound, "restaurant_not_found");
            }

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
            if (billingAccount == null)
            {
                return (null, StatusCodes.Status404NotFound, "billing_account_missing");
            }

            if (billingAccount.PendingPaymentPlan == null)
            {
                return (null, StatusCodes.Status404NotFound, "no_pending_payment");
            }

            if (!IsFreeBillingAccount(billingAccount))
            {
                return (null, StatusCodes.Status400BadRequest, "pending_payment_not_applicable");
            }

            var paidDeny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(billingAccount)
            );
            if (paidDeny != null)
            {
                return (null, StatusCodes.Status403Forbidden, paidDeny);
            }

            var pendingPlan = billingAccount.PendingPaymentPlan;
            var cadence = billingAccount.PendingPaymentCadence ?? "monthly";
            string code;
            try
            {
                if (
                    string.Equals(
                        pendingPlan,
                        BillingSubscriptionPlans.Custom,
                        StringComparison.Ordinal
                    )
                )
                {
                    var quote = CustomPlanConfig.TryParse(
                        billingAccount.PendingPaymentCustomConfigJson
                    );
                    if (quote == null || quote.PeriodNetPence <= 0)
                    {
                        return (null, StatusCodes.Status400BadRequest, "custom_plan_config_invalid");
                    }

                    var customLocationId = await ResolveFirstLocationIdAsync(restaurantId);
                    string customRedirectUrl;
                    try
                    {
                        customRedirectUrl = RevolutHostedCheckoutRedirectUrls.BuildBillingCreditsTabUrl(
                            _configuration,
                            restaurant.AccountType,
                            customLocationId,
                            "plan-subscription",
                            new Dictionary<string, string>
                            {
                                ["customCycleOutcome"] = "success",
                            }
                        );
                    }
                    catch (InvalidOperationException ex)
                        when (string.Equals(
                            ex.Message,
                            RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode,
                            StringComparison.Ordinal
                        ))
                    {
                        return (
                            null,
                            StatusCodes.Status400BadRequest,
                            RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode
                        );
                    }

                    var (checkoutUrl, error) = await _customPlanCycleBilling.StartCyclePayAsync(
                        restaurantId,
                        customRedirectUrl,
                        idempotencyKey: null,
                        quoteOverride: quote
                    );
                    if (error == null && !string.IsNullOrWhiteSpace(checkoutUrl))
                    {
                        return (
                            new PendingPaymentRetryDto { CheckoutUrl = checkoutUrl },
                            StatusCodes.Status200OK,
                            null
                        );
                    }

                    code = error ?? "custom_cycle_start_failed";
                }
                else
                {
                    var owner = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);
                    if (owner == null)
                    {
                        return (null, StatusCodes.Status400BadRequest, "billing_email_required");
                    }

                    var locationId = await ResolveFirstLocationIdAsync(restaurantId);
                    var result = await _firstPaidConversionPaySession.StartAsync(
                        billingAccount,
                        owner,
                        restaurant.AccountType,
                        locationId,
                        pendingPlan,
                        cadence,
                        $"pending-retry-{restaurantId}-{Guid.NewGuid():N}"
                    );
                    return (
                        new PendingPaymentRetryDto { CheckoutUrl = result.RedirectUrl },
                        StatusCodes.Status200OK,
                        null
                    );
                }
            }
            catch (RevolutMerchantNotReadyException ex)
            {
                code = ex.Code;
            }
            catch (InvalidOperationException ex)
            {
                code = ex.Message;
            }

            // Keep the pending plan. Record the failure for the owner and for Admin.
            code = code.Length > 64 ? code[..64] : code;
            billingAccount.PendingPaymentFailedAtUtc = DateTime.UtcNow;
            billingAccount.PendingPaymentFailureCode = code;
            _context.AdminAuditEvents.Add(
                new AdminAuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = DateTime.UtcNow,
                    Action = AdminAuditActions.BillingPendingPaymentRetryFailed,
                    ActorIdentity = $"user:{userId}",
                    TargetType = AdminAuditTargetTypes.Restaurant,
                    TargetId = restaurantId.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    ),
                    RestaurantId = restaurantId,
                    DetailJson = JsonSerializer.Serialize(
                        new { plan = pendingPlan, code }
                    ),
                    Succeeded = false,
                }
            );
            await _context.SaveChangesAsync();

            return (null, StatusCodes.Status502BadGateway, code);
        }

        private async Task<int> ResolveFirstLocationIdAsync(int restaurantId)
        {
            var locationId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => row.Id)
                .FirstOrDefaultAsync();
            return locationId == 0 ? restaurantId : locationId;
        }

        private async Task<string?> ResolveOpenCustomCycleCheckoutUrlAsync(
            int restaurantId
        )
        {
            var open = await _context.RevolutOrderIntents
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Purpose == RevolutOrderIntentPurposes.CustomPlanCycle
                    && row.IsOpen
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .Select(row => row.CheckoutUrl)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(open) ? null : open;
        }

        private static string FormatPoundsFromPence(int pence)
        {
            if (pence % 100 == 0)
            {
                return $"£{pence / 100}";
            }

            return $"£{(pence / 100m).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"))}";
        }

        private static string FormatCreditsCountLabel(int quantity)
        {
            return quantity.ToString("N0", CultureInfo.GetCultureInfo("en-GB"));
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
                SubscriptionPlan = BillingSubscriptionPlans.Free,
                BillingCycle = null,
                BillingStatus = BillingStatuses.Free,
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

        /// <summary>
        /// Post-provision / Free→Pilot billing for Pilot. Sets a fresh 30-day
        /// <see cref="BillingAccount.PilotPeriodEnd"/> from <paramref name="nowUtc"/>.
        /// </summary>
        public static void ApplyPilotSignupBilling(
            BillingAccount billingAccount,
            DateTime nowUtc
        )
        {
            billingAccount.SubscriptionPlan = BillingSubscriptionPlans.Pilot;
            billingAccount.BillingCycle = null;
            billingAccount.BillingStatus = BillingStatuses.Pilot;
            billingAccount.PilotPeriodEnd = nowUtc.AddDays(
                ActivationCodeHelper.ActivationPeriodDays
            );
            billingAccount.SoftLockEnteredAt = null;
            billingAccount.DormantEnteredAt = null;
            billingAccount.GuestRetentionPurgedAtUtc = null;
            billingAccount.PilotSoftLockNotified = false;
            billingAccount.PilotDormantNotified = false;
            billingAccount.RenewalDateUtc = null;
        }

        /// <summary>
        /// Post-provision billing for self-serve paid signup (Starter / Growth / Group).
        /// Matches Manage Plan paid state after first payment.
        /// </summary>
        public static void ApplyPaidSignupBilling(
            BillingAccount billingAccount,
            string chosenPlan,
            string chosenCadenceApi,
            DateTime nowUtc
        )
        {
            var plan = chosenPlan.Trim().ToLowerInvariant() switch
            {
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                _ => throw new InvalidOperationException("invalid_plan_target"),
            };

            var annual = string.Equals(
                chosenCadenceApi.Trim(),
                "annual",
                StringComparison.OrdinalIgnoreCase
            );

            billingAccount.SubscriptionPlan = plan;
            billingAccount.BillingCycle = annual
                ? BillingCycles.Annual
                : BillingCycles.Monthly;
            billingAccount.BillingStatus = BillingStatuses.Active;
            billingAccount.SoftLockEnteredAt = null;
            billingAccount.DormantEnteredAt = null;
            billingAccount.GuestRetentionPurgedAtUtc = null;
            billingAccount.PilotSoftLockNotified = false;
            billingAccount.PilotDormantNotified = false;
            billingAccount.RenewalDateUtc = annual
                ? nowUtc.Date.AddMonths(12)
                : nowUtc.Date.AddMonths(1);
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
                CreditTopUpRequestDto request,
                string? idempotencyKey = null
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

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return (
                    null,
                    StatusCodes.Status400BadRequest,
                    "idempotency_key_required"
                );
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return (null, StatusCodes.Status404NotFound, "Restaurant not found.");
            }

            var billingAccount = await LoadRequiredBillingAccountAsync(restaurantId);
            if (string.IsNullOrWhiteSpace(billingAccount.RevolutCustomerId))
            {
                return (
                    null,
                    StatusCodes.Status400BadRequest,
                    "revolut_customer_required"
                );
            }

            var pack = FindCurrentPricebookTopUpPack(
                request.Channel,
                request.Quantity
            );
            if (pack == null)
            {
                return (
                    null,
                    StatusCodes.Status400BadRequest,
                    "invalid_topup_pack"
                );
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
                var checkoutUrl = await _creditTopUpPaySession.StartAsync(
                    billingAccount,
                    restaurant.AccountType,
                    locationId,
                    pack,
                    idempotencyKey.Trim()
                );
                return (
                    new CreditTopUpPayDto
                    {
                        Outcome = "pay",
                        RedirectUrl = checkoutUrl,
                    },
                    StatusCodes.Status200OK,
                    null
                );
            }
            catch (RevolutMerchantNotReadyException ex)
            {
                return (
                    null,
                    StatusCodes.Status503ServiceUnavailable,
                    ex.Code
                );
            }
            catch (InvalidOperationException ex) when (
                ex.Message
                    is "idempotency_target_mismatch"
                        or "revolut_customer_required"
                        or "Frontend:BaseUrl is not configured."
                        or RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode
            )
            {
                var status = ex.Message == "idempotency_target_mismatch"
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;
                return (null, status, ex.Message);
            }
            catch (InvalidOperationException ex) when (
                ex.Message
                    is RevolutMerchantCreateGate.VatNotReady
                        or RevolutMerchantCreateGate.RevolutNotReady
                        or RevolutMerchantCreateGate.RevolutSandboxRequired
                        or RevolutMerchantCreateGate.PlanVariationMissing
            )
            {
                return (
                    null,
                    StatusCodes.Status503ServiceUnavailable,
                    ex.Message
                );
            }
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
                    "pilot_topup_unavailable"
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

            var pack = FindCurrentPricebookTopUpPack(
                request.Channel,
                request.Quantity
            );
            if (pack == null)
            {
                return (
                    null,
                    StatusCodes.Status400BadRequest,
                    "invalid_topup_pack"
                );
            }

            if (
                pack.Channel == "sms"
                && pack.Quantity == 5000
                && !CreditTopUpPricebook.IsSms5000Allowed(
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

        private PricebookTopUpPack? FindCurrentPricebookTopUpPack(
            string channel,
            int quantity
        )
        {
            var normalized = channel.Trim().ToLowerInvariant();
            var book = _pricebookCatalog.GetRequired(
                _pricebookCatalog.CurrentPricebookId
            );
            return book.TopUpPacks.FirstOrDefault(pack =>
                pack.Channel == normalized && pack.Quantity == quantity
            );
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

        private CreditTopUpConfirmDto BuildCreditTopUpConfirm(CreditTopUpPack pack)
        {
            var vatRateBps = _sellerVat.EffectiveVatRateBps;
            var gross = CreditTopUpPricebook.GrossPounds(pack.NetPounds, vatRateBps);
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

            await BackfillPaidOrderActivityIfMissingAsync(restaurantId);

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
                        Id = row.Id,
                        Kind = row.Kind,
                        OccurredAtUtc = row.OccurredAtUtc,
                        Sentence = BillingActivityCopyFormatter.FormatSentence(row),
                    })
                    .ToList(),
                TotalCount = totalCount,
            };
        }

        /// <summary>
        /// Paid Revolut apply used to mint TM invoices without writing activity.
        /// Fill missing invoice_paid / subscription_created rows from invoices.
        /// </summary>
        private async Task BackfillPaidOrderActivityIfMissingAsync(int restaurantId)
        {
            var invoices = await _context.TummlyVatInvoices
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.DocumentPrefix == TummlyDocumentSequence.PrefixTm
                )
                .OrderBy(row => row.InvoiceDateUtc)
                .ThenBy(row => row.DocumentNumber)
                .ToListAsync();
            if (invoices.Count == 0)
            {
                return;
            }

            var existingInvoiceNos = await _context.RestaurantBillingActivities
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Kind == BillingActivityKinds.InvoicePaid
                    && row.InvoiceNo != null
                )
                .Select(row => row.InvoiceNo!)
                .ToListAsync();
            var invoiceNos = new HashSet<string>(
                existingInvoiceNos,
                StringComparer.Ordinal
            );

            var wrote = false;
            foreach (var invoice in invoices)
            {
                if (invoiceNos.Contains(invoice.DocumentNumber))
                {
                    continue;
                }

                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.InvoicePaid,
                        OccurredAtUtc = invoice.InvoiceDateUtc,
                        InvoiceNo = invoice.DocumentNumber,
                    }
                );
                invoiceNos.Add(invoice.DocumentNumber);
                wrote = true;
            }

            var hasSubscriptionCreated = await _context.RestaurantBillingActivities
                .AsNoTracking()
                .AnyAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.Kind == BillingActivityKinds.SubscriptionCreated
                );
            if (!hasSubscriptionCreated)
            {
                var account = await _context.BillingAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);
                if (
                    account != null
                    && string.Equals(
                        account.BillingStatus,
                        BillingStatuses.Active,
                        StringComparison.Ordinal
                    )
                    && !string.Equals(
                        account.SubscriptionPlan,
                        BillingSubscriptionPlans.Pilot,
                        StringComparison.Ordinal
                    )
                )
                {
                    var actorName = await _context.Restaurants
                        .AsNoTracking()
                        .Where(row => row.Id == restaurantId)
                        .Select(row => row.OwnerUser.FullName)
                        .FirstOrDefaultAsync();
                    var firstPaidAt = invoices[0].InvoiceDateUtc;
                    BillingActivityWriter.TryAppend(
                        _context,
                        new BillingActivityAppendRequest
                        {
                            RestaurantId = restaurantId,
                            Kind = BillingActivityKinds.SubscriptionCreated,
                            OccurredAtUtc = firstPaidAt,
                            ActorDisplayName = string.IsNullOrWhiteSpace(actorName)
                                ? null
                                : actorName.Trim(),
                            Plan = account.SubscriptionPlan,
                            Cadence = account.BillingCycle
                                ?? BillingCycles.Monthly,
                        }
                    );
                    wrote = true;
                }
            }

            if (wrote)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
