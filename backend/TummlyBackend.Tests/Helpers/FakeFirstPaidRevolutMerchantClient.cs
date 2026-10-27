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
                    Id: $"{OrderIdPrefix}{CreateOrderCallCount}",
                    CheckoutUrl: CheckoutUrl
                )
            );
        }

        public int CreateOrderCallCount { get; private set; }

        /// <summary>
        /// Prefix of created order ids. Tests sharing one SQL database set a
        /// unique prefix so intents do not collide across factories.
        /// </summary>
        public string OrderIdPrefix { get; set; } = "ord_pm_";

        public RevolutCreateOrderRequest? LastCreateOrderRequest { get; private set; }

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

        /// <summary>
        /// Null: subscription read fails. Otherwise the read succeeds with
        /// this <c>scheduled_action</c> presence.
        /// </summary>
        public bool? SubscriptionHasScheduledAction { get; set; }

        public Task<RevolutSubscriptionRetrieveResult> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(
            SubscriptionHasScheduledAction == null
                ? new RevolutSubscriptionRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
                : new RevolutSubscriptionRetrieveResult(
                    Succeeded: true,
                    Id: subscriptionId,
                    State: "active",
                    HasScheduledAction: SubscriptionHasScheduledAction
                )
        );

        public int CancelSubscriptionCallCount { get; private set; }

        public string? LastCancelledSubscriptionId { get; private set; }


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
            if (Orders.TryGetValue(orderId, out var configured))
            {
                return Task.FromResult(configured);
            }

            return Task.FromResult(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: orderId,
                    State: "pending",
                    AmountMinor: 1200,
                    CheckoutUrl: CheckoutUrl
                )
            );
        }

        public Dictionary<string, RevolutOrderRetrieveResult> Orders { get; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Scripted <see cref="CancelOrderAsync"/> answers by Revolut order id.
        /// An order with no entry cancels successfully.
        /// </summary>
        public Dictionary<string, RevolutOrderRetrieveResult> CancelResults { get; } =
            new(StringComparer.Ordinal);

        public int CancelOrderCallCount { get; private set; }

        public Task<RevolutOrderRetrieveResult> CancelOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default
        )
        {
            CancelOrderCallCount++;
            if (CancelResults.TryGetValue(orderId, out var scripted))
            {
                return Task.FromResult(scripted);
            }

            return Task.FromResult(
                new RevolutOrderRetrieveResult(
                    Succeeded: true,
                    Id: orderId,
                    State: "cancelled"
                )
            );
        }

        public int RefundOrderCallCount { get; private set; }

        public string? LastRefundOrderId { get; private set; }

        public int? LastRefundAmountMinor { get; private set; }

        public string? LastRefundIdempotencyKey { get; private set; }

        public bool NextRefundFails { get; set; }

        public Task<RevolutMerchantCreateResult> RefundOrderAsync(
            string orderId,
            int? amountMinor,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        )
        {
            RefundOrderCallCount++;
            LastRefundOrderId = orderId;
            LastRefundAmountMinor = amountMinor;
            LastRefundIdempotencyKey = idempotencyKey;
            if (NextRefundFails)
            {
                NextRefundFails = false;
                return Task.FromResult(
                    new RevolutMerchantCreateResult(
                        Succeeded: false,
                        ErrorCode: "revolut_refund_failed"
                    )
                );
            }

            return Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: true,
                    Id: $"ord_refund_{RefundOrderCallCount}"
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
