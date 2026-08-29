namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Revolut Merchant API client. Live create methods refuse via
    /// <see cref="IRevolutMerchantCreateGate"/> before any HTTP call.
    /// </summary>
    public interface IRevolutMerchantClient
    {
        /// <summary>
        /// Ensures VAT/legal, Revolut Merchant config, and optional plan
        /// variation are present. Throws
        /// <see cref="RevolutMerchantNotReadyException"/> when blocked.
        /// </summary>
        void EnsureReadyForCreate(string? planVariationLookupKey = null);

        /// <summary>
        /// Lists Revolut customers by email. Gates first; does not call HTTP
        /// when blocked. Revolut does not dedupe — prefer list before create.
        /// </summary>
        Task<RevolutListCustomersResult> ListCustomersByEmailAsync(
            string email,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Creates a Revolut customer. Gates first; does not call HTTP when
        /// blocked.
        /// </summary>
        Task<RevolutMerchantCreateResult> CreateCustomerAsync(
            RevolutCreateCustomerRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Creates a Revolut subscription for a mapped plan variation. Gates
        /// with the lookup key first. Response may include
        /// <see cref="RevolutMerchantCreateResult.SetupOrderId"/>.
        /// </summary>
        Task<RevolutMerchantCreateResult> CreateSubscriptionAsync(
            RevolutCreateSubscriptionRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Creates a one-time pay order. Gates without a variation map entry
        /// unless <see cref="RevolutCreateOrderRequest.PlanVariationLookupKey"/>
        /// is set.
        /// </summary>
        Task<RevolutMerchantCreateResult> CreateOrderAsync(
            RevolutCreateOrderRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Cancels a Revolut subscription (abandon pending setup). Needs
        /// Merchant API credentials; does not use the create gate variation map.
        /// </summary>
        Task<RevolutMerchantCreateResult> CancelSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Retrieves an order by id (webhook retrieve gate / setup checkout).
        /// Does not use the Merchant create gate; still needs Merchant API
        /// credentials.
        /// </summary>
        Task<RevolutOrderRetrieveResult> GetOrderAsync(
            string orderId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// PATCHes <c>merchant_order_data.reference</c> with the Tummly
        /// invoice number (support link). Does not use the create gate.
        /// </summary>
        Task<RevolutMerchantCreateResult> UpdateOrderMerchantReferenceAsync(
            string orderId,
            string merchantReference,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class RevolutMerchantNotReadyException : Exception
    {
        public RevolutMerchantNotReadyException(string code)
            : base(code)
        {
            Code = code;
        }

        public string Code { get; }
    }

    public sealed record RevolutCreateCustomerRequest(
        string Email,
        string? FullName = null
    );

    public sealed record RevolutCreateSubscriptionRequest(
        string CustomerId,
        string PlanVariationLookupKey,
        string? SetupOrderRedirectUrl = null
    );

    public sealed record RevolutCreateOrderRequest(
        int AmountMinor,
        string Currency,
        string? PlanVariationLookupKey = null,
        string? CustomerId = null,
        string? RedirectUrl = null,
        string? Description = null
    );

    public sealed record RevolutMerchantCreateResult(
        bool Succeeded,
        string? Id = null,
        string? ErrorCode = null,
        string? RawBody = null,
        string? SetupOrderId = null,
        string? CheckoutUrl = null
    );

    public sealed record RevolutListCustomersResult(
        bool Succeeded,
        string? FirstCustomerId = null,
        string? ErrorCode = null,
        string? RawBody = null
    );

    public sealed record RevolutOrderRetrieveResult(
        bool Succeeded,
        string? Id = null,
        string? State = null,
        string? BillingReason = null,
        string? SubscriptionId = null,
        string? ErrorCode = null,
        string? RawBody = null,
        string? CheckoutUrl = null
    );
}
