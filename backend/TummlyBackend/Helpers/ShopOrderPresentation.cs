namespace TummlyBackend.Helpers
{
    internal static class ShopOrderPresentation
    {
        internal static string FormatGbp(int pence)
        {
            return $"£{(pence / 100m):0.00}";
        }

        internal static string FormatDisplayDate(DateTime utc)
        {
            return utc.ToString("dd MMM yyyy");
        }

        internal static string BuildMaterialsSummary(IEnumerable<Models.ShopOrderLine> lines)
        {
            var parts = lines
                .OrderBy(line => line.CatalogSkuId)
                .Select(line =>
                    line.Quantity > 1
                        ? $"{line.TitleSnapshot} · Qty {line.Quantity}"
                        : line.TitleSnapshot
                )
                .ToList();

            if (parts.Count == 0)
            {
                return "Materials";
            }

            return string.Join(" · ", parts.Take(2))
                + (parts.Count > 2 ? $" · +{parts.Count - 2} more" : string.Empty);
        }
    }
}
