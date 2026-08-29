using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// In-memory Revolut Merchant double for first-paid HPP HTTP tests.
    /// </summary>
    public sealed class FakeFirstPaidRevolutMerchantClient : IRevolutMerchantClient
    {
        public const string CheckoutUrl =
            "https://checkout.revolut.com/payment-link/fake-setup";

        public int CreateCustomerCallCount { get; private set; }

        public int CreateSubscriptionCallCount { get; private set; }

        public void EnsureReadyForCreate(string? planVariationLookupKey = null)
        {
        }

        public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
            string email,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(new RevolutListCustomersResult(Succeeded: true));
        }

        public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
            RevolutCreateCustomerRequest request,
            CancellationToken cancellationToken = default
        )
        {
            CreateCustomerCallCount++;
            return Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: $"cust_{CreateCustomerCallCount}"
                )
            );
        }

        public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
            RevolutCreateSubscriptionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            CreateSubscriptionCallCount++;
            return Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: $"sub_{CreateSubscriptionCallCount}",
                    SetupOrderId: $"ord_setup_{CreateSubscriptionCallCount}"
                )
            );
        }

        public Task<RevolutMerchantCreateResult> CreateOrderAsync(
            RevolutCreateOrderRequest request,
            CancellationToken cancellationToken = default
        )
        {
            CreateOrderCallCount++;
            LastCreateOrderRequest = request;
            return Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: $"ord_pm_{CreateOrderCallCount}",
                    CheckoutUrl: CheckoutUrl
                )
            );
        }

        public int CreateOrderCallCount { get; private set; }

        public RevolutCreateOrderRequest? LastCreateOrderRequest { get; private set; }

        public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: subscriptionId)
            );
        }

        public Task<RevolutOrderRetrieveResult> GetOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: orderId,
                    State: "pending",
                    CheckoutUrl: CheckoutUrl
                )
            );
        }
    }
}
