using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// A per-Owned-location instance of a QR type, with its own QR link
    /// (opaque <see cref="Token"/>). Five defaults (four placement types plus
    /// Smart Guest) are minted per location at Guest Loop provisioning.
    /// Operators may also create Digital guest link codes from Capture.
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

        /// <summary>
        /// Operator-facing display name for Digital guest links (trimmed;
        /// casing preserved). Null for catalog / Smart Guest types.
        /// </summary>
        [MaxLength(100)]
        public string? LinkName { get; set; }

        /// <summary>
        /// Case-insensitive uniqueness key for Digital guest links among
        /// non-archived rows at the location (trim + lower-case).
        /// </summary>
        [MaxLength(100)]
        public string? NormalizedLinkName { get; set; }

        /// <summary>
        /// Channel for Digital guest links; null for other QR types.
        /// </summary>
        public DigitalGuestLinkChannel? Channel { get; set; }

        /// <summary>
        /// Operator-only internal note on the QR code (trimmed; empty/whitespace
        /// stored as null). Used by Detail drawer Add note for all kinds.
        /// </summary>
        [MaxLength(500)]
        public string? InternalDescription { get; set; }

        /// <summary>
        /// When the code was archived; null while Active or Paused.
        /// Cleared on Restore.
        /// </summary>
        public DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// Operator who archived the code; null while Active or Paused.
        /// Cleared on Restore.
        /// </summary>
        public int? ArchivedByUserId { get; set; }

        /// <summary>
        /// Denormalized display name of the archiving operator (like note
        /// author names). Cleared on Restore.
        /// </summary>
        [MaxLength(150)]
        public string? ArchivedByDisplayName { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }

        [MaxLength(150)]
        public string? CreatedByDisplayName { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedByUserId { get; set; }

        [MaxLength(150)]
        public string? UpdatedByDisplayName { get; set; }
    }
}

