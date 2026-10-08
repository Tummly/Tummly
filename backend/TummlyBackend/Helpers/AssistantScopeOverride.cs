using System.Globalization;
using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned natural-language Analysis scope overrides (period + locations).
    /// Fixed presets, rolling <c>last N days</c>, calendar phrases, compare-all,
    /// and named single-location switches.
    /// </summary>
    public static class AssistantScopeOverride
    {
        public const int MinRollingDays = 1;

        public const int MaxRollingDays = 180;

        private static readonly (string Needle, string PresetId)[] PeriodNeedles =
        [
            ("last 7 days", "last7"),
            ("last seven days", "last7"),
            ("last7", "last7"),
            ("last 30 days", "last30"),
            ("last thirty days", "last30"),
            ("last30", "last30"),
            ("this month", "thisMonth"),
            ("thismonth", "thisMonth"),
        ];

        /// <summary>
        /// Calendar / rolling phrases listed for explain-why and caveat detection.
        /// Calendar phrases among these are also applied as custom windows in
        /// <see cref="Apply"/>.
        /// </summary>
        private static readonly string[] UnmappedPeriodNeedles =
        [
            "last week",
            "this week",
            "yesterday",
            "last month",
            // Rolling last-N examples (not fixed presets) — explain-why / caveat lists.
            "last 14 days",
            "last 45 days",
            "last 90 days",
        ];

        private static readonly (string Needle, string Kind)[] CalendarPeriodNeedles =
        [
            ("yesterday", "yesterday"),
            ("this week", "thisWeek"),
            ("last week", "lastWeek"),
            ("last month", "lastMonth"),
        ];

        private static readonly string[] CompareAllNeedles =
        [
            "all locations",
            "every location",
            "all my locations",
            "all our locations",
            "all our location",
            "across all our locations",
            "across all our location",
            "across all locations",
            "across all location",
            "all of them",
            "compare all",
            "all owned locations",
            "every venue",
            "all venues",
        ];

        private static readonly Regex LastNDaysRegex = new(
            @"\blast\s+(\d+)\s+days?\b",
            RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled
        );

        public static string? TryDetectPeriodPreset(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return null;
            }

            foreach (var (needle, presetId) in PeriodNeedles)
            {
                if (lower.Contains(needle, StringComparison.Ordinal))
                {
                    return presetId;
                }
            }

            return null;
        }

        /// <summary>
        /// Detects <c>last N days</c> for N in 1–180 when no fixed preset needle matched.
        /// </summary>
        public static int? TryDetectRollingDayCount(string message)
        {
            if (TryDetectPeriodPreset(message) is not null)
            {
                return null;
            }

            var match = LastNDaysRegex.Match(message);
            if (!match.Success)
            {
                return null;
            }

            if (!int.TryParse(
                    match.Groups[1].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var days
                ))
            {
                return null;
            }

            if (days < MinRollingDays || days > MaxRollingDays)
            {
                return null;
            }

            return days;
        }

        /// <summary>
        /// Detects calendar phrases when no preset or rolling needle matched.
        /// </summary>
        public static string? TryDetectCalendarPeriodKind(string message)
        {
            if (TryDetectPeriodPreset(message) is not null
                || TryDetectRollingDayCount(message) is not null)
            {
                return null;
            }

            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return null;
            }

            foreach (var (needle, kind) in CalendarPeriodNeedles)
            {
                if (lower.Contains(needle, StringComparison.Ordinal))
                {
                    return kind;
                }
            }

            return null;
        }

        public static bool TryDetectCompareAllIntent(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return false;
            }

            foreach (var needle in CompareAllNeedles)
            {
                if (lower.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool MentionsAnyPeriodPhrase(string lower)
        {
            foreach (var (needle, _) in PeriodNeedles)
            {
                if (lower.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (var needle in UnmappedPeriodNeedles)
            {
                if (lower.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (LastNDaysRegex.IsMatch(lower))
            {
                return true;
            }

            return false;
        }

        public static IReadOnlyList<(string Needle, string? PresetId)> PeriodNeedlesForExplainWhy()
        {
            var list = new List<(string, string?)>(PeriodNeedles.Length + UnmappedPeriodNeedles.Length);
            foreach (var (needle, presetId) in PeriodNeedles)
            {
                list.Add((needle, presetId));
            }

            foreach (var needle in UnmappedPeriodNeedles)
            {
                list.Add((needle, null));
            }

            return list;
        }

        public static string PeriodLabel(string? presetId)
            => presetId switch
            {
                "last30" => "Last 30 days",
                "thisMonth" => "This month",
                _ => "Last 7 days",
            };

        public static string PeriodLabel(AssistantReportingPeriodDto period)
        {
            if (string.Equals(period.Kind, "custom", StringComparison.OrdinalIgnoreCase))
            {
                return AssistantAnalysisScope.PeriodPhrase(period);
            }

            return PeriodLabel(period.PresetId);
        }

        public static AssistantReportingPeriodDto PresetPeriod(string presetId)
            => new()
            {
                Kind = "preset",
                PresetId = NormalizePreset(presetId),
            };

        /// <summary>
        /// Inclusive rolling window ending today (UTC date): last N calendar days.
        /// Matches <see cref="AssistantReportingPeriodWindow"/> last30 = now-29 … now.
        /// </summary>
        public static AssistantReportingPeriodDto RollingDaysPeriod(
            int days,
            DateTime utcNow
        )
        {
            if (days < MinRollingDays || days > MaxRollingDays)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(days),
                    days,
                    $"Rolling days must be {MinRollingDays}–{MaxRollingDays}."
                );
            }

            var now = EnsureUtc(utcNow);
            var endDay = StartOfUtcDay(now);
            var startDay = endDay.AddDays(-(days - 1));

            return CustomPeriod(startDay, endDay);
        }

        /// <summary>
        /// Maps calendar NL phrases to inclusive UTC custom windows.
        /// </summary>
        public static AssistantReportingPeriodDto CalendarPeriod(
            string kind,
            DateTime utcNow
        )
        {
            var now = EnsureUtc(utcNow);
            var today = StartOfUtcDay(now);

            return kind switch
            {
                "yesterday" => CustomPeriod(today.AddDays(-1), today.AddDays(-1)),
                "thisWeek" => CustomPeriod(StartOfUtcWeekMonday(today), today),
                "lastWeek" => LastWeekPeriod(today),
                "lastMonth" => LastMonthPeriod(today),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "Unknown calendar period kind."
                ),
            };
        }

        /// <summary>
        /// Longest-name-first owned location whose name appears as a whole word.
        /// </summary>
        public static AssistantOwnedLocationRef? TryMatchOwnedLocation(
            string message,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations
        )
        {
            if (ownedLocations.Count == 0)
            {
                return null;
            }

            foreach (var location in ownedLocations.OrderByDescending(item => item.Name.Length))
            {
                if (ContainsName(message, location.Name))
                {
                    return location;
                }
            }

            return null;
        }

        private static string NormalizePreset(string? presetId)
            => presetId switch
            {
                "last30" => "last30",
                "thisMonth" => "thisMonth",
                _ => "last7",
            };

        private static bool SameCustomPeriod(
            AssistantReportingPeriodDto current,
            AssistantReportingPeriodDto next
        )
        {
            if (!string.Equals(current.Kind, "custom", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.Equals(current.StartDate, next.StartDate, StringComparison.Ordinal)
                && string.Equals(current.EndDate, next.EndDate, StringComparison.Ordinal);
        }

        /// <summary>
        /// Apply NL overrides onto a working scope copy. Returns null when nothing changed.
        /// </summary>
        public static AssistantScopeChangeNoticeDto? Apply(
            string userMessage,
            AssistantAnalysisScopeDto scope,
            out bool periodChanged,
            out bool locationsChanged
        )
            => Apply(
                userMessage,
                scope,
                DateTime.UtcNow,
                Array.Empty<AssistantOwnedLocationRef>(),
                out periodChanged,
                out locationsChanged
            );

        /// <summary>
        /// Apply NL overrides with an injectable clock (tests).
        /// </summary>
        public static AssistantScopeChangeNoticeDto? Apply(
            string userMessage,
            AssistantAnalysisScopeDto scope,
            DateTime utcNow,
            out bool periodChanged,
            out bool locationsChanged
        )
            => Apply(
                userMessage,
                scope,
                utcNow,
                Array.Empty<AssistantOwnedLocationRef>(),
                out periodChanged,
                out locationsChanged
            );

        /// <summary>
        /// Apply NL overrides with owned locations for named single-location switches.
        /// </summary>
        public static AssistantScopeChangeNoticeDto? Apply(
            string userMessage,
            AssistantAnalysisScopeDto scope,
            DateTime utcNow,
            IReadOnlyList<AssistantOwnedLocationRef> ownedLocations,
            out bool periodChanged,
            out bool locationsChanged
        )
        {
            periodChanged = false;
            locationsChanged = false;

            string? previousPeriodLabel = null;
            string? nextPeriodLabel = null;
            string? previousLocationLabel = null;
            string? nextLocationLabel = null;
            var kinds = new List<string>();

            var namedPreset = TryDetectPeriodPreset(userMessage);
            if (namedPreset is not null)
            {
                var currentKind = (scope.ReportingPeriod.Kind ?? "preset")
                    .Trim()
                    .ToLowerInvariant();
                var currentPreset = NormalizePreset(scope.ReportingPeriod.PresetId);
                if (currentKind == "custom"
                    || !string.Equals(namedPreset, currentPreset, StringComparison.OrdinalIgnoreCase))
                {
                    previousPeriodLabel = PeriodLabel(scope.ReportingPeriod);
                    scope.ReportingPeriod = PresetPeriod(namedPreset);
                    nextPeriodLabel = PeriodLabel(namedPreset);
                    periodChanged = true;
                    kinds.Add("period");
                }
            }
            else
            {
                var rollingDays = TryDetectRollingDayCount(userMessage);
                if (rollingDays is int days)
                {
                    // 7 / 30 match fixed presets so chrome stays on last7 / last30.
                    var nextPeriod = days switch
                    {
                        7 => PresetPeriod("last7"),
                        30 => PresetPeriod("last30"),
                        _ => RollingDaysPeriod(days, utcNow),
                    };
                    var samePeriod = days is 7 or 30
                        ? string.Equals(
                              NormalizePreset(scope.ReportingPeriod.PresetId),
                              NormalizePreset(nextPeriod.PresetId),
                              StringComparison.OrdinalIgnoreCase
                          )
                          && string.Equals(
                              (scope.ReportingPeriod.Kind ?? "preset").Trim(),
                              "preset",
                              StringComparison.OrdinalIgnoreCase
                          )
                        : SameCustomPeriod(scope.ReportingPeriod, nextPeriod);
                    if (!samePeriod)
                    {
                        previousPeriodLabel = PeriodLabel(scope.ReportingPeriod);
                        scope.ReportingPeriod = nextPeriod;
                        nextPeriodLabel = PeriodLabel(nextPeriod);
                        periodChanged = true;
                        kinds.Add("period");
                    }
                }
                else
                {
                    var calendarKind = TryDetectCalendarPeriodKind(userMessage);
                    if (calendarKind is not null)
                    {
                        var nextPeriod = CalendarPeriod(calendarKind, utcNow);
                        if (!SameCustomPeriod(scope.ReportingPeriod, nextPeriod))
                        {
                            previousPeriodLabel = PeriodLabel(scope.ReportingPeriod);
                            scope.ReportingPeriod = nextPeriod;
                            nextPeriodLabel = PeriodLabel(nextPeriod);
                            periodChanged = true;
                            kinds.Add("period");
                        }
                    }
                }
            }

            if (TryDetectCompareAllIntent(userMessage)
                && !AssistantAnalysisScope.IsAll(scope))
            {
                previousLocationLabel = string.IsNullOrWhiteSpace(scope.OwnedLocationName)
                    ? "this location"
                    : scope.OwnedLocationName;
                scope.ScopeKind = AssistantAnalysisScope.ScopeKindAll;
                scope.OwnedLocationId = null;
                scope.OwnedLocationName = AssistantAnalysisScope.AllLocationsChromeName;
                nextLocationLabel = AssistantAnalysisScope.AllLocationsChromeName;
                locationsChanged = true;
                kinds.Add("locations");
            }
            else
            {
                var named = TryMatchOwnedLocation(userMessage, ownedLocations);
                if (named is AssistantOwnedLocationRef matched
                    && (scope.OwnedLocationId is not int currentId || matched.Id != currentId
                        || AssistantAnalysisScope.IsAll(scope)))
                {
                    previousLocationLabel = AssistantAnalysisScope.IsAll(scope)
                        ? AssistantAnalysisScope.AllLocationsChromeName
                        : string.IsNullOrWhiteSpace(scope.OwnedLocationName)
                            ? "this location"
                            : scope.OwnedLocationName;
                    scope.ScopeKind = AssistantAnalysisScope.ScopeKindSingle;
                    scope.OwnedLocationId = matched.Id;
                    scope.OwnedLocationName = matched.Name;
                    nextLocationLabel = matched.Name;
                    locationsChanged = true;
                    kinds.Add("locations");
                }
            }

            if (!periodChanged && !locationsChanged)
            {
                return null;
            }

            return new AssistantScopeChangeNoticeDto
            {
                PreviousPeriodLabel = previousPeriodLabel,
                NextPeriodLabel = nextPeriodLabel,
                PreviousLocationLabel = previousLocationLabel,
                NextLocationLabel = nextLocationLabel,
                Kinds = kinds,
            };
        }

        private static AssistantReportingPeriodDto CustomPeriod(DateTime startDay, DateTime endDay)
            => new()
            {
                Kind = "custom",
                PresetId = null,
                StartDate = startDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                EndDate = endDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            };

        private static DateTime EnsureUtc(DateTime utcNow)
            => utcNow.Kind == DateTimeKind.Utc
                ? utcNow
                : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

        private static DateTime StartOfUtcDay(DateTime utcNow)
            => new(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);

        private static DateTime StartOfUtcWeekMonday(DateTime utcDay)
        {
            // Monday = 1 … Sunday = 0 after modulo.
            var offset = ((int)utcDay.DayOfWeek + 6) % 7;
            return utcDay.AddDays(-offset);
        }

        private static AssistantReportingPeriodDto LastWeekPeriod(DateTime today)
        {
            var thisMonday = StartOfUtcWeekMonday(today);
            var lastMonday = thisMonday.AddDays(-7);
            var lastSunday = thisMonday.AddDays(-1);
            return CustomPeriod(lastMonday, lastSunday);
        }

        private static AssistantReportingPeriodDto LastMonthPeriod(DateTime today)
        {
            var firstThisMonth = new DateTime(
                today.Year,
                today.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc
            );
            var firstLastMonth = firstThisMonth.AddMonths(-1);
            var lastDayLastMonth = firstThisMonth.AddDays(-1);
            return CustomPeriod(firstLastMonth, lastDayLastMonth);
        }

        private static bool ContainsName(string text, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                return false;
            }

            return Regex.IsMatch(
                text,
                $@"\b{Regex.Escape(name)}\b",
                RegexOptions.IgnoreCase
            );
        }
    }
}
