namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Delivers low-credit and payment-failure alerts as Email plus Account notices.
    /// Sister maps emit threshold crosses and dunning day-steps; this service wires reach.
    /// </summary>
    public interface IBillingAccountNoticeNotifier
    {
        Task NotifyCreditThresholdCrossedAsync(
            int restaurantId,
            string channel,
            int thresholdBand,
            string periodKey,
            string billingStatus,
            bool isPilot,
            CancellationToken cancellationToken = default
        );

        Task NotifyPaymentFailureDayStepAsync(
            int restaurantId,
            int dayStep,
            string episodeId,
            CancellationToken cancellationToken = default
        );

        Task NotifyUnpaidPilotLockEnterAsync(
            int restaurantId,
            string episodeKey,
            CancellationToken cancellationToken = default
        );
    }
}
