using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Shop.MaterialsCatalog
{
    public static class ShopMaterialCatalogMapper
    {
        public const string CatalogVersionId = "materials-catalog-db-v1";

        public const string IncludedWithPlanSlug = "included-with-your-plan";

        public const string EssentialSlug = "essential";

        public const string ComingSoonBadgeLabel = "Coming soon";

        public static string ResolveImageUrl(ShopMaterialSkuImage? image)
        {
            if (image == null || string.IsNullOrWhiteSpace(image.StorageKey))
            {
                return string.Empty;
            }

            return ShopCatalogImageRules.BuildPublicUrl(image.StorageKey);
        }

        public static IReadOnlyList<string> ResolveImageUrls(
            IEnumerable<ShopMaterialSkuImage> images
        )
        {
            return images
                .OrderBy(row => row.SortOrder)
                .ThenBy(row => row.Id)
                .Select(row => ResolveImageUrl(row))
                .Where(url => url.Length > 0)
                .ToList();
        }

        public static IReadOnlyList<string> ResolveTagLabels(
            ShopMaterialSku sku
        )
        {
            return sku.SkuTags
                .Select(row => row.Tag)
                .Where(tag => tag != null)
                .OrderBy(tag => tag!.Label)
                .Select(tag => tag!.Label)
                .ToList();
        }

        public static bool HasIncludedWithPlanTag(ShopMaterialSku sku) =>
            sku.SkuTags.Any(row =>
                string.Equals(
                    row.Tag?.Slug,
                    IncludedWithPlanSlug,
                    StringComparison.Ordinal
                )
            );

        public static string? ResolvePopularBadge(ShopMaterialSku sku)
        {
            if (!sku.AvailableInShop)
            {
                return ComingSoonBadgeLabel;
            }

            var essential = sku.SkuTags.FirstOrDefault(row =>
                string.Equals(
                    row.Tag?.Slug,
                    EssentialSlug,
                    StringComparison.Ordinal
                )
            );
            if (essential?.Tag != null)
            {
                return essential.Tag.Label;
            }

            return sku.SkuTags
                .Select(row => row.Tag)
                .FirstOrDefault(tag =>
                    tag != null
                    && !string.Equals(
                        tag.Slug,
                        IncludedWithPlanSlug,
                        StringComparison.Ordinal
                    )
                )
                ?.Label;
        }

        public static ShopCatalogListItemDto MapListItem(ShopMaterialSku sku)
        {
            var imageUrls = ResolveImageUrls(sku.Images);
            return new ShopCatalogListItemDto
            {
                SkuId = sku.SkuId,
                Title = sku.Title,
                Category = sku.Category,
                Description = sku.Description,
                UnitNetPence = sku.UnitNetPence,
                Packs = ShopMaterialPacks.Parse(sku.PacksJson),
                DeliveryNetPence = sku.DeliveryNetPence,
                Currency = sku.Currency,
                ImageUrl = imageUrls.FirstOrDefault() ?? string.Empty,
                ImageUrls = imageUrls,
                QrType = sku.QrType,
                IsPlanIncluded = HasIncludedWithPlanTag(sku) ? true : null,
                PopularBadge = ResolvePopularBadge(sku),
                AvailableInShop = sku.AvailableInShop,
                Tags = ResolveTagLabels(sku),
            };
        }

        public static ShopCatalogDetailDto MapDetail(ShopMaterialSku sku)
        {
            var imageUrls = ResolveImageUrls(sku.Images);
            return new ShopCatalogDetailDto
            {
                SkuId = sku.SkuId,
                Title = sku.Title,
                Category = sku.Category,
                Description = sku.Description,
                UnitNetPence = sku.UnitNetPence,
                Currency = sku.Currency,
                ImageUrl = imageUrls.FirstOrDefault() ?? string.Empty,
                ImageUrls = imageUrls,
                QrType = sku.QrType,
                IsPlanIncluded = HasIncludedWithPlanTag(sku) ? true : null,
                PopularBadge = ResolvePopularBadge(sku),
                AvailableInShop = sku.AvailableInShop,
                Tags = ResolveTagLabels(sku),
                Material = sku.Material,
                Dimensions = sku.Dimensions,
                MinOrderQty = sku.MinOrderQty,
                Packs = ShopMaterialPacks.Parse(sku.PacksJson),
                DeliveryNetPence = sku.DeliveryNetPence,
                CatalogVersion = CatalogVersionId,
                MintOnShopFulfilment = sku.MintOnShopFulfilment,
                AboutBody = sku.AboutBody ?? string.Empty,
                Specifications = ShopMaterialSpecifications.Parse(
                    sku.SpecificationsJson
                ),
            };
        }

        public static MaterialsCatalogSku ToSnapshotSku(ShopMaterialSku sku)
        {
            var imageUrls = ResolveImageUrls(sku.Images);
            return new MaterialsCatalogSku
            {
                SkuId = sku.SkuId,
                Title = sku.Title,
                Category = sku.Category,
                Description = sku.Description,
                Material = sku.Material,
                Dimensions = sku.Dimensions,
                QrType = sku.QrType,
                PrintRoute =
                    sku.PlacementRecipe?.PrintRoute
                    ?? PlacementPrintRoutes.EmbeddedPdf,
                PlacementRecipeId =
                    sku.PlacementRecipeId > 0 ? sku.PlacementRecipeId : null,
                UnitNetPence = sku.UnitNetPence,
                MinOrderQty = sku.MinOrderQty,
                Currency = sku.Currency,
                ImageUrl = imageUrls.FirstOrDefault() ?? string.Empty,
                IsPlanIncluded = HasIncludedWithPlanTag(sku),
                PopularBadge = ResolvePopularBadge(sku),
                AvailableInShop = sku.AvailableInShop,
                MintOnShopFulfilment = sku.MintOnShopFulfilment,
            };
        }
    }
}
