using Microsoft.Extensions.Configuration;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Creates a zero-amount Revolut order for Hosted Payment Page card
    /// collect/update. Does not create customers or change plan.
    /// </summary>
    public sealed class PaymentMethodUpdatePaySessionService
        : IPaymentMethodUpdatePaySession
    {
        private readonly IRevolutMerchantClient _merchant;
        private readonly IConfiguration _configuration;

        public PaymentMethodUpdatePaySessionService(
            IRevolutMerchantClient merchant,
            IConfiguration configuration
        )
        {
            _merchant = merchant;
            _configuration = configuration;
        }

        public async Task<PaymentMethodUpdateSessionDto> StartAsync(
            BillingAccount billingAccount,
            string restaurantAccountType,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(billingAccount.RevolutCustomerId))
            {
                throw new InvalidOperationException("revolut_customer_required");
            }

            _merchant.EnsureReadyForCreate(planVariationLookupKey: null);

            var successRedirectUrl = BuildPaymentInvoicesRedirectUrl(
                restaurantAccountType,
                locationId
            );
            var created = await _merchant.CreateOrderAsync(
                new RevolutCreateOrderRequest(
                    AmountMinor: 0,
                    Currency: "GBP",
                    CustomerId: billingAccount.RevolutCustomerId,
                    RedirectUrl: successRedirectUrl,
                    Description: "Update payment method"
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

            return new PaymentMethodUpdateSessionDto
            {
                Outcome = "pay",
                RedirectUrl = checkoutUrl,
            };
        }

        private string BuildPaymentInvoicesRedirectUrl(
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

            return $"{baseUrl}{root}/settings/billing-credits?location={locationId}&tab=payment-invoices";
        }
    }
}
