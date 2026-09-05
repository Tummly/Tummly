using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Shop materials order pay session: one-time Revolut order HPP (ticket 16).
    /// </summary>
    public sealed class ShopMaterialsOrderPaySessionService
        : IShopMaterialsOrderPaySession
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

        public ShopMaterialsOrderPaySessionService(
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
            ShopOrder order,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(billingAccount.RevolutCustomerId))
            {
                throw new InvalidOperationException("revolut_customer_required");
            }

            if (
                !string.Equals(
                    order.PaymentStatus,
                    ShopPaymentStatuses.AwaitingPayment,
                    StringComparison.Ordinal
                )
            )
            {
                throw new InvalidOperationException("shop_order_not_payable");
            }

            _merchant.EnsureReadyForCreate(planVariationLookupKey: null);

            var key = idempotencyKey.Trim();
            var shopOrderId = order.Id;

            var byKey = await _context.RevolutOrderIntents
                .Where(row =>
                    row.RestaurantId == billingAccount.RestaurantId
                    && row.Purpose == RevolutOrderIntentPurposes.ShopMaterialsOrder
                    && row.IdempotencyKey == key
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byKey != null)
            {
                if (byKey.ShopOrderId != shopOrderId)
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
                    && row.Purpose == RevolutOrderIntentPurposes.ShopMaterialsOrder
                    && row.ShopOrderId == shopOrderId
                    && row.IsOpen
                )
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (openIntent != null)
            {
                if (
                    string.Equals(
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
            }

            var vatRateBps = TummlyVatMath.DefaultVatRateBps;
            var lineItems = BuildLineItems(order, vatRateBps);
            var net = order.MaterialsNetPence + order.DeliveryNetPence;
            var vat = order.VatPence;
            var gross = order.GrossPence;
            var description = ShopMaterialsOrderLineCopy.FormatOrderDescription(
                order
            );

            var redirectUrl = RevolutHostedCheckoutRedirectUrls.BuildShopOrdersTabUrl(
                _configuration,
                restaurantAccountType,
                order.LocationId,
                new Dictionary<string, string>
                {
                    ["shopPayOutcome"] = "success",
                    ["shopOrderId"] = shopOrderId.ToString("D"),
                }
            );

            var created = await _merchant.CreateOrderAsync(
                new RevolutCreateOrderRequest(
                    AmountMinor: gross,
                    Currency: "GBP",
                    PlanVariationLookupKey: null,
                    CustomerId: billingAccount.RevolutCustomerId,
                    RedirectUrl: redirectUrl,
                    Description: description,
                    LineItems: lineItems
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
                var retrieved = await _merchant.GetOrderAsync(
                    created.Id,
                    cancellationToken
                );
                if (
                    !retrieved.Succeeded
                    || string.IsNullOrWhiteSpace(retrieved.CheckoutUrl)
                )
                {
                    throw new InvalidOperationException(
                        retrieved.ErrorCode ?? "revolut_http_error"
                    );
                }

                checkoutUrl = retrieved.CheckoutUrl;
            }

            _context.RevolutOrderIntents.Add(
                new RevolutOrderIntent
                {
                    Id = Guid.NewGuid(),
                    OrderId = created.Id.Trim(),
                    RestaurantId = billingAccount.RestaurantId,
                    Purpose = RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    TargetPlan = string.Empty,
                    TargetCadence = string.Empty,
                    RevolutSubscriptionId = string.Empty,
                    CheckoutUrl = checkoutUrl,
                    IdempotencyKey = key,
                    IsOpen = true,
                    NetAmountMinor = net,
                    VatAmountMinor = vat,
                    GrossAmountMinor = gross,
                    ShopOrderId = shopOrderId,
                    CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
                }
            );

            if (string.IsNullOrWhiteSpace(order.RevolutOrderId))
            {
                order.RevolutOrderId = created.Id.Trim();
                order.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return checkoutUrl;
        }

        private static IReadOnlyList<RevolutOrderLineItem> BuildLineItems(
            ShopOrder order,
            int vatRateBps
        )
        {
            var items = new List<RevolutOrderLineItem>();

            foreach (var line in order.Lines.OrderBy(row => row.CatalogSkuId))
            {
                var lineVat = TummlyVatMath.VatPenceFromNetPence(
                    line.LineNetPence,
                    vatRateBps
                );
                var lineGross = line.LineNetPence + lineVat;
                items.Add(
                    new RevolutOrderLineItem(
                        Name: ShopMaterialsOrderLineCopy.FormatLineName(
                            line.TitleSnapshot
                        ),
                        UnitPriceAmount: line.UnitNetPence,
                        Quantity: line.Quantity,
                        TotalAmount: lineGross,
                        Taxes:
                        [
                            new RevolutOrderLineItemTax(
                                Name: "VAT",
                                Percentage: "20.00",
                                Amount: lineVat
                            ),
                        ],
                        Type: "physical"
                    )
                );
            }

            if (order.DeliveryNetPence > 0)
            {
                items.Add(
                    new RevolutOrderLineItem(
                        Name: ShopMaterialsOrderLineCopy.ExpressDeliveryLineName,
                        UnitPriceAmount: order.DeliveryNetPence,
                        Quantity: 1,
                        TotalAmount: order.DeliveryNetPence,
                        Taxes: []
                    )
                );
            }

            return items;
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
                await ShopMaterialsOrderPaymentFailure.TryMarkFailedAsync(
                    _context,
                    intent.OrderId,
                    cancellationToken
                );
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
    }
}
