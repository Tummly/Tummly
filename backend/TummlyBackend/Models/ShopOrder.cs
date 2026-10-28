using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class ShopPaymentStatuses
    {
        public const string AwaitingPayment = "awaiting_payment";
        public const string Paid = "paid";
        public const string PaymentFailed = "payment_failed";
        public const string Refunded = "refunded";
    }

    public static class ShopFulfilmentStatuses
    {
        public const string Processing = "processing";
        public const string InTransit = "in_transit";
        public const string Delivered = "delivered";
        public const string Cancelled = "cancelled";
    }

    /// <summary>
    /// Who or what recorded a fulfilment evidence timestamp. Admin transitions
    /// stamp <see cref="AdminManual"/> and the acting Admin user id.
    /// </summary>
    public static class ShopFulfilmentEvidenceSources
    {
        public const string AdminManual = "admin_manual";
    }

    public static class ShopOrderReviewReasons
    {
        /// <summary>
        /// Payment completed after the QR slot was released and the plan limit
        /// had no room for a new code. Admin resolves it by hand; the money is
        /// never refunded automatically.
        /// </summary>
        public const string QrCapacityUnavailableAfterPayment =
            "qr_capacity_unavailable_after_payment";

        /// <summary>
        /// A complimentary starter order whose Location has no Active code of a
        /// starter type because the Workspace QR pool is full (the code minted
        /// Paused). The order waits until an Active code exists.
        /// </summary>
        public const string QrCapacityUnavailable = "qr_capacity_unavailable";
    }

    public static class ShopDeliveryMethods
    {
        public const string Standard = "standard";
        public const string Express = "express";
    }

    /// <summary>
    /// Paid Restaurant purchase of physical QR materials (schema for ticket 15+).
    /// </summary>
    public class ShopOrder
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(32)]
        public string OrderNumber { get; set; } = string.Empty;

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        public int LocationId { get; set; }

        public RestaurantLocation Location { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string LocationNameSnapshot { get; set; } = string.Empty;

        public int PlacedByUserId { get; set; }

        public User PlacedByUser { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string PlacedByNameSnapshot { get; set; } = string.Empty;

        public int MaterialsNetPence { get; set; }

        public int VatPence { get; set; }

        public int DeliveryNetPence { get; set; }

        public int GrossPence { get; set; }

        [Required]
        [MaxLength(16)]
        public string DeliveryMethod { get; set; } = ShopDeliveryMethods.Standard;

        [Required]
        [MaxLength(32)]
        public string PaymentStatus { get; set; } =
            ShopPaymentStatuses.AwaitingPayment;

        [MaxLength(128)]
        public string? RevolutOrderId { get; set; }

        public DateTime? PaidAtUtc { get; set; }

        [MaxLength(32)]
        public string? FulfilmentStatus { get; set; }

        [MaxLength(2048)]
        public string? TrackingUrl { get; set; }

        /// <summary>
        /// Set when the order is confirmed (paid or complimentary). Not the
        /// preparation start: that is <see cref="PreparationStartedAtUtc"/>.
        /// </summary>
        public DateTime? ProcessingStartedAtUtc { get; set; }

        /// <summary>Admin recorded that order preparation has started.</summary>
        public DateTime? PreparationStartedAtUtc { get; set; }

        public int? PreparationStartedByAdminUserId { get; set; }

        /// <summary>Production-start evidence: "In production" customer status.</summary>
        public DateTime? ProductionStartedAtUtc { get; set; }

        public int? ProductionStartedByAdminUserId { get; set; }

        [MaxLength(32)]
        public string? ProductionStartSource { get; set; }

        /// <summary>Supplier or carrier dispatch evidence: "Dispatched" customer status.</summary>
        public DateTime? DispatchedAtUtc { get; set; }

        public int? DispatchedByAdminUserId { get; set; }

        [MaxLength(32)]
        public string? DispatchSource { get; set; }

        [MaxLength(200)]
        public string? DispatchCarrierName { get; set; }

        [MaxLength(200)]
        public string? DispatchCarrierReference { get; set; }

        public DateTime? DeliveredAtUtc { get; set; }

        [MaxLength(32)]
        public string? DeliverySource { get; set; }

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        public DateTime? CancelledAtUtc { get; set; }

        public int? CancelledByUserId { get; set; }

        public User? CancelledByUser { get; set; }

        [MaxLength(2000)]
        public string? OpsNotes { get; set; }

        /// <summary>
        /// Set when a paid order needs manual review, for example
        /// <see cref="ShopOrderReviewReasons.QrCapacityUnavailableAfterPayment"/>.
        /// </summary>
        [MaxLength(64)]
        public string? ReviewReason { get; set; }

        [Required]
        [MaxLength(200)]
        public string ShipToContactName { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? ShipToContactPhone { get; set; }

        [Required]
        [MaxLength(500)]
        public string ShipToAddressLine1 { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ShipToAddressLine2 { get; set; }

        [Required]
        [MaxLength(16)]
        public string ShipToPostcode { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string ShipToCountry { get; set; } = "United Kingdom";

        [MaxLength(500)]
        public string? DeliveryInstructions { get; set; }

        /// <summary>
        /// True for £0 starter QR materials orders created at provision / location add.
        /// </summary>
        public bool IsComplimentary { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ShopOrderLine> Lines { get; set; } =
            new List<ShopOrderLine>();
    }

    public class ShopOrderLine
    {
        public Guid Id { get; set; }

        public Guid ShopOrderId { get; set; }

        public ShopOrder ShopOrder { get; set; } = null!;

        [Required]
        [MaxLength(80)]
        public string CatalogSkuId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TitleSnapshot { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string MaterialType { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int UnitNetPence { get; set; }

        public int LineNetPence { get; set; }
    }
}
