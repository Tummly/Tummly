using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class ShopQrSlotReservationStatuses
    {
        public const string Open = "open";
        public const string Converted = "converted";
        public const string Released = "released";
        public const string Review = "review";
    }

    /// <summary>
    /// Checkout hold on one QR placement (location + QR type) for a Shop
    /// order. Held reservations (<c>open</c> or <c>review</c>) count toward
    /// the active-QR capacity (spec 4.4.2). One row per (order, QR type).
    /// </summary>
    public class ShopQrSlotReservation
    {
        public Guid Id { get; set; }

        public Guid ShopOrderId { get; set; }

        public ShopOrder? ShopOrder { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public QrType QrType { get; set; }

        /// <summary>
        /// Paused code the resolver found at reservation time; reactivated on
        /// conversion when still Paused.
        /// </summary>
        public int? ReactivateQrCodeId { get; set; }

        [Required]
        [MaxLength(16)]
        public string Status { get; set; } = ShopQrSlotReservationStatuses.Open;

        /// <summary>
        /// Created time + 30 minutes.
        /// </summary>
        public DateTime ReconcileAfterUtc { get; set; }

        public int ReconcileAttempts { get; set; }

        public DateTime? LastReconcileAtUtc { get; set; }

        [MaxLength(500)]
        public string? LastReconcileError { get; set; }

        /// <summary>
        /// QR code made or reactivated when the reservation converted.
        /// </summary>
        public int? ConvertedQrCodeId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
