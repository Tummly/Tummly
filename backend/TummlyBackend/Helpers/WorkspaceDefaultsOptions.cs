namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Restaurant Workspace defaults wire values (ticket 10 / lock 04).
    /// Missing or invalid → product fallbacks.
    /// </summary>
    public static class WorkspaceDefaultsOptions
    {
        public const string DefaultWeekStartsOn = "monday";
        public const string DefaultReportingPeriod = "7days";

        public const string DefaultTimezone = "Europe/London";
        public const string DefaultCurrency = "GBP";
        public const string DefaultLanguage = "English";
        public const string DefaultDateFormat = "DD/MM/YYYY";

        public static readonly string[] WeekStartsOnValues =
        [
            "monday",
            "tuesday",
            "wednesday",
            "thursday",
            "friday",
            "saturday",
            "sunday",
        ];

        public static readonly string[] ReportingPeriodValues =
        [
            "7days",
            "30days",
            "thisMonth",
        ];

        public static string NormalizeWeekStartsOn(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultWeekStartsOn;
            }

            var trimmed = value.Trim().ToLowerInvariant();
            foreach (var allowed in WeekStartsOnValues)
            {
                if (string.Equals(allowed, trimmed, StringComparison.Ordinal))
                {
                    return allowed;
                }
            }

            return DefaultWeekStartsOn;
        }

        public static string NormalizeReportingPeriod(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultReportingPeriod;
            }

            var trimmed = value.Trim();
            foreach (var allowed in ReportingPeriodValues)
            {
                if (string.Equals(allowed, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return allowed;
                }
            }

            return DefaultReportingPeriod;
        }

        public static string? NormalizeCampaignSenderName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length > 200 ? trimmed[..200] : trimmed;
        }

        /// <summary>
        /// Map wire weekday to <see cref="DayOfWeek"/>.
        /// </summary>
        public static DayOfWeek ToDayOfWeek(string weekStartsOn)
        {
            return NormalizeWeekStartsOn(weekStartsOn) switch
            {
                "tuesday" => DayOfWeek.Tuesday,
                "wednesday" => DayOfWeek.Wednesday,
                "thursday" => DayOfWeek.Thursday,
                "friday" => DayOfWeek.Friday,
                "saturday" => DayOfWeek.Saturday,
                "sunday" => DayOfWeek.Sunday,
                _ => DayOfWeek.Monday,
            };
        }
    }
}
