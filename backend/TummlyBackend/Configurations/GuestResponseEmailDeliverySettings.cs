namespace TummlyBackend.Configurations
{
    /// <summary>
    /// Background settings for <c>Guest response email delivery</c> (ADR-0026).
    /// </summary>
    public class GuestResponseEmailDeliverySettings
    {
        public const string SectionName = "GuestResponseEmailDelivery";

        /// <summary>Soft-claim lease before another worker may reclaim.</summary>
        public int ClaimLeaseMinutes { get; set; } = 10;

        /// <summary>
        /// Delay after a failed Resend before the row is claimable again.
        /// </summary>
        public int RetryBackoffSeconds { get; set; } = 30;

        /// <summary>
        /// How often the worker sweeps Pending rows (plus Channel wake-ups).
        /// </summary>
        public int SweepIntervalSeconds { get; set; } = 30;
    }
}
