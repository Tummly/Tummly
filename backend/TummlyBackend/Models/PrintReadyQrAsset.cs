using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Metadata for one Admin-downloadable Print-ready QR asset PDF
    /// (Starter QR materials or later Shop print asset). Bytes live in object
    /// storage under <see cref="StorageKey"/>.
    /// </summary>
    public class PrintReadyQrAsset
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public QrType QrType { get; set; }

        public PrintReadyQrAssetStatus Status { get; set; }
            = PrintReadyQrAssetStatus.Preparing;

        [MaxLength(512)]
        public string? StorageKey { get; set; }

        [MaxLength(128)]
        public string ContentType { get; set; } = "application/pdf";

        [MaxLength(260)]
        public string? FileName { get; set; }

        /// <summary>SHA-256 hex of the Active QR token used for generation.</summary>
        [MaxLength(64)]
        public string? QrTokenFingerprint { get; set; }

        [MaxLength(64)]
        public string? TemplatePackVersion { get; set; }

        [MaxLength(64)]
        public string? OfferCopyVersion { get; set; }

        /// <summary>Null for Starter QR materials; set for Shop print assets (ticket 03).</summary>
        public Guid? ShopOrderId { get; set; }

        [MaxLength(1000)]
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
