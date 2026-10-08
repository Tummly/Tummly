using System.Globalization;
using System.Text.RegularExpressions;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Light grounding / OIR validation for Weekly brief generate (locks 03 / 05 / 06).
    /// </summary>
    public static partial class WeeklyBriefInsightNarrativeValidation
    {
        private static readonly string[] BannedCausalPhrases =
        [
            "because",
            "caused by",
            "due to",
            "as a result of",
        ];

        public static bool IsSoleNoMaterialChange(WeeklyBriefInsightCandidateBag? bag)
            => bag is not null
                && bag.Candidates.Count == 1
                && string.Equals(
                    bag.Candidates[0].Type,
                    WeeklyBriefInsightCandidates.TypeNoMaterialChange,
                    StringComparison.Ordinal
                );

        public static bool TryValidate(
            WeeklyBriefBody body,
            WeeklyBriefEnrichment enrichment,
            WeeklyBriefInsightCandidateBag? bag,
            out string? reason
        )
        {
            reason = null;
            if (bag is null)
            {
                return true;
            }

            if (bag.Candidates.Count == 0)
            {
                reason = "insight candidate bag must not be empty";
                return false;
            }

            if (!TryValidateWatchNext(body.WatchNext, bag, out reason))
            {
                return false;
            }

            var narratives = enrichment.InsightNarratives ?? [];
            var byId = bag.Candidates.ToDictionary(
                c => c.Id,
                StringComparer.Ordinal
            );

            if (narratives.Count == 0)
            {
                reason = "insightNarratives required for each candidate";
                return false;
            }

            var coveredIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var narrative in narratives)
            {
                if (!byId.TryGetValue(narrative.CandidateId, out var candidate))
                {
                    reason = "unknown candidateId";
                    return false;
                }

                if (!coveredIds.Add(narrative.CandidateId))
                {
                    reason = "duplicate candidateId in insightNarratives";
                    return false;
                }

                if (
                    string.IsNullOrWhiteSpace(narrative.Observation)
                    || string.IsNullOrWhiteSpace(narrative.Interpretation)
                )
                {
                    reason = "observation and interpretation required";
                    return false;
                }

                var recommendation = narrative.Recommendation?.Trim();
                var hasRecommendation = !string.IsNullOrWhiteSpace(recommendation);
                if (
                    string.Equals(
                        candidate.Type,
                        WeeklyBriefInsightCandidates.TypeNoMaterialChange,
                        StringComparison.Ordinal
                    )
                    && hasRecommendation
                )
                {
                    reason = "recommendation forbidden for no-material-change";
                    return false;
                }

                if (
                    IsControlLinked(candidate.Type)
                    && !hasRecommendation
                )
                {
                    reason = "recommendation required for control-linked candidate";
                    return false;
                }

                var prose =
                    $"{narrative.Observation} {narrative.Interpretation}"
                    + (hasRecommendation ? $" {recommendation}" : string.Empty);

                if (
                    !candidate.Evidence.CausalEvidence
                    && ContainsBannedCausalPhrase(prose)
                )
                {
                    reason = "ungrounded causation";
                    return false;
                }

                if (!NumbersGroundedInSnapshot(prose, candidate.Evidence.Snapshot))
                {
                    reason = "number outside candidate snapshot";
                    return false;
                }
            }

            if (coveredIds.Count != bag.Candidates.Count)
            {
                reason = "insightNarratives must cover every candidate";
                return false;
            }

            return true;
        }

        public static WeeklyBriefEnrichment AttachCandidates(
            WeeklyBriefEnrichment? enrichment,
            WeeklyBriefInsightCandidateBag bag
        )
        {
            enrichment ??= new WeeklyBriefEnrichment(
                ExecutiveSummary: null,
                FeedbackSummary: null,
                ActionWording: []
            );
            return enrichment with
            {
                InsightCandidates = bag.Candidates,
                InsightNarratives = enrichment.InsightNarratives ?? [],
            };
        }

        private static bool TryValidateWatchNext(
            IReadOnlyList<string> watchNext,
            WeeklyBriefInsightCandidateBag bag,
            out string? reason
        )
        {
            reason = null;
            if (IsSoleNoMaterialChange(bag))
            {
                if (watchNext.Count != 0)
                {
                    reason = "watchNext must be empty for sole no-material-change";
                    return false;
                }

                return true;
            }

            if (watchNext.Count < 1 || watchNext.Count > WeeklyBriefStructuredOutput.WatchNextMaxLength)
            {
                reason = "watchNext must have 1 to 3 lines when candidates are actionable";
                return false;
            }

            return true;
        }

        private static bool IsControlLinked(string type)
            => string.Equals(
                    type,
                    WeeklyBriefInsightCandidates.TypeControlSignal,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    type,
                    WeeklyBriefInsightCandidates.TypeFunnelDrop,
                    StringComparison.Ordinal
                );

        private static bool ContainsBannedCausalPhrase(string prose)
        {
            foreach (var phrase in BannedCausalPhrases)
            {
                if (prose.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NumbersGroundedInSnapshot(
            string prose,
            IReadOnlyDictionary<string, object> snapshot
        )
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in snapshot.Values)
            {
                switch (value)
                {
                    case int i:
                        allowed.Add(i.ToString(CultureInfo.InvariantCulture));
                        break;
                    case long l:
                        allowed.Add(l.ToString(CultureInfo.InvariantCulture));
                        break;
                    case double d:
                        allowed.Add(
                            d.ToString("0.####", CultureInfo.InvariantCulture)
                        );
                        allowed.Add(
                            ((int)Math.Round(d, MidpointRounding.AwayFromZero))
                                .ToString(CultureInfo.InvariantCulture)
                        );
                        break;
                    case float f:
                        allowed.Add(
                            f.ToString("0.####", CultureInfo.InvariantCulture)
                        );
                        break;
                    case decimal m:
                        allowed.Add(
                            m.ToString("0.####", CultureInfo.InvariantCulture)
                        );
                        break;
                    case string s
                        when double.TryParse(
                            s,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var parsed
                        ):
                        allowed.Add(
                            parsed.ToString("0.####", CultureInfo.InvariantCulture)
                        );
                        break;
                }
            }

            foreach (Match match in NumberTokenRegex().Matches(prose))
            {
                var token = match.Value;
                if (token.Contains('.', StringComparison.Ordinal))
                {
                    if (
                        !double.TryParse(
                            token,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var parsed
                        )
                    )
                    {
                        return false;
                    }

                    var normalized = parsed.ToString(
                        "0.####",
                        CultureInfo.InvariantCulture
                    );
                    if (!allowed.Contains(normalized))
                    {
                        return false;
                    }
                }
                else if (!allowed.Contains(token))
                {
                    return false;
                }
            }

            return true;
        }

        [GeneratedRegex(@"\d+(?:\.\d+)?", RegexOptions.CultureInvariant)]
        private static partial Regex NumberTokenRegex();
    }
}
