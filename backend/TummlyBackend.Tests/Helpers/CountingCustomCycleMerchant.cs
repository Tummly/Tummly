using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Revolut stub that counts order creations, so a test can prove no order was
    /// started. Set <see cref="FailCreateOrder"/> to refuse the order instead.
    /// </summary>
    public sealed class CountingCustomCycleMerchant : IRevolutMerchantClient
    {
        public const string CheckoutUrl =
            "https://checkout.revolut.com/payment-link/fake-custom";

        public int CreateOrderCallCount { get; private set; }

        /// <summary>When true, CreateOrder fails (no checkout), as in the signup tests that need a failed start.</summary>
        public bool FailCreateOrder { get; set; }

        public RevolutCreateOrderRequest? LastCreateOrderRequest { get; private set; }

        public void EnsureReadyForCreate(string? planVariationLookupKey = null)
        {
        }

        public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
            string email,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new RevolutListCustomersResult(Succeeded: true));

        public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
            RevolutCreateCustomerRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: "cust")
            );

        public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
            RevolutCreateSubscriptionRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: "sub")
            );

        public Task<RevolutMerchantCreateResult> CreateOrderAsync(
            RevolutCreateOrderRequest request,
            CancellationToken cancellationToken = default
        )
        {
            CreateOrderCallCount++;
            LastCreateOrderRequest = request;
            if (FailCreateOrder)
            {
                return Task.FromResult(
                    new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
                );
            }

            return Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: $"ord_custom_{CreateOrderCallCount}",
                    CheckoutUrl: CheckoutUrl
                )
            );
        }

        public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
            string subscriptionId,
            string planVariationLookupKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: subscriptionId)
            );

        public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: subscriptionId)
            );

        public Task<RevolutOrderRetrieveResult> GetOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: orderId,
                    State: "pending",
                    CheckoutUrl: CheckoutUrl
                )
            );

        public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
            string orderId,
            string merchantReference,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: orderId)
            );

        public Task<RevolutMerchantCreateResult> AcceptDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: disputeId)
            );

        public Task<RevolutMerchantCreateResult> ChallengeDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: true, Id: disputeId)
            );
    }
}
