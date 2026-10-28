using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Wider-media Shop concept that uses Register interest (not cart/checkout).
    /// Admin can promote a product into a buyable Shop SKU and demote it back.
    /// </summary>
    public sealed class ShopInterestProduct
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string ProductKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Material { get; set; } = string.Empty;

        [MaxLength(120)]
        public string Dimensions { get; set; } = string.Empty;

        /// <summary>
        /// When set, this interest product is promoted to the Shop SKU with this id.
        /// </summary>
        [MaxLength(80)]
        public string? LinkedSkuId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ShopInterestRegistration> Registrations { get; set; } =
            new List<ShopInterestRegistration>();

        public ICollection<ShopInterestProductImage> Images { get; set; } =
            new List<ShopInterestProductImage>();
    }

    public sealed class ShopInterestProductImage
    {
        public int Id { get; set; }

        public int ShopInterestProductId { get; set; }

        public ShopInterestProduct Product { get; set; } = null!;

        /// <summary>
        /// Blob object key (same storage as Shop catalog images).
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string StorageKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string ContentType { get; set; } = "image/png";

        public int SortOrder { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Operator click to register interest. No purchase and no marketing send.
    /// </summary>
    public sealed class ShopInterestRegistration
    {
        public int Id { get; set; }

        public int ShopInterestProductId { get; set; }

        public ShopInterestProduct Product { get; set; } = null!;

        [Required]
        [MaxLength(80)]
        public string ProductKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ProductTitleSnapshot { get; set; } = string.Empty;

        public int RestaurantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string RestaurantNameSnapshot { get; set; } = string.Empty;

        public int? LocationId { get; set; }

        [MaxLength(200)]
        public string? LocationNameSnapshot { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string UserFullNameSnapshot { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string UserEmailSnapshot { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Set when the operator withdraws interest. The row stays as history;
        /// only rows with no withdrawal count as active.
        /// </summary>
        public DateTime? WithdrawnAtUtc { get; set; }

        /// <summary>
        /// The user who withdrew. Null for rows withdrawn by the one-time dedupe migration.
        /// </summary>
        public int? WithdrawnByUserId { get; set; }
    }
}
