using System.Globalization;
using System.Text.RegularExpressions;

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
        /// MVP default IANA timezone when a location has no stored timezone field yet
        /// (same UK default as campaign / recovery schedule helpers).
        /// </summary>
        public const string DefaultLocationTimeZoneId = "Europe/London";

        private static readonly Regex WeekKeyPattern = new(
            @"^\d{4}-W\d{2}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        /// <summary>
        /// True when <paramref name="weekKey"/> matches ISO week key form
        /// <c>yyyy-Www</c> (after trim).
        /// </summary>
        public static bool IsValidWeekKey(string? weekKey)
            => TryNormalizeWeekKey(weekKey, out _);

        /// <summary>
        /// Trim and accept an ISO week key <c>yyyy-Www</c>.
        /// </summary>
        public static bool TryNormalizeWeekKey(
            string? weekKey,
            out string normalized
        )
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(weekKey))
            {
                return false;
            }

            var trimmed = weekKey.Trim();
            if (!WeekKeyPattern.IsMatch(trimmed))
            {
                return false;
            }

            normalized = trimmed;
            return true;
        }

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
