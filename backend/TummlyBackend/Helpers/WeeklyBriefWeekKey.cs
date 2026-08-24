using System.Globalization;
using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Closed prior seven-day span for a Weekly brief row, in the location timezone.
    /// </summary>
    /// <param name="WeekKey">
    /// Workspace-week identity <c>{weekStartsOn}:{yyyy-MM-dd}</c> (coverage start).
    /// </param>
    /// <param name="CoverageStartUtc">
    /// Inclusive UTC instant of start-weekday 00:00 in the location timezone.
    /// </param>
    /// <param name="CoverageEndUtcExclusive">
    /// Exclusive UTC instant of the following start-weekday 00:00.
    /// </param>
    public readonly record struct WeeklyBriefClosedWeek(
        string WeekKey,
        DateTime CoverageStartUtc,
        DateTime CoverageEndUtcExclusive
    );

    /// <summary>
    /// Location timezone → closed prior workspace-week key for Weekly brief storage.
    /// Grain: one row per Owned location + this key. Absence of a row means not
    /// yet generated. Coverage is the prior full seven-day span from the
    /// configured start weekday in that timezone.
    /// </summary>
    public static class WeeklyBriefWeekKey
    {
        /// <summary>
        /// MVP default IANA timezone when a location has no stored timezone field yet
        /// (same UK default as campaign / recovery schedule helpers).
        /// </summary>
        public const string DefaultLocationTimeZoneId = "Europe/London";

        private static readonly Regex WeekKeyPattern = new(
            @"^(monday|tuesday|wednesday|thursday|friday|saturday|sunday):\d{4}-\d{2}-\d{2}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        private static readonly Regex LegacyIsoWeekKeyPattern = new(
            @"^\d{4}-W\d{2}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        /// <summary>
        /// Trim and accept a workspace-week key or legacy ISO <c>yyyy-Www</c>.
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
            if (
                WeekKeyPattern.IsMatch(trimmed)
                || LegacyIsoWeekKeyPattern.IsMatch(trimmed)
            )
            {
                normalized = trimmed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolve the closed prior week for <paramref name="utcNow"/> in
        /// <paramref name="ianaTimeZoneId"/> using <paramref name="weekStartsOn"/>.
        /// At start-weekday local midnight the week that just ended becomes the
        /// closed week; just before that instant the previous closed week remains.
        /// </summary>
        public static WeeklyBriefClosedWeek ForClosedPriorWeek(
            string ianaTimeZoneId,
            DateTime utcNow,
            string? weekStartsOn = null
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ianaTimeZoneId);

            var startWeekday = WorkspaceDefaultsOptions.NormalizeWeekStartsOn(
                weekStartsOn
            );
            var startDay = WorkspaceDefaultsOptions.ToDayOfWeek(startWeekday);

            var utc = EnsureUtc(utcNow);
            var timeZone = ResolveTimeZone(ianaTimeZoneId.Trim());
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var localDate = DateOnly.FromDateTime(localNow);

            var daysFromStart = ((int)localDate.DayOfWeek - (int)startDay + 7) % 7;
            var currentWeekStart = localDate.AddDays(-daysFromStart);
            var closedWeekStart = currentWeekStart.AddDays(-7);
            var closedWeekNextStart = closedWeekStart.AddDays(7);

            var weekKey =
                $"{startWeekday}:{closedWeekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

            return new WeeklyBriefClosedWeek(
                weekKey,
                LocalDateStartToUtc(closedWeekStart, timeZone),
                LocalDateStartToUtc(closedWeekNextStart, timeZone)
            );
        }

        /// <summary>
        /// Whether <paramref name="utcNow"/> is the configured generate day
        /// (start weekday) in the location timezone.
        /// </summary>
        public static bool IsGenerateDay(
            string ianaTimeZoneId,
            DateTime utcNow,
            string? weekStartsOn = null
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ianaTimeZoneId);

            var startDay = WorkspaceDefaultsOptions.ToDayOfWeek(
                WorkspaceDefaultsOptions.NormalizeWeekStartsOn(weekStartsOn)
            );
            var utc = EnsureUtc(utcNow);
            var timeZone = ResolveTimeZone(ianaTimeZoneId.Trim());
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            return DateOnly.FromDateTime(localNow).DayOfWeek == startDay;
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
