namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only Smart Guest Link / QR open event. Recorded when
    /// GET /api/scan/{token} resolves successfully (physical QR or shared
    /// link). Operator Preview (<c>preview=true</c>) and product-demo visits are
    /// not recorded. Powers Performance overview QR scans KPI counts.
    /// </summary>
    public class QrScanEvent
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        /// <summary>
        /// The QR code scanned. Nullable for historical rows recorded before
        /// per-location QR codes existed (backfilled onto the location's
        /// Smart Guest QR code).
        /// </summary>
        public int? QrCodeId { get; set; }

        public QrCode? QrCode { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
