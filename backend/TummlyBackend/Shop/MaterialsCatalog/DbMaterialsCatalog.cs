using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Shop.MaterialsCatalog
{
    public sealed class DbMaterialsCatalog : IMaterialsCatalog
    {
        private readonly ApplicationDbContext _context;

        public DbMaterialsCatalog(ApplicationDbContext context)
        {
            _context = context;
        }

        public string CurrentCatalogId =>
            ShopMaterialCatalogMapper.CatalogVersionId;

        public MaterialsCatalogSnapshot GetRequired(string catalogId)
        {
            if (
                !string.Equals(
                    catalogId,
                    CurrentCatalogId,
                    StringComparison.Ordinal
                )
            )
            {
                throw new InvalidOperationException(
                    $"Materials catalog '{catalogId}' is not available."
                );
            }

            var skus = _context.ShopMaterialSkus
                .AsNoTracking()
                .Include(row => row.Images)
                .Include(row => row.SkuTags)
                .ThenInclude(row => row.Tag)
                // Include Draft/Deleted so past orders can still resolve QrType
                // for SKUs that were later unpublished or soft-deleted.
                .OrderBy(row => row.Id)
                .ToList();

            return new MaterialsCatalogSnapshot
            {
                Id = CurrentCatalogId,
                Skus = skus
                    .Select(ShopMaterialCatalogMapper.ToSnapshotSku)
                    .ToList(),
            };
        }

        public IReadOnlyList<ShopCatalogListItemDto> BuildList()
        {
            var skus = LoadPublishedSkus();
            return skus
                .Select(ShopMaterialCatalogMapper.MapListItem)
                .ToList();
        }

        public ShopCatalogDetailDto? TryBuildDetail(string skuId)
        {
            var sku = LoadPublishedSkus()
                .FirstOrDefault(row =>
                    string.Equals(
                        row.SkuId,
                        skuId,
                        StringComparison.Ordinal
                    )
                );
            if (sku == null)
            {
                return null;
            }

            return ShopMaterialCatalogMapper.MapDetail(sku);
        }

        /// <summary>
        /// Admin / print helpers: any non-deleted SKU by id.
        /// </summary>
        public ShopMaterialSku? TryGetSkuEntity(
            string skuId,
            bool includeDeleted = false
        )
        {
            var query = _context.ShopMaterialSkus
                .Include(row => row.Images)
                .Include(row => row.SkuTags)
                .ThenInclude(row => row.Tag)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(row =>
                    row.Status != ShopMaterialSkuStatuses.Deleted
                );
            }

            return query.FirstOrDefault(row =>
                row.SkuId == skuId
            );
        }

        private List<ShopMaterialSku> LoadPublishedSkus()
        {
            return _context.ShopMaterialSkus
                .AsNoTracking()
                .Include(row => row.Images)
                .Include(row => row.SkuTags)
                .ThenInclude(row => row.Tag)
                .Where(row => row.Status == ShopMaterialSkuStatuses.Published)
                .OrderBy(row => row.Id)
                .ToList();
        }
    }
}
