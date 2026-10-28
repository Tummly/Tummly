using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Shop
{
    /// <summary>
    /// A Shop SKU has a QR path when fulfilment can mint its QR code: the SKU
    /// mints on Shop fulfilment, or its recipe is in the complimentary starter.
    /// Two guards enforce it. Making a SKU available (SetAvailableInShop and
    /// interest promote) is refused before any write when there is no path.
    /// A catalog update is refused when it would remove the last path from a
    /// SKU that is already available. An available SKU that already lacks a
    /// path can still be edited.
    /// </summary>
    public static class ShopQrPathRules
    {
        public const string SkuHasNoQrPathCode = "sku_has_no_qr_path";

        public const string SkuHasNoQrPathMessage =
            "This product has no QR code path. Turn on Mint on shop fulfilment for this product before making it available in the Shop.";

        public static bool HasQrPath(
            bool mintOnShopFulfilment,
            bool recipeIncludesComplimentaryStarter
        )
        {
            return mintOnShopFulfilment || recipeIncludesComplimentaryStarter;
        }

        public static async Task<bool> HasQrPathAsync(
            ApplicationDbContext db,
            ShopMaterialSku sku,
            CancellationToken cancellationToken
        )
        {
            if (sku.MintOnShopFulfilment)
            {
                return true;
            }

            return await db.PlacementRecipes
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == sku.PlacementRecipeId
                        && row.IncludeInComplimentaryStarter,
                    cancellationToken
                );
        }
    }
}
