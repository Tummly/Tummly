using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Restaurant-scoped Guest tag catalog entry.
    /// </summary>
    public class GuestTag
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant? Restaurant { get; set; }

        /// <summary>Operator-facing label (trimmed; whitespace collapsed).</summary>
        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; }
            = string.Empty;

        /// <summary>
        /// Unique per restaurant: trim, collapse whitespace, lower-case.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string NormalizedName { get; set; }
            = string.Empty;

        /// <summary>
        /// PascalCase <see cref="DetectedTag"/> name when AI-ensured; null for
        /// operator-created entries. Unique per restaurant when set.
        /// </summary>
        [MaxLength(64)]
        public string? DetectedTagKey { get; set; }

        /// <summary>
        /// True only when AI first introduced this catalog entry.
        /// </summary>
        public bool AiSourced { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public ICollection<LocationGuestTag> Memberships { get; set; }
            = new List<LocationGuestTag>();
    }
}
