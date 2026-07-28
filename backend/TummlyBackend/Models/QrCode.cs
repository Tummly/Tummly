using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// A per-Owned-location instance of a QR type, with its own QR link
    /// (opaque <see cref="Token"/>). Five defaults (four placement types plus
    /// Smart Guest) are minted per location at Guest Loop provisioning.
    /// Replaces the single <c>RestaurantLocation.LinkToken</c> model.
    /// </summary>
    public class QrCode
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        public QrType QrType { get; set; }

        [Required]
        [MaxLength(32)]
        public string Token { get; set; }
            = string.Empty;

        public QrCodeStatus Status { get; set; }
            = QrCodeStatus.Active;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
