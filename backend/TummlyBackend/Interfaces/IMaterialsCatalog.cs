using TummlyBackend.DTOs.Shop;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Interfaces
{
    public interface IMaterialsCatalog
    {
        string CurrentCatalogId { get; }

        MaterialsCatalogSnapshot GetRequired(string catalogId);

        IReadOnlyList<ShopCatalogListItemDto> BuildList();

        ShopCatalogDetailDto? TryBuildDetail(string skuId);
    }
}
