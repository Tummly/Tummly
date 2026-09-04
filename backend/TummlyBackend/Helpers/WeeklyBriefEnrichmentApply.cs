using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Prefer phase-2 enrichment over phase-1 derive when present and non-empty.
    /// Deterministic facts stay the gate; enrichment may only supply wording.
    /// </summary>
    public static class WeeklyBriefEnrichmentApply
    {
        public static string ResolveExecutiveSummary(
            string phase1ExecutiveSummary,
            WeeklyBriefEnrichment? enrichment
        )
        {
            if (!string.IsNullOrWhiteSpace(enrichment?.ExecutiveSummary))
            {
                return enrichment.ExecutiveSummary.Trim();
            }

            return phase1ExecutiveSummary;
        }

        public static WeeklyBriefPhase1Sections.FeedbackSummaryDto? ResolveFeedbackSummary(
            WeeklyBriefPhase1Sections.FeedbackSummaryDto? phase1,
            WeeklyBriefMetrics metrics,
            WeeklyBriefEnrichment? enrichment
        )
        {
            var enriched = enrichment?.FeedbackSummary;
            if (enriched is null || string.IsNullOrWhiteSpace(enriched.Text))
            {
                return phase1;
            }

            var subtitle = string.IsNullOrWhiteSpace(enriched.Subtitle)
                ? phase1?.Subtitle ?? string.Empty
                : enriched.Subtitle.Trim();

            return new WeeklyBriefPhase1Sections.FeedbackSummaryDto(
                enriched.Text.Trim(),
                subtitle,
                metrics.NeedsAttentionCount
            );
        }

        public static IReadOnlyList<object> ApplyActionWording(
            IReadOnlyList<object> facts,
            WeeklyBriefEnrichment? enrichment
        )
        {
            if (enrichment is null || enrichment.ActionWording.Count == 0)
            {
                return facts;
            }

            var byKind = new Dictionary<string, WeeklyBriefEnrichmentActionWording>(
                StringComparer.Ordinal
            );
            foreach (var wording in enrichment.ActionWording)
            {
                if (!byKind.ContainsKey(wording.Kind))
                {
                    byKind[wording.Kind] = wording;
                }
            }

            var result = new List<object>(facts.Count);
            foreach (var fact in facts)
            {
                result.Add(
                    fact switch
                    {
                        WeeklyBriefRecommendedActions.FeedbackNeedsAttentionFactDto feedback
                            when byKind.TryGetValue(feedback.Kind, out var wording)
                            => feedback with
                            {
                                Title = wording.Title,
                                Subtitle = wording.Subtitle,
                            },
                        WeeklyBriefRecommendedActions.RepeatedInvalidFactDto repeated
                            when byKind.TryGetValue(repeated.Kind, out var wording)
                            => repeated with
                            {
                                Title = wording.Title,
                                Subtitle = wording.Subtitle,
                            },
                        WeeklyBriefRecommendedActions.LowRedemptionFactDto low
                            when byKind.TryGetValue(low.Kind, out var wording)
                            => low with
                            {
                                Title = wording.Title,
                                Subtitle = wording.Subtitle,
                            },
                        _ => fact,
                    }
                );
            }

            return result;
        }

        public static WeeklyBriefEnrichment? TryDeserialize(string? enrichmentJson)
        {
            if (string.IsNullOrWhiteSpace(enrichmentJson))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<WeeklyBriefEnrichment>(
                    enrichmentJson,
                    WeeklyBriefStoreJson.Options
                );
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        public static string FormatRecommendedActionLine(object fact)
            => fact switch
            {
                WeeklyBriefRecommendedActions.FeedbackNeedsAttentionFactDto feedback
                    => !string.IsNullOrWhiteSpace(feedback.Title)
                        ? feedback.Title
                        : $"Follow up with {feedback.Count} guests",
                WeeklyBriefRecommendedActions.RepeatedInvalidFactDto repeated
                    => !string.IsNullOrWhiteSpace(repeated.Title)
                        ? repeated.Title
                        : $"Repeated invalid attempts ({repeated.Count})",
                WeeklyBriefRecommendedActions.LowRedemptionFactDto low
                    => !string.IsNullOrWhiteSpace(low.Title)
                        ? low.Title
                        : $"High claims, lower redemptions - {low.OfferTitle} ({low.Claims} claims, {low.Redemptions} redemptions)",
                _ => "Recommended action",
            };
    }
}
