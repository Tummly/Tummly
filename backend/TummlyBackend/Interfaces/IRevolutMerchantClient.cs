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
        /// Schedules a subscription plan variation change at cycle end
        /// (<c>POST …/change-plan</c> with <c>scheduled: at_cycle_end</c>).
        /// Gates and resolves the variation id first; never PATCHes live amounts.
        /// </summary>
        Task<RevolutMerchantCreateResult> ChangeSubscriptionPlanAsync(
            string subscriptionId,
            string planVariationLookupKey,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Schedules subscription cancel at the current cycle end via
        /// <c>PATCH …/subscriptions/{id}</c> (<c>scheduled_action.type: cancel</c>).
        /// Needs Merchant API credentials; does not use the create gate.
        /// </summary>
        Task<RevolutMerchantCreateResult> ScheduleSubscriptionCancelAtCycleEndAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Cancels a Revolut subscription immediately. Needs Merchant API
        /// credentials; does not use the create gate variation map.
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
        /// Merchant refund: <c>POST …/orders/{id}/refund</c> with
        /// <c>Idempotency-Key</c>. Omit <paramref name="amountMinor"/> for a
        /// full refund when the API allows.
        /// </summary>
        Task<RevolutMerchantCreateResult> RefundOrderAsync(
            string orderId,
            int? amountMinor,
            string idempotencyKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
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

        /// <summary>
        /// Retrieves a subscription (overdue gate / cycle order refresh).
        /// </summary>
        Task<RevolutSubscriptionRetrieveResult> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutSubscriptionRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
            );

        /// <summary>
        /// Retrieves a subscription cycle (outstanding cycle <c>order_id</c>).
        /// </summary>
        Task<RevolutSubscriptionCycleRetrieveResult> GetSubscriptionCycleAsync(
            string subscriptionId,
            string cycleId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutSubscriptionCycleRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
            );

        /// <summary>
        /// Merchant-initiated Pay on a saved payment method for an order.
        /// </summary>
        Task<RevolutMerchantCreateResult> PayOrderAsync(
            RevolutPayOrderRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
            );

        /// <summary>
        /// Retrieves a dispute (resolve <c>payment.order_id</c> for
        /// <c>DISPUTE_*</c> handlers). Production Merchant API.
        /// </summary>
        Task<RevolutDisputeRetrieveResult> GetDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutDisputeRetrieveResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
            );

        /// <summary>
        /// Accepts a dispute (Production). Not the order refund endpoint.
        /// </summary>
        Task<RevolutMerchantCreateResult> AcceptDisputeAsync(
            string disputeId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
            );

        /// <summary>
        /// Challenges a dispute (Production). Use
        /// <see cref="RevolutDisputeChallengeReasons.RefundAlreadyIssued"/>
        /// when a Support <c>payment_refund</c> already completed.
        /// </summary>
        Task<RevolutMerchantCreateResult> ChallengeDisputeAsync(
            string disputeId,
            string reason,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new RevolutMerchantCreateResult(
                    Succeeded: false,
                    ErrorCode: "not_implemented"
                )
            );
    }

    public static class RevolutDisputeChallengeReasons
    {
        /// <summary>
        /// Challenge reason when Support already completed a
        /// <c>payment_refund</c> for the disputed order. Dispute accept is not
        /// the refund endpoint.
        /// </summary>
        public const string RefundAlreadyIssued = "refund_already_issued";
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
        string? Description = null,
        IReadOnlyList<RevolutOrderLineItem>? LineItems = null
    );

    public sealed record RevolutOrderLineItem(
        string Name,
        int UnitPriceAmount,
        int Quantity,
        int TotalAmount,
        IReadOnlyList<RevolutOrderLineItemTax> Taxes,
        string? ExternalId = null
    );

    public sealed record RevolutOrderLineItemTax(
        string Name,
        string Percentage,
        int Amount
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
        string? CheckoutUrl = null,
        string? OrderType = null,
        string? RelatedOrderId = null,
        int? AmountMinor = null
    );

    public static class RevolutOrderTypes
    {
        public const string Payment = "payment";

        public const string Refund = "refund";

        public const string Chargeback = "chargeback";

        public const string ChargebackReversal = "chargeback_reversal";

        public const string CreditReimbursement = "credit_reimbursement";

        public static bool IsRefundFamily(string? orderType)
        {
            if (string.IsNullOrWhiteSpace(orderType))
            {
                return false;
            }

            return orderType.Trim() is
                Refund
                or Chargeback
                or ChargebackReversal
                or CreditReimbursement;
        }
    }

    public sealed record RevolutSubscriptionRetrieveResult(
        bool Succeeded,
        string? Id = null,
        string? State = null,
        string? CurrentCycleId = null,
        string? PaymentMethodId = null,
        string? CustomerId = null,
        string? ErrorCode = null,
        string? RawBody = null
    );

    public sealed record RevolutSubscriptionCycleRetrieveResult(
        bool Succeeded,
        string? Id = null,
        string? OrderId = null,
        string? ErrorCode = null,
        string? RawBody = null
    );

    public sealed record RevolutPayOrderRequest(
        string OrderId,
        string SavedPaymentMethodId,
        string SavedPaymentMethodType = "card",
        string Initiator = "merchant"
    );

    public sealed record RevolutDisputeRetrieveResult(
        bool Succeeded,
        string? Id = null,
        string? PaymentOrderId = null,
        int? AmountMinor = null,
        string? Currency = null,
        string? ErrorCode = null,
        string? RawBody = null
    );
}
