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

        public DateTime? ProcessingStartedAtUtc { get; set; }

        public DateTime? DispatchedAtUtc { get; set; }

        public DateTime? DeliveredAtUtc { get; set; }

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        public DateTime? CancelledAtUtc { get; set; }

        public int? CancelledByUserId { get; set; }

        public User? CancelledByUser { get; set; }

        [MaxLength(2000)]
        public string? OpsNotes { get; set; }

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
