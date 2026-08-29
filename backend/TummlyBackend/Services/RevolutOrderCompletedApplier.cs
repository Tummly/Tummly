using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
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
            private readonly IPlanChangeService _planChange;
            private readonly IExtraGroupLocationService _extraGroupLocation;
            private readonly ICreditLedger _creditLedger;
            private readonly IRevolutMerchantClient _merchant;
            private readonly TimeProvider _clock;

            public RevolutOrderCompletedApplier(
                ApplicationDbContext context,
                IIncludedPeriodMintService mint,
                ITummlyVatInvoiceService vatInvoices,
                IPlanChangeService planChange,
                IExtraGroupLocationService extraGroupLocation,
                ICreditLedger creditLedger,
                IRevolutMerchantClient merchant,
                TimeProvider clock
            )
            {
                _context = context;
                _mint = mint;
                _vatInvoices = vatInvoices;
                _planChange = planChange;
                _extraGroupLocation = extraGroupLocation;
                _creditLedger = creditLedger;
                _merchant = merchant;
                _clock = clock;
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
            var isSetup = string.Equals(
                reason,
                SetupIntent,
                StringComparison.Ordinal
            );
            var cycle = ResolveCycleWindow(
                billingAccount,
                pending,
                isSetup,
                nowUtc
            );

            ApplyPaymentFields(
                billingAccount,
                pending,
                isSetup,
                cycle.RenewalDateUtc
            );

            if (isSetup && pending.IsOpen)
            {
                pending.IsOpen = false;
            }

            await _context.SaveChangesAsync(cancellationToken);

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

            await _vatInvoices.MintForCompletedOrderAsync(
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
        }

        private async Task ApplyPlanUpgradeProrationAsync(
            RevolutOrderIntent intent,
            CancellationToken cancellationToken
        )
        {
            await _planChange.ApplyImmediateSameCadenceUpgradeAsync(
                intent.RestaurantId,
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
            await _vatInvoices.MintForCompletedOrderAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: intent.RevolutSubscriptionId,
                    RestaurantId: intent.RestaurantId,
                    Plan: intent.TargetPlan,
                    BillingCycle: CadenceToBillingCycle(intent.TargetCadence),
                    PaymentSuccessUtc: taxPoint,
                    NetPenceOverride: intent.NetAmountMinor,
                    LineDescriptionOverride: $"Plan upgrade to {intent.TargetPlan}"
                ),
                cancellationToken
            );

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
                await _context.SaveChangesAsync(cancellationToken);
            }
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
            }

            // Pilot→paid / renewal dates only. Dunning episode clears stay on
            // ticket 24 RecoverDunningAsync.
            billingAccount.BillingStatus = BillingStatuses.Active;
            billingAccount.RenewalDateUtc = renewalDateUtc;
            billingAccount.PilotPeriodEnd = null;
            billingAccount.SoftLockEnteredAt = null;
            billingAccount.DormantEnteredAt = null;
            billingAccount.PilotSoftLockNotified = false;
            billingAccount.PilotDormantNotified = false;
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

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var apply = await _extraGroupLocation.ApplyAddOnOrderCompletedAsync(
                intent.RestaurantId,
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
                        intent.RestaurantId,
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

            await _vatInvoices.MintForCompletedOrderAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: string.IsNullOrWhiteSpace(subscriptionId)
                        ? null
                        : subscriptionId,
                    RestaurantId: intent.RestaurantId,
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

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
                await _context.SaveChangesAsync(cancellationToken);
            }
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

            var channel = (intent.Channel ?? string.Empty).Trim().ToLowerInvariant();
            var quantity = intent.Quantity ?? 0;
            if (string.IsNullOrWhiteSpace(channel) || quantity <= 0)
            {
                throw new InvalidOperationException("invalid_topup_intent");
            }

            var alreadyMinted = await _context.CreditLedgerEntries
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.RestaurantId == intent.RestaurantId
                        && row.SourcePaymentRef == intent.OrderId
                        && row.EntryType == CreditLedgerEntryTypes.TopupAllocation,
                    cancellationToken
                );

            if (!alreadyMinted)
            {
                var mintResult = await _creditLedger.MintTopupAllocationAsync(
                    new CreditLedgerMintTopupRequest
                    {
                        RestaurantId = intent.RestaurantId,
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

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == intent.RestaurantId,
                    cancellationToken
                );
            var plan = billingAccount?.SubscriptionPlan
                ?? BillingSubscriptionPlans.Starter;
            var cycle = billingAccount?.BillingCycle ?? BillingCycles.Monthly;
            var lineName = FormatTopupLineDescription(channel, quantity);

            await _vatInvoices.MintForCompletedOrderAsync(
                new TummlyVatInvoiceMintRequest(
                    RevolutOrderId: intent.OrderId,
                    RevolutSubscriptionId: null,
                    RestaurantId: intent.RestaurantId,
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

            if (intent.IsOpen)
            {
                intent.IsOpen = false;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private static string FormatTopupLineDescription(string channel, int quantity)
        {
            var label = channel switch
            {
                "sms" => "SMS",
                "email" => "Email",
                "ai" => "AI",
                _ => channel,
            };
            return $"{label} credit pack ({quantity:N0})";
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

        private static string CadenceToBillingCycle(string targetCadence)
        {
            return targetCadence.Trim().ToLowerInvariant() switch
            {
                "annual" => BillingCycles.Annual,
                _ => BillingCycles.Monthly,
            };
        }
    }
}
