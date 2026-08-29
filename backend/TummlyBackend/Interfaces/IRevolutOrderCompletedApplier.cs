namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Side effects for a verified <c>ORDER_COMPLETED</c> with retrieve
    /// <c>state: completed</c>. Ticket 15 ships a no-op; ticket 16+ mints
    /// and applies payment inside the same claim transaction.
    /// </summary>
    public interface IRevolutOrderCompletedApplier
    {
        Task ApplyAsync(
            RevolutOrderCompletedApplyRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record RevolutOrderCompletedApplyRequest(
        string OrderId,
        string OrderState,
        string? BillingReason,
        string? SubscriptionId,
        string RawWebhookBody,
        string RawOrderBody
    );
}
