using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Tummly statutory VAT invoice row (ticket 17 / lock 05). One per
    /// Revolut order UUID; PDF is Tummly-owned.
    /// </summary>
    public class TummlyVatInvoice
    {
        public const string PrefixTm = TummlyDocumentSequence.PrefixTm;

        public const string PaymentStatusPaid = "Paid";

        public const string CurrencyGbp = "GBP";

        [Key]
        public Guid Id { get; set; }

        [MaxLength(32)]
        public string DocumentNumber { get; set; } = string.Empty;

        [MaxLength(8)]
        public string DocumentPrefix { get; set; } = PrefixTm;

        [MaxLength(128)]
        public string RevolutOrderId { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? RevolutSubscriptionId { get; set; }

        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public BillingAccount? BillingAccount { get; set; }

        public DateTime InvoiceDateUtc { get; set; }

        public DateTime TaxPointUtc { get; set; }

        [MaxLength(256)]
        public string LineDescription { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        public int NetPence { get; set; }

        public int VatRateBps { get; set; } = Helpers.TummlyVatMath.DefaultVatRateBps;

        public int VatPence { get; set; }

        public int GrossPence { get; set; }

        [MaxLength(8)]
        public string Currency { get; set; } = CurrencyGbp;

        [MaxLength(32)]
        public string PaymentStatus { get; set; } = PaymentStatusPaid;

        [MaxLength(200)]
        public string CustomerBusinessName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string CustomerAddress { get; set; } = string.Empty;

        [MaxLength(200)]
        public string SellerLegalName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string SellerRegisteredAddress { get; set; } = string.Empty;

        [MaxLength(64)]
        public string SellerVatRegistrationNumber { get; set; } = string.Empty;
    }
}
