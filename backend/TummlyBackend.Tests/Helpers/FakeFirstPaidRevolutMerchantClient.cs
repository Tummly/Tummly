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

<<<<<<< HEAD
        public int ChangeSubscriptionPlanCallCount { get; private set; }

        public string? LastChangePlanSubscriptionId { get; private set; }

        public string? LastChangePlanLookupKey { get; private set; }

        public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
            string subscriptionId,
            string planVariationLookupKey,
            CancellationToken cancellationToken = default
        )
        {
            ChangeSubscriptionPlanCallCount++;
            LastChangePlanSubscriptionId = subscriptionId;
            LastChangePlanLookupKey = planVariationLookupKey;
            return Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: subscriptionId
                )
            );
        }
=======
        public int CancelSubscriptionCallCount { get; private set; }

        public string? LastCancelledSubscriptionId { get; private set; }
>>>>>>> 4e39a19c (Call Revolut cancel only at period end for cancel-at-period-end (ticket 23).)

        public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        )
        {
            CancelSubscriptionCallCount++;
            LastCancelledSubscriptionId = subscriptionId;
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

        public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
            string orderId,
            string merchantReference,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
            );
        }
    }
}
