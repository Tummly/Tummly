using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    public static class ShopMaterialSkuStatuses
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string Deleted = "Deleted";
    }

    /// <summary>
    /// Print fulfilment engines. Engineering seeds recipes that point at these.
    /// </summary>
    public static class PlacementPrintRoutes
    {
        public const string EmbeddedPdf = "EmbeddedPdf";

        public const string PatchCsv60 = "PatchCsv60";

        public const string GuestCardCsv = "GuestCardCsv";
    }

    /// <summary>
    /// Seeded placement + print binding. Admin attaches commercial SKUs to unused recipes.
    /// </summary>
    public sealed class PlacementRecipe
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Platform placement key (matches <c>QrType</c> enum name), e.g. TableTent.
        /// </summary>
        [Required]
        [MaxLength(40)]
        public string PlacementKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string PrintRoute { get; set; } = PlacementPrintRoutes.EmbeddedPdf;

        public bool MintOnFulfilmentDefault { get; set; }

        /// <summary>
        /// When true, the published SKU on this recipe is included in complimentary starter orders.
        /// </summary>
        public bool IncludeInComplimentaryStarter { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ShopMaterialSku> Skus { get; set; } =
            new List<ShopMaterialSku>();
    }

    public sealed class ShopMaterialTag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string Label { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ShopMaterialSkuTag> SkuTags { get; set; } =
            new List<ShopMaterialSkuTag>();
    }

    public sealed class ShopMaterialSku
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string SkuId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Material { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Dimensions { get; set; } = string.Empty;

        /// <summary>
        /// Denormalized copy of <see cref="PlacementRecipe.PlacementKey"/> for fulfilment queries.
        /// </summary>
        [Required]
        [MaxLength(40)]
        public string QrType { get; set; } = string.Empty;

        public int PlacementRecipeId { get; set; }

        public PlacementRecipe PlacementRecipe { get; set; } = null!;

        /// <summary>
        /// Denormalized catalog "From" price = lowest pack <c>netPence</c>.
        /// </summary>
        public int UnitNetPence { get; set; }

        /// <summary>
        /// Back-compat minimum; kept as <c>min(pack.qty)</c>. Cart validates pack tiers only.
        /// </summary>
        public int MinOrderQty { get; set; } = 1;

        /// <summary>
        /// JSON array of { "qty", "netPence" } pack tiers (pack shop price, not per-unit).
        /// </summary>
        public string PacksJson { get; set; } = "[]";

        /// <summary>
        /// Standard delivery net for this SKU (pence).
        /// </summary>
        public int DeliveryNetPence { get; set; }

        /// <summary>
        /// Admin-only supplier label. Never exposed on operator catalog DTOs.
        /// </summary>
        [MaxLength(200)]
        public string? SupplierName { get; set; }

        /// <summary>
        /// Admin-only internal cost (pence). Never exposed on operator catalog DTOs.
        /// </summary>
        public int? InternalCostNetPence { get; set; }

        [Required]
        [MaxLength(8)]
        public string Currency { get; set; } = "GBP";

        public bool MintOnShopFulfilment { get; set; }

        [Required]
        [MaxLength(16)]
        public string Status { get; set; } = ShopMaterialSkuStatuses.Draft;

        /// <summary>
        /// Product-page "About" body. Empty until Admin sets it.
        /// </summary>
        public string AboutBody { get; set; } = string.Empty;

        /// <summary>
        /// JSON array of { "label", "value" } rows for Product specifications.
        /// </summary>
        public string SpecificationsJson { get; set; } = "[]";

        public DateTime? PublishedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ShopMaterialSkuTag> SkuTags { get; set; } =
            new List<ShopMaterialSkuTag>();

        public ICollection<ShopMaterialSkuImage> Images { get; set; } =
            new List<ShopMaterialSkuImage>();
    }

    public sealed class ShopMaterialSkuTag
    {
        public int SkuId { get; set; }

        public ShopMaterialSku Sku { get; set; } = null!;

        public int TagId { get; set; }

        public ShopMaterialTag Tag { get; set; } = null!;
    }

    public sealed class ShopMaterialSkuImage
    {
        public int Id { get; set; }

        public int SkuId { get; set; }

        public ShopMaterialSku Sku { get; set; } = null!;

        /// <summary>
        /// Blob object key, or <c>legacy:</c> + public path for seeded assets.
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
}
