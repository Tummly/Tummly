namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Revolut native cancel (ticket 23). Confirm day makes no Revolut call;
    /// the included-period job calls this from
    /// <c>IncludedPeriodMintService.RevolutCancelLead</c> before renewal.
    /// </summary>
    public interface IRevolutCancelAtPeriodEndAdapter
    {
        /// <summary>
        /// Cancels the live Revolut subscription for the restaurant (native
        /// immediate API). Resolves subscription id from pay-session
        /// correlation. No-op when no subscription id is stored. Safe to call
        /// again: skips the cancel when Revolut already ended the subscription.
        /// </summary>
        Task CancelNativeSubscriptionAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }
}
