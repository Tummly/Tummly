using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Server cart for one operator User at one Owned location (ticket 14).
    /// Scope key: (RestaurantId, LocationId, UserId).
    /// </summary>
    public class ShopCart
    {
        public Guid Id { get; set; }

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        public int LocationId { get; set; }

        public RestaurantLocation Location { get; set; } = null!;

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ShopCartLine> Lines { get; set; } =
            new List<ShopCartLine>();
    }

    public class ShopCartLine
    {
        public int Id { get; set; }

        public Guid ShopCartId { get; set; }

        public ShopCart ShopCart { get; set; } = null!;

        [Required]
        [MaxLength(80)]
        public string SkuId { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
