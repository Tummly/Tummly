using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Required 1:1 billing child of <see cref="Restaurant"/>.
    /// Primary key is <see cref="RestaurantId"/>.
    /// </summary>
    public class BillingAccount
    {
        [Key]
        [ForeignKey(nameof(Restaurant))]
        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        [MaxLength(320)]
        public string? BillingEmail { get; set; }

        public bool LowCreditAlertOwner { get; set; } = true;

        public bool LowCreditAlertAdmin { get; set; }

        public bool LowCreditAlertBillingContact { get; set; } = true;

        public bool PaymentFailureAlertOwner { get; set; } = true;

        public bool PaymentFailureAlertBillingContact { get; set; } = true;
    }
}
