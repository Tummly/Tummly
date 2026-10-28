using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class ShopProductArtwork : Migration
    {
        /// <summary>
        /// One-time switch to the product artwork (`ShopProductArtworkPaths.PathFor`).
        /// Live core SKUs swap the seeded placeholder image for their artwork; Admin
        /// uploads stay. Seeded interest products with no image get their artwork.
        /// New rows get the artwork from the startup seeds.
        /// </summary>
        public const string ArtworkSql = @"
UPDATE i
SET i.StorageKey = CONCAT('legacy:/assets/images/shop/products/', s.SkuId, '.webp'),
    i.ContentType = 'image/webp'
FROM ShopMaterialSkuImages i
INNER JOIN ShopMaterialSkus s ON s.Id = i.SkuId
WHERE s.Status <> 'Deleted'
  AND s.SkuId IN ('qr-counter-display', 'table-tents', 'table-talker', 'window-stickers', 'qr-guest-cards')
  AND i.StorageKey IN ('legacy:/assets/images/shop/tummly-sticker.png', 'legacy:/assets/images/shop/tummly-bag.png');

INSERT INTO ShopInterestProductImages (ShopInterestProductId, StorageKey, ContentType, SortOrder, CreatedAtUtc)
SELECT p.Id, CONCAT('legacy:/assets/images/shop/products/', p.ProductKey, '.webp'), 'image/webp', 0, SYSUTCDATETIME()
FROM ShopInterestProducts p
WHERE p.ProductKey IN ('receipt-label', 'delivery-bag-seal', 'standard-package-label', 'drink-cup-seal', 'pizza-box-label', 'poster-customer-club', 'tray-liner')
  AND NOT EXISTS (SELECT 1 FROM ShopInterestProductImages x WHERE x.ShopInterestProductId = p.Id);";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ArtworkSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only change; the placeholder images are not restored.
        }
    }
}
