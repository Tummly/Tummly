using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
        /// ORDER_COMPLETED apply for setup/cycle mint (ticket 16),
        /// plan_upgrade_proration (ticket 20), extra_location (ticket 22),
        /// and topup (ticket 18).
        /// Runs inside the webhook claim transaction.
        /// </summary>
        public sealed class RevolutOrderCompletedApplier : IRevolutOrderCompletedApplier
        {
            public const string SetupIntent = "setup_intent";

            public const string CycleBilling = "cycle_billing";

            public const string FinalSettlement = "final_settlement";

            private readonly ApplicationDbContext _context;
            private readonly IIncludedPeriodMintService _mint;
            private readonly ITummlyVatInvoiceService _vatInvoices;
            private readonly ITummlyVatInvoiceEmailDelivery _invoiceEmail;
            private readonly IPlanChangeService _planChange;
            private readonly IExtraGroupLocationService _extraGroupLocation;
            private readonly ICreditLedger _creditLedger;
            private readonly IRevolutMerchantClient _merchant;
            private readonly TimeProvider _clock;
            private readonly TummlySellerVatSettings _sellerVat;
            private readonly IShopOrderEmailNotifier _shopOrderEmail;
            private readonly IComplimentaryStarterShopOrderService _complimentaryStarterShopOrders;
            private readonly IPrintReadyQrMaterialsWork _printReadyQrMaterialsWork;
            private readonly IEmailService _emailService;
            private readonly ICaptureQrLifecycleService _captureQrLifecycle;
            private readonly ILogger<RevolutOrderCompletedApplier> _logger;

            public RevolutOrderCompletedApplier(
                ApplicationDbContext context,
                IIncludedPeriodMintService mint,
                ITummlyVatInvoiceService vatInvoices,
                IPlanChangeService planChange,
                IExtraGroupLocationService extraGroupLocation,
                ICreditLedger creditLedger,
                IRevolutMerchantClient merchant,
                TimeProvider clock,
                IOptions<TummlySellerVatSettings> sellerVat,
                IEmailService emailService,
                ICaptureQrLifecycleService captureQrLifecycle,
                ITummlyVatInvoiceEmailDelivery? invoiceEmail = null,
                IShopOrderEmailNotifier? shopOrderEmail = null,
                IComplimentaryStarterShopOrderService? complimentaryStarterShopOrders =
                    null,
                IPrintReadyQrMaterialsWork? printReadyQrMaterialsWork = null,
                ILogger<RevolutOrderCompletedApplier>? logger = null
            )
            {
                _context = context;
                _mint = mint;
                _vatInvoices = vatInvoices;
                _invoiceEmail = invoiceEmail ?? NoOpTummlyVatInvoiceEmailDelivery.Instance;
                _planChange = planChange;
                _extraGroupLocation = extraGroupLocation;
                _creditLedger = creditLedger;
                _merchant = merchant;
                _clock = clock;
                _sellerVat = sellerVat.Value;
                _emailService = emailService;
                _captureQrLifecycle = captureQrLifecycle;
                _shopOrderEmail = shopOrderEmail ?? NoOpShopOrderEmailNotifier.Instance;
                _complimentaryStarterShopOrders =
                    complimentaryStarterShopOrders
                    ?? NoOpComplimentaryStarterShopOrderService.Instance;
                _printReadyQrMaterialsWork =
                    printReadyQrMaterialsWork
                    ?? NoOpPrintReadyQrMaterialsWork.Instance;
                _logger = logger ?? NullLogger<RevolutOrderCompletedApplier>.Instance;
            }

        public static bool IsMintableBillingReason(string? billingReason)
        {
            var reason = billingReason?.Trim() ?? string.Empty;
            return string.Equals(reason, SetupIntent, StringComparison.Ordinal)
                || string.Equals(reason, CycleBilling, StringComparison.Ordinal);
        }

        public static bool IsFinalSettlement(string? billingReason)
        {
            return string.Equals(
                billingReason?.Trim() ?? string.Empty,
                FinalSettlement,
                StringComparison.Ordinal
            );
        }

        public async Task ApplyAsync(
            RevolutOrderCompletedApplyRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var intent = await _context.RevolutOrderIntents
                .FirstOrDefaultAsync(
                    row => row.OrderId == request.OrderId,
                    cancellationToken
                );
            if (
                intent != null
                && string.Equals(
                    intent.Purpose,
                    RevolutOrderIntentPurposes.PlanUpgradeProration,
                    StringComparison.Ordinal
                )
            )
            {
                await ApplyPlanUpgradeProrationAsync(
                    intent,
                    cancellationToken
                );
                return;
            }

            if (
                intent != null
                && string.Equals(
                    intent.Purpose,
                    RevolutOrderIntentPurposes.ExtraLocation,
                    StringComparison.Ordinal
                )
            )
            {
                await ApplyExtraLocationAsync(intent, cancellationToken);
                return;
            }

            if (
                intent != null
                && string.Equals(
                    intent.Purpose,
                    RevolutOrderIntentPurposes.Topup,
                    StringComparison.Ordinal
                )
            )
            {
                await ApplyTopupAsync(intent, cancellationToken);
                return;
            }

            if (
                intent != null
                && string.Equals(
                    intent.Purpose,
                    RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    StringComparison.Ordinal
                )
            )
            {
                await ApplyShopMaterialsOrderAsync(
                    intent,
                    request.PaymentMethodSummary,
                    cancellationToken
                );
                return;
            }

            if (
                intent != null
                && string.Equals(
                    intent.Purpose,
                    RevolutOrderIntentPurposes.CustomPlanCycle,
                    StringComparison.Ordinal
                )
            )
            {
                await ApplyCustomPlanCycleAsync(intent, cancellationToken);
                return;
            }

            var reason = request.BillingReason?.Trim() ?? string.Empty;
            if (!IsMintableBillingReason(reason))
            {
                return;
            }

            var pending = await ResolvePendingAsync(
                request.OrderId,
                request.SubscriptionId,
                cancellationToken
            );
            if (pending == null)
            {
                throw new InvalidOperationException(
                    "revolut_billing_correlation_missing"
                );
            }

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == pending.RestaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                throw new InvalidOperationException(
                    "billing_account_missing"
                );
            }

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            // Sandbox/Merchant may bill the first subscription charge as
            // cycle_billing (not setup_intent). Open pending pay session still
            // means first-paid conversion — apply plan/cadence from pending.
            var isSetup =
                string.Equals(
                    reason,
                    SetupIntent,
                    StringComparison.Ordinal
                )
                || (
                    pending.IsOpen
                    && string.Equals(
                        reason,
                        CycleBilling,
                        StringComparison.Ordinal
                    )
                );
            var cycle = ResolveCycleWindow(
                billingAccount,
                pending,
                isSetup,
                nowUtc
            );

            // Captured before ApplyPaymentFields changes the plan. Free (plan or
            // status) and Pilot accounts get plan-started. Only Free accounts get
            // their plan-paused codes activated. A Pilot account may hold codes
            // that the operator paused.
            var priorPlan = billingAccount.SubscriptionPlan;
            var wasFree = IsFreeAccount(billingAccount);
            var startsPaidPlan =
                isSetup
                && (
                    wasFree
                    || string.Equals(
                        priorPlan,
                        BillingSubscriptionPlans.Pilot,
                        StringComparison.Ordinal
                    )
                );
            ApplyPaymentFields(
                billingAccount,
                pending,
                isSetup,
                cycle.RenewalDateUtc
            );

            if (isSetup)
            {
                // Clocks are per user, and members copy the owner clock. Every
                // setup apply clears them, also when the pay session was closed.
                // Renewals never touch them.
                await PilotClockSync.ClearForRestaurantAsync(
                    _context,
                    billingAccount.RestaurantId,
                    cancellationToken
                );
                pending.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (isSetup)
            {
                var owner = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == billingAccount.RestaurantId)
                    .Select(row => new
                    {
                        row.OwnerUserId,
                        FullName = row.OwnerUser.FullName,
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                if (owner != null)
                {
                    var shopOrderIds =
                        await _complimentaryStarterShopOrders.EnsureAllActiveLocationsAsync(
                            billingAccount.RestaurantId,
                            owner.OwnerUserId,
                            owner.FullName ?? string.Empty,
                            cancellationToken
                        );
                    foreach (var shopOrderId in shopOrderIds)
                    {
                        await _printReadyQrMaterialsWork.RequestShopOrderEnsureAsync(
                            shopOrderId,
                            cancellationToken
                        );
                    }
                }

                if (wasFree)
                {
                    await _captureQrLifecycle.ActivatePlanPausedCodesAsync(
                        billingAccount.RestaurantId,
                        cancellationToken
                    );
                }
            }

            var mintResult = await _mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = billingAccount.RestaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = cycle.CycleStartUtc,
                    NextCycleStartUtc = cycle.NextCycleStartUtc,
                    CycleEndUtc = cycle.CycleEndUtc,
                },
                cancellationToken
            );

            if (
                !mintResult.Succeeded
                || (
                    !string.IsNullOrEmpty(mintResult.Code)
                    && mintResult.Code != "grant_already_exists"
                    && mintResult.InsertedAllocationIds.Count == 0
                )
            )
            {
                throw new InvalidOperationException(
                    mintResult.Code ?? "included_mint_failed"
                );
            }

            var invoice = await MintInvoiceAndDeliverEmailAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: request.OrderId,
                    RevolutSubscriptionId: request.SubscriptionId
                        ?? pending.RevolutSubscriptionId,
                    RestaurantId: billingAccount.RestaurantId,
                    Plan: billingAccount.SubscriptionPlan,
                    BillingCycle: billingAccount.BillingCycle
                        ?? BillingCycles.Monthly,
                    PaymentSuccessUtc: nowUtc
                ),
                cancellationToken
            );

            var actorName = await ResolveOwnerDisplayNameAsync(
                billingAccount.RestaurantId,
                cancellationToken
            );
            if (isSetup)
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = billingAccount.RestaurantId,
                        Kind = BillingActivityKinds.SubscriptionCreated,
                        OccurredAtUtc = nowUtc,
                        ActorDisplayName = actorName,
                        Plan = billingAccount.SubscriptionPlan,
                        Cadence = billingAccount.BillingCycle
                            ?? BillingCycles.Monthly,
                    }
                );
            }
            else
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = billingAccount.RestaurantId,
                        Kind = BillingActivityKinds.SubscriptionRenewed,
                        OccurredAtUtc = nowUtc,
                        Plan = billingAccount.SubscriptionPlan,
                    }
                );
            }

            if (invoice != null)
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = billingAccount.RestaurantId,
                        Kind = BillingActivityKinds.InvoicePaid,
                        OccurredAtUtc = nowUtc,
                        InvoiceNo = invoice.DocumentNumber,
                    }
                );
            }
            await _context.SaveChangesAsync(cancellationToken);

            if (startsPaidPlan)
            {
                await TrySendPlanStartedEmailAsync(
                    billingAccount.RestaurantId,
                    billingAccount.SubscriptionPlan,
                    cancellationToken
                );
            }
        }

        private async Task ApplyPlanUpgradeProrationAsync(
            RevolutOrderIntent intent,
            CancellationToken cancellationToken
        )
        {
            var restaurantId = RequireRestaurantId(intent);
            await _planChange.ApplyImmediateSameCadenceUpgradeAsync(
                restaurantId,
                intent.TargetPlan,
                cancellationToken
            );

            var lookupKey = RevolutPlanVariationKeys.ForPlanCadence(
                intent.TargetPlan,
                intent.TargetCadence
            );
            if (lookupKey == null)
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            var change = await _merchant.ChangeSubscriptionPlanAsync(
                intent.RevolutSubscriptionId,
                lookupKey,
                cancellationToken
            );
            if (!change.Succeeded)
            {
                throw new InvalidOperationException(
                    change.ErrorCode ?? "revolut_http_error"
                );
            }

            var taxPoint = _clock.GetUtcNow().UtcDateTime;
            var invoice = await MintInvoiceAndDeliverEmailAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: intent.RevolutSubscriptionId,
                    RestaurantId: restaurantId,
                    Plan: intent.TargetPlan,
                    BillingCycle: CadenceToBillingCycle(intent.TargetCadence),
                    PaymentSuccessUtc: taxPoint,
                    NetPenceOverride: intent.NetAmountMinor,
                    LineDescriptionOverride: $"Plan upgrade to {intent.TargetPlan}"
                ),
                cancellationToken
            );

            var actorName = await ResolveOwnerDisplayNameAsync(
                restaurantId,
                cancellationToken
            );
            BillingActivityWriter.TryAppend(
                _context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = restaurantId,
                    Kind = BillingActivityKinds.SubscriptionUpgraded,
                    OccurredAtUtc = taxPoint,
                    ActorDisplayName = actorName,
                    Plan = intent.TargetPlan,
                }
            );
            if (invoice != null)
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.InvoicePaid,
                        OccurredAtUtc = taxPoint,
                        InvoiceNo = invoice.DocumentNumber,
                    }
                );
            }

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Sends plan-started to the owner. Runs once per first activation, after
        /// the apply is saved. The owner lookup and the send both sit inside the
        /// try, so a failure never aborts the apply. Only the exception type is
        /// logged.
        /// </summary>
        private async Task TrySendPlanStartedEmailAsync(
            int restaurantId,
            string planName,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var owner = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId)
                    .Select(row => new
                    {
                        row.Name,
                        OwnerFullName = row.OwnerUser.FullName,
                        OwnerEmail = row.OwnerUser.Email,
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                if (owner == null || string.IsNullOrWhiteSpace(owner.OwnerEmail))
                {
                    return;
                }

                await _emailService.SendPlanStartedEmailAsync(
                    owner.OwnerEmail,
                    SignInMetadataResolver.ExtractFirstName(owner.OwnerFullName ?? string.Empty),
                    planName,
                    owner.Name
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Plan started email failed for restaurant {RestaurantId}: {ExceptionType}",
                    restaurantId,
                    ex.GetType().Name
                );
            }
        }

        private async Task<string?> ResolveOwnerDisplayNameAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var fullName = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == restaurantId)
                .Select(row => row.OwnerUser.FullName)
                .FirstOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(fullName)
                ? null
                : fullName.Trim();
        }

        private async Task<RevolutPendingPaySession?> ResolvePendingAsync(
            string orderId,
            string? subscriptionId,
            CancellationToken cancellationToken
        )
        {
            var byOrder = await _context.RevolutPendingPaySessions
                .Where(row => row.SetupOrderId == orderId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (byOrder != null)
            {
                return byOrder;
            }

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return null;
            }

            var subId = subscriptionId.Trim();
            return await _context.RevolutPendingPaySessions
                .Where(row => row.RevolutSubscriptionId == subId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static bool IsFreeAccount(BillingAccount billingAccount) =>
            string.Equals(
                billingAccount.SubscriptionPlan,
                BillingSubscriptionPlans.Free,
                StringComparison.Ordinal
            )
            || string.Equals(
                billingAccount.BillingStatus,
                BillingStatuses.Free,
                StringComparison.Ordinal
            );

        private static void ApplyPaymentFields(
            BillingAccount billingAccount,
            RevolutPendingPaySession pending,
            bool isSetup,
            DateTime renewalDateUtc
        )
        {
            if (isSetup)
            {
                billingAccount.SubscriptionPlan = pending.TargetPlan;
                billingAccount.BillingCycle = CadenceToBillingCycle(
                    pending.TargetCadence
                );
                ClearPendingPayment(billingAccount);
            }

            // Pilot→paid / renewal dates only. Dunning episode clears stay on
            // ticket 24 RecoverDunningAsync.
            billingAccount.BillingStatus = BillingStatuses.Active;
            billingAccount.RenewalDateUtc = renewalDateUtc;
            billingAccount.SoftLockEnteredAt = null;
            billingAccount.DormantEnteredAt = null;
            billingAccount.GuestRetentionPurgedAtUtc = null;
            billingAccount.PilotSoftLockNotified = false;
            billingAccount.PilotDormantNotified = false;
        }

        /// <summary>
        /// Payment confirmed. Drop the pending upgrade that the owner was asked to pay.
        /// </summary>
        private static void ClearPendingPayment(BillingAccount billingAccount)
        {
            billingAccount.PendingPaymentPlan = null;
            billingAccount.PendingPaymentCadence = null;
            billingAccount.PendingPaymentCustomConfigJson = null;
            billingAccount.PendingPaymentFailedAtUtc = null;
            billingAccount.PendingPaymentFailureCode = null;
        }

        private async Task ApplyCustomPlanCycleAsync(
            RevolutOrderIntent intent,
            CancellationToken cancellationToken
        )
        {
            if (!intent.IsOpen)
            {
                return;
            }

            var restaurantId = RequireRestaurantId(intent);
            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                throw new InvalidOperationException("billing_account_missing");
            }

            // Acquire sets SubscriptionPlan=Custom before first pay; RenewalDateUtc
            // stays null until this apply. Renewals already have a renewal date.
            var isFirstPaid = billingAccount.RenewalDateUtc == null;
            var wasFree = IsFreeAccount(billingAccount);

            billingAccount.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            billingAccount.BillingStatus = BillingStatuses.Active;
            ClearPendingPayment(billingAccount);
            billingAccount.BillingCycle = CadenceToBillingCycle(
                intent.TargetCadence
            );
            var periodDays = string.Equals(
                billingAccount.BillingCycle,
                BillingCycles.Annual,
                StringComparison.Ordinal
            )
                ? 365
                : 30;
            billingAccount.RenewalDateUtc = nowUtc.AddDays(periodDays);
            billingAccount.SoftLockEnteredAt = null;
            billingAccount.DormantEnteredAt = null;
            billingAccount.GuestRetentionPurgedAtUtc = null;
            billingAccount.PilotSoftLockNotified = false;
            billingAccount.PilotDormantNotified = false;
            billingAccount.DunningEpisodeStartedAt = null;
            billingAccount.DunningFiredSteps = null;
            billingAccount.DunningOutstandingOrderId = null;
            intent.IsOpen = false;
            if (isFirstPaid)
            {
                await PilotClockSync.ClearForRestaurantAsync(
                    _context,
                    restaurantId,
                    cancellationToken
                );
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (isFirstPaid && wasFree)
            {
                await _captureQrLifecycle.ActivatePlanPausedCodesAsync(
                    restaurantId,
                    cancellationToken
                );
            }

            await _mint.MintOnOrderCompletedAsync(
                new IncludedPeriodOrderCompletedRequest
                {
                    RestaurantId = restaurantId,
                    PaymentCompleted = true,
                    CycleStartUtc = nowUtc,
                    NextCycleStartUtc = billingAccount.RenewalDateUtc,
                    CycleEndUtc = billingAccount.RenewalDateUtc,
                },
                cancellationToken
            );

            var invoice = await MintInvoiceAndDeliverEmailAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: string.IsNullOrWhiteSpace(
                        intent.RevolutSubscriptionId
                    )
                        ? null
                        : intent.RevolutSubscriptionId,
                    RestaurantId: restaurantId,
                    Plan: BillingSubscriptionPlans.Custom,
                    BillingCycle: billingAccount.BillingCycle
                        ?? BillingCycles.Monthly,
                    PaymentSuccessUtc: nowUtc,
                    NetPenceOverride: intent.NetAmountMinor > 0
                        ? intent.NetAmountMinor
                        : null,
                    LineDescriptionOverride: "Tummly Custom plan"
                ),
                cancellationToken
            );

            var actorName = await ResolveOwnerDisplayNameAsync(
                restaurantId,
                cancellationToken
            );
            BillingActivityWriter.TryAppend(
                _context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = restaurantId,
                    Kind = isFirstPaid
                        ? BillingActivityKinds.SubscriptionCreated
                        : BillingActivityKinds.SubscriptionRenewed,
                    OccurredAtUtc = nowUtc,
                    ActorDisplayName = isFirstPaid ? actorName : null,
                    Plan = BillingSubscriptionPlans.Custom,
                    Cadence = isFirstPaid
                        ? billingAccount.BillingCycle ?? BillingCycles.Monthly
                        : null,
                }
            );
            if (invoice != null)
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.InvoicePaid,
                        OccurredAtUtc = nowUtc,
                        InvoiceNo = invoice.DocumentNumber,
                    }
                );
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (isFirstPaid)
            {
                await TrySendPlanStartedEmailAsync(
                    restaurantId,
                    billingAccount.SubscriptionPlan,
                    cancellationToken
                );
            }
        }

        private async Task ApplyExtraLocationAsync(
            RevolutOrderIntent intent,
            CancellationToken cancellationToken
        )
        {
            if (!intent.IsOpen)
            {
                return;
            }

            var restaurantId = RequireRestaurantId(intent);
            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var apply = await _extraGroupLocation.ApplyAddOnOrderCompletedAsync(
                restaurantId,
                nowUtc,
                cancellationToken
            );
            if (!apply.Succeeded)
            {
                throw new InvalidOperationException(
                    apply.Code ?? "extra_location_apply_failed"
                );
            }

            var subscriptionId = intent.RevolutSubscriptionId;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                subscriptionId =
                    await RevolutSubscriptionCorrelation.ResolveLatestSubscriptionIdAsync(
                        _context,
                        restaurantId,
                        cancellationToken
                    ) ?? string.Empty;
            }

            var cadenceApi = string.IsNullOrWhiteSpace(intent.TargetCadence)
                ? "monthly"
                : intent.TargetCadence.Trim().ToLowerInvariant();
            var lookupKey = RevolutPlanVariationKeys.ForExtraLocation(cadenceApi);
            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var change = await _merchant.ChangeSubscriptionPlanAsync(
                    subscriptionId,
                    lookupKey,
                    cancellationToken
                );
                if (!change.Succeeded)
                {
                    throw new InvalidOperationException(
                        change.ErrorCode ?? "revolut_http_error"
                    );
                }
            }

            var invoice = await MintInvoiceAndDeliverEmailAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: string.IsNullOrWhiteSpace(subscriptionId)
                        ? null
                        : subscriptionId,
                    RestaurantId: restaurantId,
                    Plan: BillingSubscriptionPlans.Group,
                    BillingCycle: CadenceToBillingCycle(cadenceApi),
                    PaymentSuccessUtc: nowUtc,
                    NetPenceOverride: intent.NetAmountMinor > 0
                        ? intent.NetAmountMinor
                        : null,
                    LineDescriptionOverride: "Additional Group Location"
                ),
                cancellationToken
            );

            var actorName = await ResolveOwnerDisplayNameAsync(
                restaurantId,
                cancellationToken
            );
            BillingActivityWriter.TryAppend(
                _context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = restaurantId,
                    Kind = BillingActivityKinds.AdditionalLocationAdded,
                    OccurredAtUtc = nowUtc,
                    ActorDisplayName = actorName,
                    LocationName = "Additional Group Location",
                }
            );
            if (invoice != null)
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.InvoicePaid,
                        OccurredAtUtc = nowUtc,
                        InvoiceNo = invoice.DocumentNumber,
                    }
                );
            }

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> IsTopupMintedAsync(
            int restaurantId,
            string orderId,
            CancellationToken cancellationToken
        )
        {
            return await _context.CreditLedgerEntries
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.SourcePaymentRef == orderId
                        && row.EntryType == CreditLedgerEntryTypes.TopupAllocation,
                    cancellationToken
                );
        }

        private async Task DiscardFailedTopupMintAsync(
            string orderId,
            CancellationToken cancellationToken
        )
        {
            foreach (var entry in _context.ChangeTracker.Entries<CreditLedgerEntry>().ToList())
            {
                if (
                    entry.State == EntityState.Added
                    && entry.Entity.EntryType == CreditLedgerEntryTypes.TopupAllocation
                    && entry.Entity.SourcePaymentRef == orderId
                )
                {
                    entry.State = EntityState.Detached;
                }
            }

            // The failed SaveChanges also left threshold watermark changes
            // pending. Drop them so a later SaveChanges cannot persist values
            // computed from the lost mint.
            foreach (var entry in _context.ChangeTracker.Entries<CreditWarningState>().ToList())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.State = EntityState.Detached;
                }
                else if (entry.State == EntityState.Modified)
                {
                    await entry.ReloadAsync(cancellationToken);
                }
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqlException
            {
                Number: 2601 or 2627,
            };
        }

        private async Task ApplyTopupAsync(
            RevolutOrderIntent intent,
            CancellationToken cancellationToken
        )
        {
            if (!intent.IsOpen)
            {
                return;
            }

            var restaurantId = RequireRestaurantId(intent);
            var channel = (intent.Channel ?? string.Empty).Trim().ToLowerInvariant();
            var quantity = intent.Quantity ?? 0;
            if (string.IsNullOrWhiteSpace(channel) || quantity <= 0)
            {
                throw new InvalidOperationException("invalid_topup_intent");
            }

            // Only the apply that inserts the allocation does the invoice and
            // activity work. An apply that finds the mint already present, or
            // loses the insert race, only closes the intent. This keeps the
            // post-mint work once per order under concurrency.
            var mintedElsewhere = await IsTopupMintedAsync(
                restaurantId,
                intent.OrderId,
                cancellationToken
            );
            var lostMintRace = false;
            if (!mintedElsewhere)
            {
                try
                {
                    var mintResult = await _creditLedger.MintTopupAllocationAsync(
                        new CreditLedgerMintTopupRequest
                        {
                            RestaurantId = restaurantId,
                            Channel = channel,
                            Quantity = quantity,
                            SourcePaymentRef = intent.OrderId,
                        },
                        cancellationToken
                    );
                    if (!mintResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            mintResult.Code ?? "topup_allocate_failed"
                        );
                    }
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // A concurrent apply of this order minted first. Drop the
                    // failed mint. Rethrow if the row is not there.
                    await DiscardFailedTopupMintAsync(intent.OrderId, cancellationToken);
                    if (!await IsTopupMintedAsync(restaurantId, intent.OrderId, cancellationToken))
                    {
                        throw;
                    }

                    lostMintRace = true;
                }
            }

            if (mintedElsewhere || lostMintRace)
            {
                if (intent.IsOpen)
                {
                    intent.IsOpen = false;
                }

                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            var plan = billingAccount?.SubscriptionPlan
                ?? BillingSubscriptionPlans.Starter;
            var cycle = billingAccount?.BillingCycle ?? BillingCycles.Monthly;
            var lineName = CreditTopUpLineCopy.FormatLineDescription(
                channel,
                quantity
            );

            var invoice = await MintInvoiceAndDeliverEmailAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: null,
                    RestaurantId: restaurantId,
                    Plan: plan,
                    BillingCycle: cycle,
                    PaymentSuccessUtc: nowUtc,
                    NetPenceOverride: intent.NetAmountMinor > 0
                        ? intent.NetAmountMinor
                        : null,
                    LineDescriptionOverride: lineName
                ),
                cancellationToken
            );

            var actorName = await ResolveOwnerDisplayNameAsync(
                restaurantId,
                cancellationToken
            );
            BillingActivityWriter.TryAppend(
                _context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = restaurantId,
                    Kind = BillingActivityKinds.TopupPurchased,
                    OccurredAtUtc = nowUtc,
                    ActorDisplayName = actorName,
                    Channel = channel,
                    Qty = quantity,
                }
            );
            if (invoice != null)
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.InvoicePaid,
                        OccurredAtUtc = nowUtc,
                        InvoiceNo = invoice.DocumentNumber,
                    }
                );
            }

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ApplyShopMaterialsOrderAsync(
            RevolutOrderIntent intent,
            string? paymentMethodSummary,
            CancellationToken cancellationToken
        )
        {
            if (!intent.IsOpen)
            {
                return;
            }

            var restaurantId = RequireRestaurantId(intent);
            if (intent.ShopOrderId is not Guid shopOrderId)
            {
                throw new InvalidOperationException("invalid_shop_order_intent");
            }

            var shopOrder = await _context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == shopOrderId
                        && row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (shopOrder == null)
            {
                throw new InvalidOperationException("shop_order_missing");
            }

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            shopOrder.PaymentStatus = ShopPaymentStatuses.Paid;
            shopOrder.FulfilmentStatus = ShopFulfilmentStatuses.Processing;
            shopOrder.PaidAtUtc = nowUtc;
            shopOrder.ProcessingStartedAtUtc = nowUtc;
            shopOrder.RevolutOrderId = intent.OrderId;
            shopOrder.UpdatedAtUtc = nowUtc;

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            var plan = billingAccount?.SubscriptionPlan
                ?? BillingSubscriptionPlans.Starter;
            var cycle = billingAccount?.BillingCycle ?? BillingCycles.Monthly;
            var lineDescription =
                $"Tummly Shop materials order {shopOrder.OrderNumber}";
            var vatRateBps = _sellerVat.EffectiveVatRateBps;
            var lineItems = BuildShopInvoiceLineItems(shopOrder, vatRateBps);
            var deliverTo = FormatShopDeliverToSnapshot(shopOrder);
            var paymentSummary = string.IsNullOrWhiteSpace(paymentMethodSummary)
                ? "Paid"
                : paymentMethodSummary.Trim();

            var invoice = await MintInvoiceAndDeliverEmailAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: null,
                    RestaurantId: restaurantId,
                    Plan: plan,
                    BillingCycle: cycle,
                    PaymentSuccessUtc: nowUtc,
                    NetPenceOverride: intent.NetAmountMinor > 0
                        ? intent.NetAmountMinor
                        : shopOrder.MaterialsNetPence + shopOrder.DeliveryNetPence,
                    LineDescriptionOverride: lineDescription,
                    CustomerBillingEmail: billingAccount?.BillingEmail,
                    DeliverToSnapshot: deliverTo,
                    PaymentMethodSummary: paymentSummary,
                    LineItems: lineItems
                ),
                cancellationToken
            );

            if (invoice != null)
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = restaurantId,
                        Kind = BillingActivityKinds.InvoicePaid,
                        OccurredAtUtc = nowUtc,
                        InvoiceNo = invoice.DocumentNumber,
                    }
                );
            }

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _shopOrderEmail.TryNotifyConfirmedAsync(
                shopOrder.Id,
                cancellationToken
            );
        }

        private static IReadOnlyList<TummlyVatInvoiceLineItemDto> BuildShopInvoiceLineItems(
            ShopOrder shopOrder,
            int vatRateBps
        )
        {
            var lines = new List<TummlyVatInvoiceLineItemDto>();
            foreach (var line in shopOrder.Lines.OrderBy(row => row.TitleSnapshot))
            {
                lines.Add(
                    new TummlyVatInvoiceLineItemDto(
                        Title: line.TitleSnapshot,
                        Subtitle: $"Pack of {line.Quantity}",
                        Quantity: 1,
                        UnitNetPence: line.LineNetPence,
                        VatRateBps: vatRateBps,
                        AmountNetPence: line.LineNetPence
                    )
                );
            }

            var deliveryTitle = string.Equals(
                shopOrder.DeliveryMethod,
                ShopDeliveryMethods.Express,
                StringComparison.OrdinalIgnoreCase
            )
                ? "Express delivery"
                : "Standard delivery";
            lines.Add(
                new TummlyVatInvoiceLineItemDto(
                    Title: deliveryTitle,
                    Subtitle: null,
                    Quantity: 1,
                    UnitNetPence: shopOrder.DeliveryNetPence,
                    VatRateBps: vatRateBps,
                    AmountNetPence: shopOrder.DeliveryNetPence
                )
            );

            return lines;
        }

        private static string FormatShopDeliverToSnapshot(ShopOrder shopOrder)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(shopOrder.ShipToContactName))
            {
                parts.Add(shopOrder.ShipToContactName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(shopOrder.LocationNameSnapshot))
            {
                parts.Add(shopOrder.LocationNameSnapshot.Trim());
            }

            if (!string.IsNullOrWhiteSpace(shopOrder.ShipToAddressLine1))
            {
                parts.Add(shopOrder.ShipToAddressLine1.Trim());
            }

            if (!string.IsNullOrWhiteSpace(shopOrder.ShipToAddressLine2))
            {
                parts.Add(shopOrder.ShipToAddressLine2.Trim());
            }

            if (!string.IsNullOrWhiteSpace(shopOrder.ShipToPostcode))
            {
                parts.Add(shopOrder.ShipToPostcode.Trim());
            }

            if (!string.IsNullOrWhiteSpace(shopOrder.ShipToCountry))
            {
                parts.Add(shopOrder.ShipToCountry.Trim());
            }

            return string.Join('\n', parts);
        }

        private static (
            DateTime CycleStartUtc,
            DateTime? NextCycleStartUtc,
            DateTime? CycleEndUtc,
            DateTime RenewalDateUtc
        ) ResolveCycleWindow(
            BillingAccount billingAccount,
            RevolutPendingPaySession pending,
            bool isSetup,
            DateTime nowUtc
        )
        {
            var cycleStart = nowUtc.Date;
            if (
                !isSetup
                && billingAccount.RenewalDateUtc is DateTime priorRenewal
            )
            {
                cycleStart = priorRenewal.Kind == DateTimeKind.Utc
                    ? priorRenewal
                    : DateTime.SpecifyKind(priorRenewal, DateTimeKind.Utc);
            }

            var billingCycle = isSetup
                ? CadenceToBillingCycle(pending.TargetCadence)
                : billingAccount.BillingCycle ?? BillingCycles.Monthly;

            if (
                string.Equals(
                    billingCycle,
                    BillingCycles.Annual,
                    StringComparison.Ordinal
                )
            )
            {
                var yearEnd = cycleStart.AddMonths(12);
                return (
                    CycleStartUtc: cycleStart,
                    NextCycleStartUtc: null,
                    CycleEndUtc: yearEnd,
                    RenewalDateUtc: yearEnd
                );
            }

            var monthEnd = cycleStart.AddMonths(1);
            return (
                CycleStartUtc: cycleStart,
                NextCycleStartUtc: monthEnd,
                CycleEndUtc: null,
                RenewalDateUtc: monthEnd
            );
        }

        private async Task<TummlyVatInvoice?> MintInvoiceAndDeliverEmailAsync(
            TummlyVatInvoiceMintRequest request,
            CancellationToken cancellationToken
        )
        {
            var preexisting = await _vatInvoices.FindByRevolutOrderIdAsync(
                request.RevolutOrderId,
                cancellationToken
            );
            var invoice = await _vatInvoices.MintForCompletedOrderAsync(
                request,
                cancellationToken
            );
            await _invoiceEmail.DeliverIfNewAsync(
                invoice,
                wasNewlyMinted: preexisting == null,
                cancellationToken
            );
            return invoice;
        }

        private static int RequireRestaurantId(RevolutOrderIntent intent)
        {
            return intent.RestaurantId
                ?? throw new InvalidOperationException("missing_restaurant_id");
        }

        private static string CadenceToBillingCycle(string targetCadence)
        {
            return targetCadence.Trim().ToLowerInvariant() switch
            {
                "annual" => BillingCycles.Annual,
                _ => BillingCycles.Monthly,
            };
        }
    }

    file sealed class NoOpTummlyVatInvoiceEmailDelivery : ITummlyVatInvoiceEmailDelivery
    {
        public static readonly NoOpTummlyVatInvoiceEmailDelivery Instance = new();

        public Task DeliverIfNewAsync(
            TummlyVatInvoice invoice,
            bool wasNewlyMinted,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
