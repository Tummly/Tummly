using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Wider-media Shop concept that uses Register interest (not cart/checkout).
    /// Launch seeds zero rows; Design adds products later.
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

        [MaxLength(512)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ShopInterestRegistration> Registrations { get; set; } =
            new List<ShopInterestRegistration>();
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
    }
}
