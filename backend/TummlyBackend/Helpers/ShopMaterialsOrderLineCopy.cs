using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Friendly Revolut Hosted Checkout labels for Tummly Shop materials orders.
    /// </summary>
    public static class ShopMaterialsOrderLineCopy
    {
        public static string FormatOrderDescription(ShopOrder order)
        {
            var titles = order
                .Lines.OrderBy(line => line.CatalogSkuId, StringComparer.Ordinal)
                .Select(line => line.TitleSnapshot?.Trim() ?? string.Empty)
                .Where(title => title.Length > 0)
                .ToArray();

            var products =
                titles.Length == 0 ? "materials" : string.Join(", ", titles);
            return $"Tummly Shop · {products}";
        }

        public static string FormatLineName(string? titleSnapshot)
        {
            var title = titleSnapshot?.Trim() ?? string.Empty;
            return title.Length == 0
                ? "Tummly Shop product"
                : $"Tummly Shop · {title}";
        }

        public static string ExpressDeliveryLineName { get; } =
            "Tummly Shop · Express delivery";
    }
}
