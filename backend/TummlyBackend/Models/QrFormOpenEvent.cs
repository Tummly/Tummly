namespace TummlyBackend.Models
{
    /// <summary>
    /// Append-only guest form open event. Recorded when GET /api/scan/{token}
    /// resolves to a Live guest form. Powers Capture / Reports form-open KPIs.
    /// </summary>
    public class QrFormOpenEvent
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public int? QrCodeId { get; set; }

        public QrCode? QrCode { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
