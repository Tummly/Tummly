using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned natural-language Analysis scope overrides (period + all locations).
    /// Model cannot invent open-ended date ranges; presets only.
    /// </summary>
    public static class AssistantScopeOverride
    {
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
        /// Period phrases that are recognized but not mappable to a preset
        /// (used by explain-why "names new period" and TwoPeriodCaveat lists).
        /// </summary>
        private static readonly string[] UnmappedPeriodNeedles =
        [
            "last week",
            "this week",
            "yesterday",
            "last month",
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

        private static string NormalizePreset(string? presetId)
            => presetId switch
            {
                "last30" => "last30",
                "thisMonth" => "thisMonth",
                _ => "last7",
            };

        /// <summary>
        /// Apply NL overrides onto a working scope copy. Returns null when nothing changed.
        /// </summary>
        public static AssistantScopeChangeNoticeDto? Apply(
            string userMessage,
            AssistantAnalysisScopeDto scope,
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
    }
}
