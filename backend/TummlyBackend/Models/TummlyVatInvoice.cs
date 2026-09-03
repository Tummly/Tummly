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

        public const string PrefixTcn = TummlyDocumentSequence.PrefixTcn;

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

        /// <summary>
        /// Original payment order UUID on credit notes (TCN); null on TM invoices.
        /// </summary>
        [MaxLength(128)]
        public string? RelatedRevolutOrderId { get; set; }

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

        [MaxLength(320)]
        public string? CustomerBillingEmail { get; set; }

        [MaxLength(320)]
        public string? SellerBillingEmail { get; set; }

        /// <summary>
        /// Newline-separated Deliver-to party lines (Shop ship-to snapshot).
        /// Null on billing-only invoices.
        /// </summary>
        [MaxLength(2000)]
        public string? DeliverToSnapshot { get; set; }

        /// <summary>
        /// e.g. <c>Paid via Visa ending in 4242</c>. Null when unknown.
        /// </summary>
        [MaxLength(128)]
        public string? PaymentMethodSummary { get; set; }

        /// <summary>
        /// JSON array of <see cref="Helpers.TummlyVatInvoiceLineItemDto"/>.
        /// Null falls back to single <see cref="LineDescription"/> row.
        /// </summary>
        public string? LineItemsJson { get; set; }
    }
}
