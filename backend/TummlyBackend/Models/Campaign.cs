using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Campaign Draft row — status is always draft in slice 1 (ticket 29).
    /// </summary>
    public class Campaign
    {
        public int Id { get; set; }

        public int RestaurantLocationId { get; set; }

        public RestaurantLocation? RestaurantLocation { get; set; }

        /// <summary>Always "draft" in slice 1.</summary>
        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = "draft";

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? GoalId { get; set; }

        [MaxLength(64)]
        public string? TemplateId { get; set; }

        public int? TemplateVersion { get; set; }

        [MaxLength(64)]
        public string? AudienceKey { get; set; }

        [MaxLength(32)]
        public string? Channel { get; set; }

        [MaxLength(64)]
        public string? OfferStance { get; set; }

        [MaxLength(500)]
        public string? MessageSubject { get; set; }

        [MaxLength(8000)]
        public string? MessageBody { get; set; }

        /// <summary>Optimistic concurrency token — incremented on each PATCH.</summary>
        public int RowVersion { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
