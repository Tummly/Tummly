namespace TummlyBackend.Shop.MaterialsCatalog
{
    /// <summary>
    /// Bundled product artwork. The frontend maps each path to a Vite asset
    /// (`src/lib/operatorShop/shopProductArtwork.ts`); keep both lists in sync.
    /// </summary>
    public static class ShopProductArtworkPaths
    {
        public const string ContentType = "image/webp";

        /// <summary>Public path for a core SkuId or an interest ProductKey.</summary>
        public static string PathFor(string key) =>
            $"/assets/images/shop/products/{key}.webp";
    }
}
