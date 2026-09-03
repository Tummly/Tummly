using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Operator-entered location facts for Shop recommendations (ticket 19).
    /// One row per Owned location; 1:1 with <see cref="RestaurantLocation"/>.
    /// </summary>
    public class ShopLocationDetails
    {
        public int LocationId { get; set; }

        public RestaurantLocation Location { get; set; } = null!;

        public int TableCount { get; set; }

        public int CounterCount { get; set; }

        public int EntranceCount { get; set; }

        public int SecondaryEntranceCount { get; set; }

        [Required]
        [MaxLength(32)]
        public string TakeawayVolume { get; set; } = "not-sure";

        /// <summary>Comma-joined prompt location ids (tables, counters, …).</summary>
        [Required]
        [MaxLength(256)]
        public string PromptLocations { get; set; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string ExistingMaterials { get; set; } = "no";

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
