using System.Globalization;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Closed prior Mon–Sun week for a Weekly brief row, in the location timezone.
    /// </summary>
    /// <param name="WeekKey">
    /// ISO week key <c>yyyy-Www</c> (ISO week-year, Monday-based).
    /// </param>
    /// <param name="CoverageStartUtc">
    /// Inclusive UTC instant of Monday 00:00 in the location timezone.
    /// </param>
    /// <param name="CoverageEndUtcExclusive">
    /// Exclusive UTC instant of the following Monday 00:00 in the location timezone
    /// (end of Sunday local day).
    /// </param>
    public readonly record struct WeeklyBriefClosedWeek(
        string WeekKey,
        DateTime CoverageStartUtc,
        DateTime CoverageEndUtcExclusive
    );

    /// <summary>
    /// Location timezone → closed prior ISO week key for Weekly brief storage.
    /// Grain: one row per Owned location + this key. Absence of a row means not
    /// yet generated. Monday is local midnight in the location IANA timezone;
    /// coverage is the prior full Mon–Sun week in that timezone.
    /// </summary>
    public static class WeeklyBriefWeekKey
    {
        /// <summary>
        /// Resolve the closed prior week for <paramref name="utcNow"/> in
        /// <paramref name="ianaTimeZoneId"/>.
        /// At Monday local midnight the week that just ended becomes the closed week;
        /// just before that instant the previous closed week remains.
        /// </summary>
        public static WeeklyBriefClosedWeek ForClosedPriorWeek(
            string ianaTimeZoneId,
            DateTime utcNow
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ianaTimeZoneId);

            var utc = EnsureUtc(utcNow);
            var timeZone = ResolveTimeZone(ianaTimeZoneId.Trim());
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var localDate = DateOnly.FromDateTime(localNow);

            var daysFromMonday = ((int)localDate.DayOfWeek + 6) % 7;
            var currentWeekMonday = localDate.AddDays(-daysFromMonday);
            var closedWeekMonday = currentWeekMonday.AddDays(-7);
            var closedWeekNextMonday = closedWeekMonday.AddDays(7);

            var weekYear = ISOWeek.GetYear(closedWeekMonday.ToDateTime(TimeOnly.MinValue));
            var weekNumber = ISOWeek.GetWeekOfYear(
                closedWeekMonday.ToDateTime(TimeOnly.MinValue)
            );
            var weekKey = $"{weekYear:D4}-W{weekNumber:D2}";

            return new WeeklyBriefClosedWeek(
                weekKey,
                LocalDateStartToUtc(closedWeekMonday, timeZone),
                LocalDateStartToUtc(closedWeekNextMonday, timeZone)
            );
        }

        private static DateTime LocalDateStartToUtc(
            DateOnly localDate,
            TimeZoneInfo timeZone
        )
        {
            var localStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        }

        private static DateTime EnsureUtc(DateTime utcNow)
            => utcNow.Kind == DateTimeKind.Utc
                ? utcNow
                : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

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
