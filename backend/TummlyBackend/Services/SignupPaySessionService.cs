using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// One-time Revolut order for signup paid plan checkout
    /// (<see cref="RevolutOrderIntentPurposes.SignupPlan"/>).
    /// </summary>
    public sealed class SignupPaySessionService : ISignupPaySession
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;
        private readonly IPricebookCatalog _pricebook;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _clock;

        public SignupPaySessionService(
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

        public async Task<string> StartAsync(
            PendingSignup pending,
            string targetPlan,
            string targetCadenceApi,
            CancellationToken cancellationToken = default
        )
        {
            var plan = NormalizePlanDisplay(targetPlan);
            var cadence = string.IsNullOrWhiteSpace(targetCadenceApi)
                ? "monthly"
                : targetCadenceApi.Trim().ToLowerInvariant();

            var lookupKey = RevolutPlanVariationKeys.ForPlanCadence(plan, cadence);
            if (lookupKey == null)
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            _merchant.EnsureReadyForCreate(lookupKey);

            var idempotencyKey = $"signup:{pending.Id:N}:{plan}:{cadence}";

            var byKey = await _context.RevolutOrderIntents
                .Where(row =>
                    row.PendingSignupId == pending.Id
                    && row.Purpose == RevolutOrderIntentPurposes.SignupPlan
                    && row.IdempotencyKey == idempotencyKey
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byKey != null && byKey.IsOpen)
            {
                var reused = await TryReuseCheckoutAsync(byKey, cancellationToken);
                if (reused != null)
                {
                    return reused;
                }
            }

            var openIntent = await _context.RevolutOrderIntents
                .Where(row =>
                    row.PendingSignupId == pending.Id
                    && row.Purpose == RevolutOrderIntentPurposes.SignupPlan
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
                        plan,
                        cadence
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

                        return reused;
                    }
                }

                openIntent.IsOpen = false;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var amounts = ResolvePlanAmounts(plan, cadence);
            var redirectUrl = BuildSignupProvisioningRedirectUrl();
            var lineName = $"Tummly {plan} ({cadence})";

            var created = await _merchant.CreateOrderAsync(
                new RevolutCreateOrderRequest(
                    AmountMinor: amounts.GrossAmountMinor,
                    Currency: "GBP",
                    PlanVariationLookupKey: lookupKey,
                    CustomerId: null,
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
                    OrderId = created.Id.Trim(),
                    RestaurantId = null,
                    PendingSignupId = pending.Id,
                    Purpose = RevolutOrderIntentPurposes.SignupPlan,
                    TargetPlan = plan,
                    TargetCadence = cadence,
                    RevolutSubscriptionId = string.Empty,
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

            return checkoutUrl;
        }

        private (
            int NetAmountMinor,
            int VatAmountMinor,
            int GrossAmountMinor
        ) ResolvePlanAmounts(string plan, string cadenceApi)
        {
            var book = _pricebook.GetRequired(_pricebook.CurrentPricebookId);
            var key = plan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(key, out var pricebookPlan))
            {
                throw new InvalidOperationException("invalid_plan_target");
            }

            var billingCycle = string.Equals(
                cadenceApi,
                "annual",
                StringComparison.OrdinalIgnoreCase
            )
                ? BillingCycles.Annual
                : BillingCycles.Monthly;

            var net = PlanUpgradeProrationMath.NetPenceForCadence(
                pricebookPlan.MonthlyNetPence,
                pricebookPlan.AnnualNetPence,
                billingCycle
            );
            if (net <= 0)
            {
                throw new InvalidOperationException("invalid_plan_amount");
            }

            var vat = TummlyVatMath.VatPenceFromNetPence(
                net,
                TummlyVatMath.DefaultVatRateBps
            );
            return (net, vat, net + vat);
        }

        private string BuildSignupProvisioningRedirectUrl()
        {
            var baseUrl = RevolutHostedCheckoutRedirectUrls.ResolveRedirectBaseUrl(
                _configuration
            );
            RevolutHostedCheckoutRedirectUrls.ValidateRevolutRedirectHost(baseUrl);
            return $"{baseUrl}/signup/provisioning?pay=return";
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
            if (!CreditTopUpPaySessionService.IsPayableOrder(order))
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

        private static string NormalizePlanDisplay(string plan)
        {
            return plan.Trim().ToLowerInvariant() switch
            {
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                "group" => BillingSubscriptionPlans.Group,
                _ => throw new InvalidOperationException("invalid_plan_target"),
            };
        }
    }
}
