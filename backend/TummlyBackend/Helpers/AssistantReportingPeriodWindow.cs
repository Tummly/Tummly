using System.Globalization;
using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Helpers
{
    public static class AssistantReportingPeriodWindow
    {
        public static (DateTime FromUtc, DateTime ToUtc) Resolve(
            AssistantReportingPeriodDto period,
            DateTime utcNow
        )
        {
            var now = utcNow.Kind == DateTimeKind.Utc
                ? utcNow
                : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

            if (string.Equals(period.Kind, "custom", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseDate(period.StartDate, out var start)
                    || !TryParseDate(period.EndDate, out var end))
                {
                    throw new ArgumentException("Custom Reporting period dates are invalid.");
                }

                return (start, end.AddDays(1));
            }

            return period.PresetId switch
            {
                "last30" => (StartOfUtcDay(now).AddDays(-29), now),
                "thisMonth" => (
                    new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    now
                ),
                _ => (StartOfUtcDay(now).AddDays(-6), now),
            };
        }

        private static DateTime StartOfUtcDay(DateTime utcNow)
            => new(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);

        private static bool TryParseDate(string? value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out date
            );
        }
    }
}
