using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Location Guest note authored by an operator. Soft-delete hides the row in
    /// product lists; body edits overwrite in place with last-editor metadata.
    /// AuthorDisplayName is snapshotted at create; AuthorUserId SET NULL on user delete.
    /// </summary>
    public class LocationGuestNote
    {
        public int Id { get; set; }

        public int LocationGuestId { get; set; }

        public LocationGuest? LocationGuest { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Body { get; set; }
            = string.Empty;

        public int? AuthorUserId { get; set; }

        public User? AuthorUser { get; set; }

        [Required]
        [MaxLength(150)]
        public string AuthorDisplayName { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        /// <summary>Set on in-place body edit; null means never edited.</summary>
        public DateTime? UpdatedAt { get; set; }

        public int? LastEditedByUserId { get; set; }

        public User? LastEditedByUser { get; set; }

        [MaxLength(150)]
        public string? LastEditedByDisplayName { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedByUserId { get; set; }

        public User? DeletedByUser { get; set; }

        [MaxLength(150)]
        public string? DeletedByDisplayName { get; set; }
    }
}
