using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Per-channel credit-warning watermark for one Billing Account.
    /// </summary>
    public class CreditWarningState
    {
        public int RestaurantId { get; set; }

        public BillingAccount BillingAccount { get; set; } = null!;

        [MaxLength(16)]
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// Highest band emitted this period: 0, 80, 90, or 100.
        /// </summary>
        public int HighestBandThisPeriod { get; set; }
    }

    public static class CreditThresholdBands
    {
        public const int None = 0;

        public const int Band80 = 80;

        public const int Band90 = 90;

        public const int Band100 = 100;

        public static readonly IReadOnlyList<int> Ordered = [Band80, Band90, Band100];
    }
}
