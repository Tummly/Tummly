namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only Smart Guest Link / QR open event. Recorded when
    /// GET /api/scan/{token} resolves successfully (physical QR, shared
    /// link, or operator Preview form). Powers Performance overview
    /// QR scans KPI counts.
    /// </summary>
    public class QrScanEvent
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
