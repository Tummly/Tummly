using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>Minimal merchant stub for CustomPlanCycleBillingService wiring in tests.</summary>
    internal sealed class NoOpRevolutMerchantForCustomPlan : IRevolutMerchantClient
    {
        public void EnsureReadyForCreate(string? planVariationLookupKey = null)
        {
        }

        public Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
            string email,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutListCustomersResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutMerchantCreateResult> CreateCustomerAsync(
            RevolutCreateCustomerRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
            RevolutCreateSubscriptionRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutMerchantCreateResult> CreateOrderAsync(
            RevolutCreateOrderRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "noop_merchant"
                )
            );

        public Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
            string subscriptionId,
            string planVariationLookupKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutOrderRetrieveResult> GetOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutOrderRetrieveResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
            string orderId,
            string merchantReference,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutMerchantCreateResult> AcceptDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
            );

        public Task<RevolutMerchantCreateResult> ChallengeDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(Succeeded: false, ErrorCode: "noop")
            );
    }
}
