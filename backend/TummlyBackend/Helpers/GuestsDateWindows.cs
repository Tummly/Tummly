namespace TummlyBackend.Helpers
{
    public static class GuestsDateWindows
    {
        public const int MaxInclusiveCalendarDays = 180;

        private static readonly HashSet<string> TablePresets =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "today",
                "last-7",
                "last-30",
                "this-month",
                "previous-month",
            };

        private static readonly HashSet<string> OverviewPresets =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "last-7",
                "last-30",
                "this-month",
            };

        public static bool IsValidTablePreset(string preset) =>
            TablePresets.Contains(preset);

        public static bool IsValidOverviewPreset(string preset) =>
            OverviewPresets.Contains(preset);

        /// <summary>
        /// Resolves preset windows in the operator's local calendar, then returns
        /// UTC bounds. <paramref name="utcOffsetMinutes"/> is minutes east of UTC
        /// (same sign as JavaScript <c>-Date#getTimezoneOffset()</c>).
        /// Preferred live path still expands presets client-side to
        /// dateFrom/dateTo (US 75); this path keeps API preset tokens correct.
        /// </summary>
        public static (DateTime FromUtc, DateTime ToUtc) ResolvePreset(
            string preset,
            DateTime utcNow,
            int utcOffsetMinutes = 0
        )
        {
            var normalized = TablePresets.Contains(preset)
                ? TablePresets.Single(value =>
                    value.Equals(preset, StringComparison.OrdinalIgnoreCase)
                )
                : OverviewPresets.Single(value =>
                    value.Equals(preset, StringComparison.OrdinalIgnoreCase)
                );

            var utcNowUtc = EnsureUtc(utcNow);
            var offset = TimeSpan.FromMinutes(utcOffsetMinutes);
            var localNow = utcNowUtc + offset;
            var startOfLocalToday = new DateTime(
                localNow.Year,
                localNow.Month,
                localNow.Day,
                0,
                0,
                0,
                DateTimeKind.Unspecified
            );
            var startOfLocalTodayUtc = DateTime.SpecifyKind(
                startOfLocalToday - offset,
                DateTimeKind.Utc
            );

            return normalized switch
            {
                "today" => (startOfLocalTodayUtc, startOfLocalTodayUtc.AddDays(1)),
                "last-7" => (startOfLocalTodayUtc.AddDays(-6), utcNowUtc),
                "last-30" => (startOfLocalTodayUtc.AddDays(-29), utcNowUtc),
                "this-month" => (
                    LocalMonthStartUtc(localNow.Year, localNow.Month, offset),
                    utcNowUtc
                ),
                "previous-month" => ResolvePreviousMonth(localNow, offset),
                _ => throw new ArgumentException("Invalid date preset."),
            };
        }

        public static (DateTime FromUtc, DateTime ToUtc) ResolveCustom(
            DateTime from,
            DateTime to,
            string fromFieldName = "dateFrom",
            string toFieldName = "dateTo"
        )
        {
            var fromUtc = EnsureUtc(from);
            var toUtc = EnsureUtc(to);

            if (fromUtc >= toUtc)
            {
                throw new ArgumentException(
                    $"{fromFieldName} must be before {toFieldName}."
                );
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                throw new ArgumentException(
                    "Date range cannot exceed 180 days."
                );
            }

            return (fromUtc, toUtc);
        }

        public static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }

        private static DateTime LocalMonthStartUtc(
            int localYear,
            int localMonth,
            TimeSpan offset
        )
        {
            var localStart = new DateTime(
                localYear,
                localMonth,
                1,
                0,
                0,
                0,
                DateTimeKind.Unspecified
            );
            return DateTime.SpecifyKind(localStart - offset, DateTimeKind.Utc);
        }

        private static (DateTime FromUtc, DateTime ToUtc) ResolvePreviousMonth(
            DateTime localNow,
            TimeSpan offset
        )
        {
            var firstOfThisMonthUtc = LocalMonthStartUtc(
                localNow.Year,
                localNow.Month,
                offset
            );
            var previousLocal = localNow.AddMonths(-1);
            var firstOfPreviousMonthUtc = LocalMonthStartUtc(
                previousLocal.Year,
                previousLocal.Month,
                offset
            );
            return (firstOfPreviousMonthUtc, firstOfThisMonthUtc);
        }
    }
}
