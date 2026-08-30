namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Handles ORDER_COMPLETED for refund / chargeback-family order types
    /// (ticket 25 / lock 09). Drain + TCN; never opens chargeback overlay.
    /// </summary>
    public interface IRevolutPaymentRefundCompletedHandler
    {
        Task HandleAsync(
            RevolutPaymentRefundCompletedRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record RevolutPaymentRefundCompletedRequest(
        string RefundOrderId,
        string? RelatedOrderId,
        string? OrderType,
        int? AmountMinor,
        string RawOrderBody
    );
}
