using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Shop
{
    /// <summary>
    /// QR types a Shop order can take a placement slot for. Only SKUs that mint
    /// at fulfilment (<c>MintOnShopFulfilment</c>) take part. Starter SKUs such
    /// as Counter Display use the QR code the starter mint made and never
    /// reserve a slot (spec 4.3).
    /// </summary>
    public static class ShopQrSlotTypes
    {
        public static IReadOnlyList<QrType> FromSkuIds(
            IEnumerable<string> skuIds,
            IMaterialsCatalog catalog
        )
        {
            var lines = skuIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(id => catalog.TryBuildDetail(id)?.MintOnShopFulfilment == true)
                .Select(id => new ShopOrderLine { CatalogSkuId = id, Quantity = 1 })
                .ToList();
            if (lines.Count == 0)
            {
                return [];
            }

            return PrintReadyQrMaterialsService
                .ResolveOrderedQrTypes(lines, catalog)
                .Keys.OrderBy(type => type)
                .ToList();
        }

        public static IReadOnlyList<QrType> FromOrderLines(
            IEnumerable<ShopOrderLine> lines,
            IMaterialsCatalog catalog
        ) => FromSkuIds(lines.Select(line => line.CatalogSkuId), catalog);
    }
}
