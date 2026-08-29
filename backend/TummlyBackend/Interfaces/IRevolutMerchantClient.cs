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
        /// Creates a Revolut customer. Gates first; does not call HTTP when
        /// blocked.
        /// </summary>
        Task<RevolutMerchantCreateResult> CreateCustomerAsync(
            RevolutCreateCustomerRequest request,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Creates a Revolut subscription for a mapped plan variation. Gates
        /// with the lookup key first.
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
        string? PlanVariationLookupKey = null
    );

    public sealed record RevolutMerchantCreateResult(
        bool Succeeded,
        string? Id = null,
        string? ErrorCode = null,
        string? RawBody = null
    );
}
