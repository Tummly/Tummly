using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// ORDER_COMPLETED apply+mint for <c>setup_intent</c> and
    /// <c>cycle_billing</c> (ticket 16 / lock 04). Runs inside the webhook
    /// claim transaction.
    /// </summary>
    public sealed class RevolutOrderCompletedApplier : IRevolutOrderCompletedApplier
    {
        public const string SetupIntent = "setup_intent";

        public const string CycleBilling = "cycle_billing";

        private readonly ApplicationDbContext _context;
        private readonly IIncludedPeriodMintService _mint;
        private readonly TimeProvider _clock;

        public RevolutOrderCompletedApplier(
            ApplicationDbContext context,
            IIncludedPeriodMintService mint,
            TimeProvider clock
        )
        {
            _context = context;
            _mint = mint;
            _clock = clock;
        }

        public async Task ApplyAsync(
            RevolutOrderCompletedApplyRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var reason = request.BillingReason?.Trim() ?? string.Empty;
            if (
                !string.Equals(reason, SetupIntent, StringComparison.Ordinal)
                && !string.Equals(reason, CycleBilling, StringComparison.Ordinal)
            )
            {
                // Caller claims skipped_unknown_billing_reason before invoke.
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
            var cycle = ResolveCycleWindow(
                billingAccount,
                pending,
                reason,
                nowUtc
            );

            ApplyPaymentFields(
                billingAccount,
                pending,
                reason,
                cycle.RenewalDateUtc
            );

            if (
                string.Equals(reason, SetupIntent, StringComparison.Ordinal)
                && pending.IsOpen
            )
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
            string reason,
            DateTime renewalDateUtc
        )
        {
            if (string.Equals(reason, SetupIntent, StringComparison.Ordinal))
            {
                billingAccount.SubscriptionPlan = pending.TargetPlan;
                billingAccount.BillingCycle = CadenceToBillingCycle(
                    pending.TargetCadence
                );
            }

            billingAccount.BillingStatus = BillingStatuses.Active;
            billingAccount.RenewalDateUtc = renewalDateUtc;
            billingAccount.PilotPeriodEnd = null;
            billingAccount.SoftLockEnteredAt = null;
            billingAccount.DormantEnteredAt = null;
            billingAccount.PilotSoftLockNotified = false;
            billingAccount.PilotDormantNotified = false;
            billingAccount.DunningEpisodeStartedAt = null;
            billingAccount.DunningFiredSteps = null;
        }

        private static (
            DateTime CycleStartUtc,
            DateTime? NextCycleStartUtc,
            DateTime? CycleEndUtc,
            DateTime RenewalDateUtc
        ) ResolveCycleWindow(
            BillingAccount billingAccount,
            RevolutPendingPaySession pending,
            string reason,
            DateTime nowUtc
        )
        {
            var cycleStart = nowUtc.Date;
            if (
                string.Equals(reason, CycleBilling, StringComparison.Ordinal)
                && billingAccount.RenewalDateUtc is DateTime priorRenewal
            )
            {
                cycleStart = priorRenewal.Kind == DateTimeKind.Utc
                    ? priorRenewal
                    : DateTime.SpecifyKind(priorRenewal, DateTimeKind.Utc);
            }

            var billingCycle =
                string.Equals(reason, SetupIntent, StringComparison.Ordinal)
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
