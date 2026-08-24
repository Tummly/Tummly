namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Generate window for Recommended next step from Default reporting period
    /// in the location timezone (Europe/London until a location field exists).
    /// </summary>
    public static class DefaultReportingPeriodWindow
    {
        public static (DateTime FromUtc, DateTime ToUtc) Resolve(
            string? reportingPeriod,
            DateTime utcNow,
            string ianaTimeZoneId = WeeklyBriefWeekKey.DefaultLocationTimeZoneId
        )
        {
            var period = WorkspaceDefaultsOptions.NormalizeReportingPeriod(
                reportingPeriod
            );
            var utc = EnsureUtc(utcNow);
            var timeZone = ResolveTimeZone(ianaTimeZoneId);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var localDate = DateOnly.FromDateTime(localNow);

            DateOnly fromLocal = period switch
            {
                "30days" => localDate.AddDays(-29),
                "thisMonth" => new DateOnly(localDate.Year, localDate.Month, 1),
                _ => localDate.AddDays(-6),
            };

            var fromUtc = LocalDateStartToUtc(fromLocal, timeZone);
            return (fromUtc, utc);
        }

        private static DateTime LocalDateStartToUtc(
            DateOnly localDate,
            TimeZoneInfo timeZone
        )
        {
            var localStart = localDate.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Unspecified
            );
            return TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        }

        private static DateTime EnsureUtc(DateTime utcNow) =>
            utcNow.Kind switch
            {
                DateTimeKind.Utc => utcNow,
                DateTimeKind.Local => utcNow.ToUniversalTime(),
                _ => throw new ArgumentException(
                    "utcNow must be DateTimeKind.Utc (or Local, which is converted).",
                    nameof(utcNow)
                ),
            };

        private static TimeZoneInfo ResolveTimeZone(string ianaTimeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
            }
            catch (TimeZoneNotFoundException) when (
                string.Equals(
                    ianaTimeZoneId,
                    "Europe/London",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            }
        }
    }
}
