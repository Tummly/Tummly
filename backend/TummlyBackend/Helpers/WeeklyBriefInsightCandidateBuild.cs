using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Load prior aggregates + control signals, then emit insight candidates.
    /// </summary>
    public static class WeeklyBriefInsightCandidateBuild
    {
        /// <summary>
        /// Build the candidate bag for a closed week. Prior metrics are fresh
        /// aggregates (not stored prior-brief <c>MetricsJson</c>).
        /// </summary>
        public static async Task<WeeklyBriefInsightCandidateBag> BuildAsync(
            ApplicationDbContext context,
            int locationId,
            string weekKey,
            DateTime coverageStartUtc,
            DateTime coverageEndUtcExclusive,
            WeeklyBriefMetrics currentMetrics,
            string? ianaTimeZoneId = null,
            CancellationToken cancellationToken = default
        )
        {
            var tz =
                string.IsNullOrWhiteSpace(ianaTimeZoneId)
                    ? WeeklyBriefWeekKey.DefaultLocationTimeZoneId
                    : ianaTimeZoneId.Trim();

            var prior = await WeeklyBriefMetricsLoader.LoadPriorWeekAsync(
                context,
                locationId,
                weekKey,
                tz,
                cancellationToken
            );

            WeeklyBriefMetrics? priorPrior = null;
            if (
                WeeklyBriefWeekKey.TryPriorWeekKey(weekKey, out var priorKey)
            )
            {
                priorPrior = await WeeklyBriefMetricsLoader.LoadPriorWeekAsync(
                    context,
                    locationId,
                    priorKey,
                    tz,
                    cancellationToken
                );
            }

            var facts = await WeeklyBriefRecommendedActions.BuildFactsAsync(
                context,
                locationId,
                currentMetrics,
                coverageStartUtc,
                coverageEndUtcExclusive,
                cancellationToken
            );
            var controlSignals = MapControlSignals(facts);
            var dataQuality =
                WeeklyBriefInsightCandidates.IsDataQualityIssue(currentMetrics);

            return WeeklyBriefInsightCandidates.Emit(
                currentMetrics,
                prior,
                priorPrior,
                controlSignals,
                dataQuality
            );
        }

        private static IReadOnlyList<WeeklyBriefControlSignalInput> MapControlSignals(
            IReadOnlyList<object> facts
        )
        {
            var signals = new List<WeeklyBriefControlSignalInput>(facts.Count);
            foreach (var fact in facts)
            {
                switch (fact)
                {
                    case WeeklyBriefRecommendedActions.FeedbackNeedsAttentionFactDto f:
                        signals.Add(
                            new WeeklyBriefControlSignalInput(
                                f.Kind,
                                new Dictionary<string, object>(StringComparer.Ordinal)
                                {
                                    ["needsAttentionCount"] = f.Count,
                                }
                            )
                        );
                        break;
                    case WeeklyBriefRecommendedActions.UnderperformQrFactDto f:
                        signals.Add(
                            new WeeklyBriefControlSignalInput(
                                f.Kind,
                                new Dictionary<string, object>(StringComparer.Ordinal)
                                {
                                    ["qrCodeId"] = f.QrCodeId,
                                    ["scans"] = f.Scans,
                                    ["contactable"] = f.Contactable,
                                    ["placementLabel"] = f.PlacementLabel,
                                }
                            )
                        );
                        break;
                    case WeeklyBriefRecommendedActions.RepeatedInvalidFactDto f:
                        signals.Add(
                            new WeeklyBriefControlSignalInput(
                                f.Kind,
                                new Dictionary<string, object>(StringComparer.Ordinal)
                                {
                                    ["count"] = f.Count,
                                }
                            )
                        );
                        break;
                    case WeeklyBriefRecommendedActions.LowRedemptionFactDto f:
                        signals.Add(
                            new WeeklyBriefControlSignalInput(
                                f.Kind,
                                new Dictionary<string, object>(StringComparer.Ordinal)
                                {
                                    ["offerId"] = f.OfferId,
                                    ["claims"] = f.Claims,
                                    ["redemptions"] = f.Redemptions,
                                    ["rate"] = f.Rate,
                                }
                            )
                        );
                        break;
                }
            }

            return signals;
        }
    }
}
