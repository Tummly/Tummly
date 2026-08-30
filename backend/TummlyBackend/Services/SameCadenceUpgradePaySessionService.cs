using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Same-cadence upgrade pay-now one-time Revolut order HPP (ticket 20).
    /// </summary>
    public sealed class SameCadenceUpgradePaySessionService
        : ISameCadenceUpgradePaySession
    {
        private static readonly HashSet<string> TerminalOrderStates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "completed",
                "cancelled",
                "failed",
                "expired",
            };

        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;
        private readonly IPricebookCatalog _pricebook;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _clock;

        public SameCadenceUpgradePaySessionService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IPricebookCatalog pricebook,
            IConfiguration configuration,
            TimeProvider clock
        )
        {
            _context = context;
            _merchant = merchant;
            _pricebook = pricebook;
            _configuration = configuration;
            _clock = clock;
        }

        public async Task<PlanChangeResultDto> StartAsync(
            BillingAccount billingAccount,
            string restaurantAccountType,
            int locationId,
            string targetPlan,
            string targetCadenceApi,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        )
        {
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

            if (string.IsNullOrWhiteSpace(billingAccount.RevolutCustomerId))
            {
                throw new InvalidOperationException("revolut_customer_required");
            }

            var lookupKey = RevolutPlanVariationKeys.ForPlanCadence(
                targetPlan,
                targetCadenceApi
            );
            if (lookupKey == null)
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            _merchant.EnsureReadyForCreate(lookupKey);

            var subscriptionId =
                await RevolutSubscriptionCorrelation.ResolveLatestSubscriptionIdAsync(
                    _context,
                    billingAccount.RestaurantId,
                    cancellationToken
                );
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new InvalidOperationException(
                    "revolut_subscription_required"
                );
            }

            var byKey = await _context.RevolutOrderIntents
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.IdempotencyKey == idempotencyKey
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byKey != null)
            {
                if (
                    !SameTarget(
                        byKey.TargetPlan,
                        byKey.TargetCadence,
                        targetPlan,
                        targetCadenceApi
                    )
                )
                {
                    throw new InvalidOperationException(
                        "idempotency_target_mismatch"
                    );
                }

                if (byKey.IsOpen)
                {
                    var reused = await TryReuseCheckoutAsync(
                        byKey,
                        cancellationToken
                    );
                    if (reused != null)
                    {
                        return PayResult(reused);
                    }
                }
            }

            var openIntent = await _context.RevolutOrderIntents
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.IsOpen
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (openIntent != null)
            {
                if (
                    SameTarget(
                        openIntent.TargetPlan,
                        openIntent.TargetCadence,
                        targetPlan,
                        targetCadenceApi
                    )
                )
                {
                    var reused = await TryReuseCheckoutAsync(
                        openIntent,
                        cancellationToken
                    );
                    if (reused != null)
                    {
                        if (
                            !string.Equals(
                                openIntent.IdempotencyKey,
                                idempotencyKey,
                                StringComparison.Ordinal
                            )
                        )
                        {
                            openIntent.IdempotencyKey = idempotencyKey;
                            await _context.SaveChangesAsync(cancellationToken);
                        }

                        return PayResult(reused);
                    }
                }

                openIntent.IsOpen = false;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var amounts = ComputeProration(billingAccount, targetPlan);
            if (amounts.NetAmountMinor <= 0)
            {
                throw new InvalidOperationException("upgrade_proration_zero");
            }

            var redirectUrl = BuildPlanSubscriptionRedirectUrl(
                restaurantAccountType,
                locationId
            );
            var lineName =
                $"Plan upgrade to {NormalizePlanDisplay(targetPlan)}";
            var created = await _merchant.CreateOrderAsync(
                new RevolutCreateOrderRequest(
                    AmountMinor: amounts.GrossAmountMinor,
                    Currency: "GBP",
                    PlanVariationLookupKey: lookupKey,
                    CustomerId: billingAccount.RevolutCustomerId,
                    RedirectUrl: redirectUrl,
                    Description: lineName,
                    LineItems:
                    [
                        new RevolutOrderLineItem(
                            Name: lineName,
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
                ),
                cancellationToken
            );
            if (!created.Succeeded || string.IsNullOrWhiteSpace(created.Id))
            {
                throw new InvalidOperationException(
                    created.ErrorCode ?? "revolut_http_error"
                );
            }

            var checkoutUrl = created.CheckoutUrl;
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                var order = await _merchant.GetOrderAsync(
                    created.Id,
                    cancellationToken
                );
                if (
                    !order.Succeeded
                    || string.IsNullOrWhiteSpace(order.CheckoutUrl)
                )
                {
                    throw new InvalidOperationException(
                        order.ErrorCode ?? "revolut_http_error"
                    );
                }

                checkoutUrl = order.CheckoutUrl;
            }

            _context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = created.Id,
                    RestaurantId = billingAccount.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.PlanUpgradeProration,
                    TargetPlan = NormalizePlanDisplay(targetPlan),
                    TargetCadence = targetCadenceApi.Trim().ToLowerInvariant(),
                    RevolutSubscriptionId = subscriptionId,
                    CheckoutUrl = checkoutUrl,
                    IdempotencyKey = idempotencyKey,
                    IsOpen = true,
                    NetAmountMinor = amounts.NetAmountMinor,
                    VatAmountMinor = amounts.VatAmountMinor,
                    GrossAmountMinor = amounts.GrossAmountMinor,
                    CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
                }
            );
            await _context.SaveChangesAsync(cancellationToken);

            return PayResult(checkoutUrl);
        }

        private (
            int NetAmountMinor,
            int VatAmountMinor,
            int GrossAmountMinor
        ) ComputeProration(BillingAccount billingAccount, string targetPlan)
        {
            if (billingAccount.RenewalDateUtc is not DateTime renewal)
            {
                throw new InvalidOperationException("renewal_date_required");
            }

            var book = _pricebook.GetRequired(
                billingAccount.ContractedPricebookId
            );
            var currentKey = PlanKey(billingAccount.SubscriptionPlan);
            var targetKey = PlanKey(targetPlan);
            if (
                !book.Plans.TryGetValue(currentKey, out var currentPlan)
                || !book.Plans.TryGetValue(targetKey, out var targetPlanRow)
            )
            {
                throw new InvalidOperationException("pricebook_plan_missing");
            }

            var cycle = billingAccount.BillingCycle ?? BillingCycles.Monthly;
            var currentNet = PlanUpgradeProrationMath.NetPenceForCadence(
                currentPlan.MonthlyNetPence,
                currentPlan.AnnualNetPence,
                cycle
            );
            var targetNet = PlanUpgradeProrationMath.NetPenceForCadence(
                targetPlanRow.MonthlyNetPence,
                targetPlanRow.AnnualNetPence,
                cycle
            );

            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var (periodStart, periodEnd) =
                PlanUpgradeProrationMath.ResolvePeriod(renewal, cycle);
            var ratio = PlanMigrationMath.RemainingPeriodRatio(
                periodStart,
                periodEnd,
                nowUtc
            );
            var net = PlanUpgradeProrationMath.ProratedNetPence(
                currentNet,
                targetNet,
                ratio
            );
            var vat = PlanUpgradeProrationMath.VatOnNetPence(net);
            return (net, vat, net + vat);
        }

        private async Task<string?> TryReuseCheckoutAsync(
            RevolutOrderIntent intent,
            CancellationToken cancellationToken
        )
        {
            var order = await _merchant.GetOrderAsync(
                intent.OrderId,
                cancellationToken
            );
            if (!IsPayableOrder(order))
            {
                intent.IsOpen = false;
                await _context.SaveChangesAsync(cancellationToken);
                return null;
            }

            var url = order.CheckoutUrl!;
            if (
                !string.Equals(
                    intent.CheckoutUrl,
                    url,
                    StringComparison.Ordinal
                )
            )
            {
                intent.CheckoutUrl = url;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return url;
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

        internal static bool IsPayableOrder(RevolutOrderRetrieveResult order)
        {
            if (
                !order.Succeeded
                || string.IsNullOrWhiteSpace(order.CheckoutUrl)
            )
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(order.State))
            {
                return true;
            }

            return !TerminalOrderStates.Contains(order.State);
        }

        private static bool SameTarget(
            string planA,
            string cadenceA,
            string planB,
            string cadenceB
        )
        {
            return string.Equals(planA, planB, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    cadenceA,
                    cadenceB,
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private static string PlanKey(string plan)
        {
            return plan.Trim().ToLowerInvariant();
        }

        private static string NormalizePlanDisplay(string plan)
        {
            var key = PlanKey(plan);
            return key switch
            {
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                _ => plan.Trim(),
            };
        }

        private static PlanChangeResultDto PayResult(string redirectUrl)
        {
            return new PlanChangeResultDto
            {
                Outcome = "pay",
                RedirectUrl = redirectUrl,
            };
        }
    }
}
