using System.Globalization;

namespace TummlyBackend.Helpers
{
    internal static class ShopOrderPresentation
    {
        private static readonly TimeZoneInfo LondonTimeZone = ResolveLondonTimeZone();

        internal static string FormatGbp(int pence)
        {
            return $"£{(pence / 100m):0.00}";
        }

        /// <summary>
        /// Order date as the buyer sees it: the stored UTC instant in London
        /// time, so an order placed at 23:30 UTC in summer reads as the next day.
        /// </summary>
        internal static string FormatDisplayDate(DateTime utc)
        {
            var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo
                .ConvertTimeFromUtc(asUtc, LondonTimeZone)
                .ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        }

        private static TimeZoneInfo ResolveLondonTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            }
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
