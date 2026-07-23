using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Create-only Location Guest note authored by an operator.
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
    }
}
