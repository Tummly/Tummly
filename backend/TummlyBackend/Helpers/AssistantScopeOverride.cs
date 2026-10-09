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
            "today",
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

        private static readonly Regex TodayRegex = new(
            @"\btoday\b",
            RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled
        );

        private static readonly Regex NamedReportingDayRegex = new(
            @"\b(?:(?<iso>\d{4}-\d{2}-\d{2})|(?<numeric>\d{1,2}[/-]\d{1,2}[/-]\d{4})|(?<day>\d{1,2})(?:st|nd|rd|th)?(?:\s+of)?\s+(?<month>jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\.?(?:\s+(?<year>\d{4}))?|(?<month2>jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\.?\s+(?<day2>\d{1,2})(?:st|nd|rd|th)?(?:\s+(?<year2>\d{4}))?)\b",
            RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled
        );

        private static readonly Regex WeekdayRegex = new(
            @"\b(?<rel>last|this|on)\s+(?<day>monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b",
            RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled
        );

        private static readonly string[] ReportingDayCues =
        [
            "feedback",
            "scan",
            "guest",
            "kpi",
            "redemption",
            "capture",
            "performance",
            "campaign",
            "how many",
            "how is",
            "received",
            "joined",
        ];

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

            // "What should I do today" is an attention ask, not a one-day window.
            if (TodayRegex.IsMatch(lower)
                && !AssistantAttentionAsk.IsAttentionRetrieve(message))
            {
                return "today";
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

        /// <summary>
        /// One calendar day named in a data question: "3 October", "03/10/2026",
        /// or "on Monday". Create, schedule, and offer-validity dates are left
        /// alone. More than one day is not guessed.
        /// </summary>
        public static DateTime? TryDetectNamedReportingDay(string message, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(message)
                || !LooksLikeReportingDayAsk(message)
                || AssistantTaskClassification.LooksLikeCreateTurn(message)
                || AssistantSendScheduleAsk.LooksLikeSendOrSchedule(message))
            {
                return null;
            }

            var named = NamedReportingDayRegex.Matches(message);
            if (named.Count == 1 && TryParseNamedReportingDay(named[0], utcNow, out var day))
            {
                return day;
            }

            if (named.Count > 0)
            {
                return null;
            }

            var weekdays = WeekdayRegex.Matches(message);
            if (weekdays.Count != 1)
            {
                return null;
            }

            return WeekdayOnOrBefore(
                weekdays[0].Groups["rel"].Value,
                weekdays[0].Groups["day"].Value,
                utcNow
            );
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
                "today" => CustomPeriod(today, today),
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
                    var namedDay = calendarKind is null
                        ? TryDetectNamedReportingDay(userMessage, utcNow)
                        : null;
                    var nextPeriod = calendarKind is not null
                        ? CalendarPeriod(calendarKind, utcNow)
                        : namedDay is DateTime day
                            ? CustomPeriod(day, day)
                            : null;
                    if (nextPeriod is not null
                        && !SameCustomPeriod(scope.ReportingPeriod, nextPeriod))
                    {
                        previousPeriodLabel = PeriodLabel(scope.ReportingPeriod);
                        scope.ReportingPeriod = nextPeriod;
                        nextPeriodLabel = PeriodLabel(nextPeriod);
                        periodChanged = true;
                        kinds.Add("period");
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

        private static bool LooksLikeReportingDayAsk(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            foreach (var cue in ReportingDayCues)
            {
                if (lower.Contains(cue, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseNamedReportingDay(
            Match match,
            DateTime utcNow,
            out DateTime day
        )
        {
            day = default;
            var now = EnsureUtc(utcNow);
            if (match.Groups["iso"].Success
                && DateTime.TryParseExact(
                    match.Groups["iso"].Value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var iso
                ))
            {
                day = StartOfUtcDay(iso);
                return true;
            }

            if (match.Groups["numeric"].Success)
            {
                var parts = match.Groups["numeric"].Value.Split('/', '-');
                if (parts.Length == 3
                    && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var first)
                    && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var second)
                    && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
                    && TryBuildDayMonth(first, second, year, out day))
                {
                    return true;
                }

                return false;
            }

            var dayText = match.Groups["day"].Success
                ? match.Groups["day"].Value
                : match.Groups["day2"].Value;
            var monthText = match.Groups["month"].Success
                ? match.Groups["month"].Value
                : match.Groups["month2"].Value;
            var yearText = match.Groups["year"].Success
                ? match.Groups["year"].Value
                : match.Groups["year2"].Value;
            if (!int.TryParse(dayText, NumberStyles.None, CultureInfo.InvariantCulture, out var dayNumber)
                || !TryMonthNumber(monthText, out var monthNumber))
            {
                return false;
            }

            int? explicitYear = null;
            if (!string.IsNullOrEmpty(yearText))
            {
                if (!int.TryParse(yearText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedYear))
                {
                    return false;
                }

                explicitYear = parsedYear;
            }

            var yearNumber = explicitYear ?? now.Year;
            if (!TryCalendarDay(yearNumber, monthNumber, dayNumber, out day))
            {
                return false;
            }

            if (explicitYear is null && day.Date > now.Date)
            {
                if (!TryCalendarDay(yearNumber - 1, monthNumber, dayNumber, out day))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Slash dates are day/month. When only one side can be a month, use that.
        /// </summary>
        private static bool TryBuildDayMonth(int first, int second, int year, out DateTime day)
        {
            day = default;
            var dayNumber = first;
            var monthNumber = second;
            if (first > 12 && second <= 12)
            {
                dayNumber = first;
                monthNumber = second;
            }
            else if (second > 12 && first <= 12)
            {
                dayNumber = second;
                monthNumber = first;
            }

            return TryCalendarDay(year, monthNumber, dayNumber, out day);
        }

        private static bool TryCalendarDay(int year, int month, int dayNumber, out DateTime day)
        {
            day = default;
            if (month is < 1 or > 12 || dayNumber < 1 || year < 1 || year > 9999)
            {
                return false;
            }

            if (dayNumber > DateTime.DaysInMonth(year, month))
            {
                return false;
            }

            day = new DateTime(year, month, dayNumber, 0, 0, 0, DateTimeKind.Utc);
            return true;
        }

        private static bool TryMonthNumber(string text, out int month)
        {
            month = 0;
            var token = text.Trim().TrimEnd('.').ToLowerInvariant();
            if (token.StartsWith("sept", StringComparison.Ordinal))
            {
                token = "sep";
            }

            if (token.Length > 3)
            {
                token = token[..3];
            }

            month = token switch
            {
                "jan" => 1,
                "feb" => 2,
                "mar" => 3,
                "apr" => 4,
                "may" => 5,
                "jun" => 6,
                "jul" => 7,
                "aug" => 8,
                "sep" => 9,
                "oct" => 10,
                "nov" => 11,
                "dec" => 12,
                _ => 0,
            };
            return month != 0;
        }

        private static DateTime WeekdayOnOrBefore(string relation, string weekdayName, DateTime utcNow)
        {
            var today = StartOfUtcDay(EnsureUtc(utcNow));
            var target = Enum.Parse<DayOfWeek>(weekdayName, ignoreCase: true);
            var delta = ((int)today.DayOfWeek - (int)target + 7) % 7;
            if (relation.Equals("last", StringComparison.OrdinalIgnoreCase) && delta == 0)
            {
                delta = 7;
            }

            return today.AddDays(-delta);
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
