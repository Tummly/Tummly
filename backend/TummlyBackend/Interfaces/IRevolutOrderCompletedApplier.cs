namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Side effects for a verified <c>ORDER_COMPLETED</c> with retrieve
    /// <c>state: completed</c> and a mintable <c>billing_reason</c>
    /// (<c>setup_intent</c> / <c>cycle_billing</c>). Applies payment then
    /// <see cref="IIncludedPeriodMintService.MintOnOrderCompletedAsync"/>
    /// inside the webhook claim transaction (ticket 16).
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
