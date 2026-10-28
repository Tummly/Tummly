using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Starts a Revolut cycle order for a Custom plan from the snapshotted period amount.
    /// </summary>
    public sealed class CustomPlanCycleBillingService
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
        private readonly IRevolutMerchantClient _revolut;
        private readonly TimeProvider _clock;
        private readonly TummlySellerVatSettings _sellerVat;
        private readonly ICustomPlanQuoteService _quote;

        public CustomPlanCycleBillingService(
            ApplicationDbContext context,
            IRevolutMerchantClient revolut,
            TimeProvider clock,
            IOptions<TummlySellerVatSettings> sellerVat,
            ICustomPlanQuoteService quote
        )
        {
            _context = context;
            _revolut = revolut;
            _clock = clock;
            _sellerVat = sellerVat.Value;
            _quote = quote;
        }

        /// <summary>
        /// Re-quote a saved Custom config on the server before any charge that does
        /// not come from a fresh review (a retried signup or pending-payment checkout).
        /// Throws <see cref="CustomPlanQuoteException"/>: <c>legacy_pack_key</c> for a
        /// pack the catalogue no longer sells, <c>quote_changed</c> when the saved
        /// quote hash is missing or no longer matches the current price. The customer
        /// then reviews again; no Revolut order is started.
        /// </summary>
        public async Task<CustomPlanQuoteResult> VerifySavedConfigAsync(
            string? savedConfigJson,
            CancellationToken cancellationToken = default
        )
        {
            var saved = CustomPlanConfig.TryParse(savedConfigJson);
            if (saved == null || !saved.IsV2)
            {
                throw new CustomPlanQuoteException(
                    "custom_plan_config_invalid",
                    "The saved plan could not be read."
                );
            }

            var quote = await _quote.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = saved.Cadence,
                    BasePlan = saved.BasePlan,
                    Locations = saved.Locations ?? saved.Quantities.Locations,
                    Packs = saved.Packs?.ToDictionary(
                        kv => kv.Key,
                        kv => (decimal)kv.Value,
                        StringComparer.Ordinal
                    ),
                },
                cancellationToken
            );
            if (
                string.IsNullOrWhiteSpace(saved.QuoteHash)
                || !string.Equals(saved.QuoteHash, quote.QuoteHash, StringComparison.Ordinal)
            )
            {
                throw new CustomPlanQuoteException(
                    "quote_changed",
                    "Your quote has changed. Review the updated total before continuing."
                );
            }

            return quote;
        }

        public async Task<(string? CheckoutUrl, string? Error)> StartCyclePayAsync(
            int restaurantId,
            string redirectUrl,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default,
            CustomPlanConfig? quoteOverride = null
        )
        {
            var account = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account == null)
            {
                return (null, "billing_account_missing");
            }

            // A pending Custom retry passes the saved quote while the account is still Free.
            if (
                quoteOverride == null
                && !string.Equals(
                    account.SubscriptionPlan,
                    BillingSubscriptionPlans.Custom,
                    StringComparison.Ordinal
                )
            )
            {
                return (null, "not_custom_plan");
            }

            var config = quoteOverride
                ?? CustomPlanConfig.TryParse(account.CustomPlanConfigJson);
            if (config == null || config.PeriodNetPence <= 0)
            {
                return (null, "custom_plan_config_invalid");
            }

            var key = string.IsNullOrWhiteSpace(idempotencyKey)
                ? $"custom-cycle-{restaurantId}-{Guid.NewGuid():N}"
                : idempotencyKey.Trim();

            var byKey = await _context.RevolutOrderIntents
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Purpose == RevolutOrderIntentPurposes.CustomPlanCycle
                    && row.IdempotencyKey == key
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byKey != null && byKey.IsOpen)
            {
                var reused = await TryReuseCheckoutAsync(byKey, cancellationToken);
                if (reused != null)
                {
                    return (reused, null);
                }
            }

            var net = config.PeriodNetPence;
            var vatRateBps = _sellerVat.EffectiveVatRateBps;
            var vat = TummlyVatMath.VatPenceFromNetPence(net, vatRateBps);
            var gross = net + vat;
            const string lineName = "Tummly Custom plan";

            var created = await _revolut.CreateOrderAsync(
                new RevolutCreateOrderRequest(
                    AmountMinor: gross,
                    Currency: "GBP",
                    PlanVariationLookupKey: null,
                    CustomerId: account.RevolutCustomerId,
                    RedirectUrl: redirectUrl,
                    Description: lineName,
                    LineItems:
                    [
                        new RevolutOrderLineItem(
                            Name: lineName,
                            UnitPriceAmount: net,
                            Quantity: 1,
                            TotalAmount: gross,
                            Taxes: BuildVatTaxes(vatRateBps, vat)
                        ),
                    ]
                ),
                cancellationToken
            );

            if (!created.Succeeded || string.IsNullOrWhiteSpace(created.Id))
            {
                return (null, created.ErrorCode ?? "revolut_http_error");
            }

            var checkoutUrl = created.CheckoutUrl;
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                var order = await _revolut.GetOrderAsync(
                    created.Id,
                    cancellationToken
                );
                if (
                    !order.Succeeded
                    || string.IsNullOrWhiteSpace(order.CheckoutUrl)
                )
                {
                    return (null, order.ErrorCode ?? "revolut_http_error");
                }

                checkoutUrl = order.CheckoutUrl;
            }

            _context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = created.Id.Trim(),
                    RestaurantId = restaurantId,
                    Purpose = RevolutOrderIntentPurposes.CustomPlanCycle,
                    TargetPlan = BillingSubscriptionPlans.Custom,
                    TargetCadence = string.Equals(
                        config.Cadence,
                        "annual",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "annual"
                        : "monthly",
                    CustomPlanQuoteHash = config.QuoteHash,
                    CheckoutUrl = checkoutUrl,
                    IdempotencyKey = key,
                    IsOpen = true,
                    NetAmountMinor = net,
                    VatAmountMinor = vat,
                    GrossAmountMinor = gross,
                    CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
                }
            );
            await _context.SaveChangesAsync(cancellationToken);
            return (checkoutUrl, null);
        }

        private async Task<string?> TryReuseCheckoutAsync(
            RevolutOrderIntent intent,
            CancellationToken cancellationToken
        )
        {
            var order = await _revolut.GetOrderAsync(
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

        private static IReadOnlyList<RevolutOrderLineItemTax> BuildVatTaxes(
            int vatRateBps,
            int vatAmount
        )
        {
            if (vatRateBps <= 0)
            {
                return [];
            }

            return
            [
                new RevolutOrderLineItemTax(
                    Name: "VAT",
                    Percentage: "20.00",
                    Amount: vatAmount
                ),
            ];
        }
    }
}
