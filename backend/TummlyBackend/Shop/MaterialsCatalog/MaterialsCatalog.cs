using System.Text.Json;
using Microsoft.Extensions.Hosting;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Shop.MaterialsCatalog
{
    public sealed class MaterialsCatalog : IMaterialsCatalog
    {
        public const string CurrentIdFileName = "current-materials-catalog-id";
        public const string PackJsonFileName = "tummly_uk_materials_catalog_v1.json";
        public const string PackRelativeDirectory = "docs/product/materials-catalog-v1";
        public const string AssetsRelativeDirectory = "Assets/materials-catalog";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IReadOnlyDictionary<string, MaterialsCatalogSnapshot> _byId;

        private MaterialsCatalog(
            string currentCatalogId,
            IReadOnlyDictionary<string, MaterialsCatalogSnapshot> byId
        )
        {
            CurrentCatalogId = currentCatalogId;
            _byId = byId;
        }

        public string CurrentCatalogId { get; }

        public static MaterialsCatalog LoadFromDirectory(string packDirectory)
        {
            if (!Directory.Exists(packDirectory))
            {
                throw new InvalidOperationException(
                    $"Materials catalog pack directory is missing: {packDirectory}"
                );
            }

            var currentIdPath = Path.Combine(packDirectory, CurrentIdFileName);
            if (!File.Exists(currentIdPath))
            {
                throw new InvalidOperationException(
                    $"Materials catalog current id file is missing: {currentIdPath}"
                );
            }

            var currentId = File.ReadAllText(currentIdPath).Trim();
            if (string.IsNullOrWhiteSpace(currentId))
            {
                throw new InvalidOperationException(
                    "Materials catalog current id file is empty."
                );
            }

            var jsonPath = Path.Combine(packDirectory, PackJsonFileName);
            if (!File.Exists(jsonPath))
            {
                throw new InvalidOperationException(
                    $"Materials catalog JSON is missing: {jsonPath}"
                );
            }

            using var stream = File.OpenRead(jsonPath);
            using var document = JsonDocument.Parse(stream);
            var snapshot = BindSnapshot(document.RootElement);
            if (
                !string.Equals(snapshot.Id, currentId, StringComparison.Ordinal)
            )
            {
                throw new InvalidOperationException(
                    $"Materials catalog current id '{currentId}' does not match pack id '{snapshot.Id}'."
                );
            }

            return new MaterialsCatalog(
                currentId,
                new Dictionary<string, MaterialsCatalogSnapshot>(StringComparer.Ordinal)
                {
                    [snapshot.Id] = snapshot,
                }
            );
        }

        public static MaterialsCatalog LoadFromContentRoot(string contentRootPath)
        {
            return LoadFromDirectory(ResolvePackDirectory(contentRootPath));
        }

        public static string ResolvePackDirectory(string contentRootPath)
        {
            var candidates = new[]
            {
                Path.Combine(contentRootPath, AssetsRelativeDirectory),
                Path.GetFullPath(
                    Path.Combine(contentRootPath, "..", "..", PackRelativeDirectory)
                ),
                Path.GetFullPath(
                    Path.Combine(contentRootPath, "..", PackRelativeDirectory)
                ),
            };

            foreach (var candidate in candidates)
            {
                if (
                    Directory.Exists(candidate)
                    && File.Exists(Path.Combine(candidate, CurrentIdFileName))
                    && File.Exists(Path.Combine(candidate, PackJsonFileName))
                )
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "Materials catalog pack directory could not be resolved from content root."
            );
        }

        public MaterialsCatalogSnapshot GetRequired(string catalogId)
        {
            if (_byId.TryGetValue(catalogId, out var snapshot))
            {
                return snapshot;
            }

            throw new InvalidOperationException(
                $"Materials catalog '{catalogId}' is not available."
            );
        }

        public IReadOnlyList<ShopCatalogListItemDto> BuildList()
        {
            var catalog = GetRequired(CurrentCatalogId);
            return catalog.Skus.Select(MapListItem).ToList();
        }

        public ShopCatalogDetailDto? TryBuildDetail(string skuId)
        {
            var catalog = GetRequired(CurrentCatalogId);
            var sku = catalog.Skus.FirstOrDefault(
                row => string.Equals(row.SkuId, skuId, StringComparison.Ordinal)
            );
            if (sku == null)
            {
                return null;
            }

            return MapDetail(catalog.Id, sku);
        }

        public static MaterialsCatalog CreateForHost(IHostEnvironment environment)
        {
            return LoadFromContentRoot(environment.ContentRootPath);
        }

        private static MaterialsCatalogSnapshot BindSnapshot(JsonElement root)
        {
            var catalog = root.GetProperty("catalog");
            var id = catalog.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("catalog.id is missing.");

            var skus = new List<MaterialsCatalogSku>();
            foreach (var skuElement in root.GetProperty("skus").EnumerateArray())
            {
                skus.Add(BindSku(skuElement));
            }

            return new MaterialsCatalogSnapshot
            {
                Id = id,
                Skus = skus,
            };
        }

        private static MaterialsCatalogSku BindSku(JsonElement element)
        {
            return new MaterialsCatalogSku
            {
                SkuId = element.GetProperty("skuId").GetString()
                    ?? throw new InvalidOperationException("skuId is missing."),
                Title = element.GetProperty("title").GetString()
                    ?? throw new InvalidOperationException("title is missing."),
                Category = element.GetProperty("category").GetString()
                    ?? throw new InvalidOperationException("category is missing."),
                Description = element.GetProperty("description").GetString()
                    ?? throw new InvalidOperationException("description is missing."),
                Material = element.GetProperty("material").GetString()
                    ?? throw new InvalidOperationException("material is missing."),
                Dimensions = element.GetProperty("dimensions").GetString()
                    ?? throw new InvalidOperationException("dimensions is missing."),
                QrType = element.GetProperty("qrType").GetString()
                    ?? throw new InvalidOperationException("qrType is missing."),
                UnitNetPence = element.GetProperty("unitNetPence").GetInt32(),
                MinOrderQty = element.GetProperty("minOrderQty").GetInt32(),
                Currency = element.GetProperty("currency").GetString()
                    ?? throw new InvalidOperationException("currency is missing."),
                ImageUrl = element.GetProperty("imageUrl").GetString()
                    ?? throw new InvalidOperationException("imageUrl is missing."),
                IsPlanIncluded = element.GetProperty("isPlanIncluded").GetBoolean(),
                PopularBadge = element.TryGetProperty("popularBadge", out var badge)
                    && badge.ValueKind != JsonValueKind.Null
                    ? badge.GetString()
                    : null,
                MintOnShopFulfilment = element
                    .GetProperty("mintOnShopFulfilment")
                    .GetBoolean(),
            };
        }

        private static ShopCatalogListItemDto MapListItem(MaterialsCatalogSku sku)
        {
            return new ShopCatalogListItemDto
            {
                SkuId = sku.SkuId,
                Title = sku.Title,
                Category = sku.Category,
                Description = sku.Description,
                UnitNetPence = sku.UnitNetPence,
                Currency = sku.Currency,
                ImageUrl = sku.ImageUrl,
                QrType = sku.QrType,
                IsPlanIncluded = sku.IsPlanIncluded ? true : null,
                PopularBadge = sku.PopularBadge,
            };
        }

        private static ShopCatalogDetailDto MapDetail(
            string catalogId,
            MaterialsCatalogSku sku
        )
        {
            return new ShopCatalogDetailDto
            {
                SkuId = sku.SkuId,
                Title = sku.Title,
                Category = sku.Category,
                Description = sku.Description,
                UnitNetPence = sku.UnitNetPence,
                Currency = sku.Currency,
                ImageUrl = sku.ImageUrl,
                QrType = sku.QrType,
                IsPlanIncluded = sku.IsPlanIncluded ? true : null,
                PopularBadge = sku.PopularBadge,
                Material = sku.Material,
                Dimensions = sku.Dimensions,
                MinOrderQty = sku.MinOrderQty,
                CatalogVersion = catalogId,
                MintOnShopFulfilment = sku.MintOnShopFulfilment,
            };
        }
    }
}
