using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        [Required]
        [MaxLength(64)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        [MaxLength(100)]
        public string? CtaLabel { get; set; }

        [MaxLength(500)]
        public string? CtaHref { get; set; }

        [MaxLength(64)]
        public string? Capability { get; set; }

        [MaxLength(128)]
        public string? DedupeKey { get; set; }
    }
}
