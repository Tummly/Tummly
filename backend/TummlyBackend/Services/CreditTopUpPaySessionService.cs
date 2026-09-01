using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Credit-pack top-up pay session: one-time Revolut order HPP (ticket 18).
    /// </summary>
    public sealed class CreditTopUpPaySessionService : ICreditTopUpPaySession
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
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _clock;

        public CreditTopUpPaySessionService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IConfiguration configuration,
            TimeProvider clock
        )
        {
            _context = context;
            _merchant = merchant;
            _configuration = configuration;
            _clock = clock;
        }

        public async Task<string> StartAsync(
            BillingAccount billingAccount,
            string restaurantAccountType,
            int locationId,
            PricebookTopUpPack pack,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(billingAccount.RevolutCustomerId))
            {
                throw new InvalidOperationException("revolut_customer_required");
            }

            _merchant.EnsureReadyForCreate(planVariationLookupKey: null);

            var channel = pack.Channel.Trim().ToLowerInvariant();
            var key = idempotencyKey.Trim();

            var byKey = await _context.RevolutOrderIntents
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.Purpose == RevolutOrderIntentPurposes.Topup
                    && row.IdempotencyKey == key
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byKey != null)
            {
                if (!SamePack(byKey, channel, pack.Quantity))
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
                        return reused;
                    }
                }
            }

            var openIntent = await _context.RevolutOrderIntents
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.Purpose == RevolutOrderIntentPurposes.Topup
                    && row.IsOpen
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (openIntent != null)
            {
                if (
                    SamePack(openIntent, channel, pack.Quantity)
                    && string.Equals(
                        openIntent.IdempotencyKey,
                        key,
                        StringComparison.Ordinal
                    )
                )
                {
                    var reused = await TryReuseCheckoutAsync(
                        openIntent,
                        cancellationToken
                    );
                    if (reused != null)
                    {
                        return reused;
                    }
                }

                // New key → new order; leave prior pending unpaid (lock 07).
            }

            var vatRateBps = TummlyVatMath.DefaultVatRateBps;
            var net = pack.NetPence;
            var vat = TummlyVatMath.VatPenceFromNetPence(net, vatRateBps);
            var gross = net + vat;
            var lineName = CreditTopUpLineCopy.FormatLineDescription(
                channel,
                pack.Quantity
            );
            var redirectUrl = RevolutHostedCheckoutRedirectUrls.BuildBillingCreditsTabUrl(
                _configuration,
                restaurantAccountType,
                locationId,
                "credits-usage",
                new Dictionary<string, string>
                {
                    ["topUpOutcome"] = "success",
                }
            );

            var created = await _merchant.CreateOrderAsync(
                new RevolutCreateOrderRequest(
                    AmountMinor: gross,
                    Currency: "GBP",
                    PlanVariationLookupKey: null,
                    CustomerId: billingAccount.RevolutCustomerId,
                    RedirectUrl: redirectUrl,
                    Description: lineName,
                    LineItems:
                    [
                        new RevolutOrderLineItem(
                            Name: lineName,
                            UnitPriceAmount: net,
                            Quantity: 1,
                            TotalAmount: gross,
                            Taxes:
                            [
                                new RevolutOrderLineItemTax(
                                    Name: "VAT",
                                    Percentage: "20.00",
                                    Amount: vat
                                ),
                            ],
                            ExternalId: pack.LookupKey
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
                    RestaurantId = billingAccount.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.Topup,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = checkoutUrl,
                    IdempotencyKey = key,
                    IsOpen = true,
                    NetAmountMinor = net,
                    VatAmountMinor = vat,
                    GrossAmountMinor = gross,
                    Channel = channel,
                    Quantity = pack.Quantity,
                    PackLookupKey = pack.LookupKey,
                    CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
                }
            );
            await _context.SaveChangesAsync(cancellationToken);

            return checkoutUrl;
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

        private static bool SamePack(
            RevolutOrderIntent intent,
            string channel,
            int quantity
        )
        {
            return string.Equals(
                    intent.Channel,
                    channel,
                    StringComparison.OrdinalIgnoreCase
                )
                && intent.Quantity == quantity;
        }

    }
}
