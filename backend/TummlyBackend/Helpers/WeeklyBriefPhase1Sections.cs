using System.Globalization;
using System.Text;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Phase-1 Weekly brief What changed rows + Feedback summary facts for the
    /// shared ready envelope (Reports Figma page; Home ignores unused fields).
    /// </summary>
    /// <remarks>
    /// <para>
    /// What changed candidate areas (fixed order): QR scans, Feedback received,
    /// Contactable guests, Offer redemptions, Unsubscribes — mapped to
    /// <see cref="WeeklyBriefMetrics.QrScanEvents"/>,
    /// <see cref="WeeklyBriefMetrics.FeedbackCount"/>,
    /// <see cref="WeeklyBriefMetrics.GuestsJoined"/>,
    /// <see cref="WeeklyBriefMetrics.RedemptionsInWeek"/>,
    /// <see cref="WeeklyBriefMetrics.UnsubscribesInWeek"/>.
    /// </para>
    /// <para>
    /// When prior-week metrics exist and prior &gt; 0:
    /// percent = round((current − prior) / prior × 100); omit row when percent
    /// is 0; change is a signed percent string (e.g. "+12%", "-4%").
    /// When prior is missing or prior is 0: emit "{n} total" when current &gt; 0;
    /// otherwise omit the row. Empty list → hide the section.
    /// </para>
    /// <para>
    /// Feedback summary (phase 1, no theme AI): present when FeedbackCount &gt; 0
    /// or NeedsAttentionCount &gt; 0. Text from counts + up to two Detected Tag
    /// labels by count; subtitle uses the period label. Null → hide the section.
    /// </para>
    /// </remarks>
    public static class WeeklyBriefPhase1Sections
    {
        public sealed record WhatChangedRowDto(
            string Area,
            string Change,
            string Meaning
        );

        public sealed record FeedbackSummaryDto(
            string Text,
            string Subtitle,
            int NeedsAttentionCount
        );

        public static IReadOnlyList<WhatChangedRowDto> BuildWhatChanged(
            WeeklyBriefMetrics current,
            WeeklyBriefMetrics? prior
        )
        {
            var rows = new List<WhatChangedRowDto>(5);
            TryAddRow(
                rows,
                "QR scans",
                current.QrScanEvents,
                prior?.QrScanEvents,
                up: "More guests are engaging with your QR placements.",
                down: "Fewer guests are engaging with your QR placements.",
                absolute: "Guests are engaging with your QR placements."
            );
            TryAddRow(
                rows,
                "Feedback received",
                current.FeedbackCount,
                prior?.FeedbackCount,
                up: "More guests are sharing private feedback.",
                down: "Fewer guests are sharing private feedback.",
                absolute: "Guests are sharing private feedback."
            );
            TryAddRow(
                rows,
                "Contactable guests",
                current.GuestsJoined,
                prior?.GuestsJoined,
                up: "Your guest list is growing.",
                down: "Fewer guests joined this week.",
                absolute: "Your guest list is growing."
            );
            TryAddRow(
                rows,
                "Offer redemptions",
                current.RedemptionsInWeek,
                prior?.RedemptionsInWeek,
                up: "More guests are redeeming claimed offers.",
                down: "Claimed offers may need clearer staff visibility.",
                absolute: "Guests are redeeming claimed offers."
            );
            TryAddRow(
                rows,
                "Unsubscribes",
                current.UnsubscribesInWeek,
                prior?.UnsubscribesInWeek,
                up: "Review message frequency and audience relevance.",
                down: "Fewer guests opted out of marketing.",
                absolute: "Review message frequency and audience relevance."
            );
            return rows;
        }

        public static FeedbackSummaryDto? BuildFeedbackSummary(
            WeeklyBriefMetrics metrics,
            string periodLabel
        )
        {
            if (metrics.FeedbackCount <= 0 && metrics.NeedsAttentionCount <= 0)
            {
                return null;
            }

            var text = new StringBuilder();
            if (metrics.FeedbackCount > 0)
            {
                text.Append(
                    CultureInfo.InvariantCulture,
                    $"{metrics.FeedbackCount} private feedback messages this week"
                );
                text.Append(
                    CultureInfo.InvariantCulture,
                    $" ({metrics.PositiveFeedbackCount} positive, {metrics.NeutralFeedbackCount} neutral, {metrics.NegativeFeedbackCount} negative)."
                );
            }
            else
            {
                text.Append("No new private feedback messages this week.");
            }

            if (metrics.NeedsAttentionCount > 0)
            {
                text.Append(' ');
                text.Append(
                    CultureInfo.InvariantCulture,
                    $"{metrics.NeedsAttentionCount} may need follow-up."
                );
            }

            var topTags = TopDetectedTags(metrics.DetectedTagCounts, 2);
            if (topTags.Count > 0)
            {
                text.Append(' ');
                text.Append(
                    CultureInfo.InvariantCulture,
                    $"Top themes: {string.Join(", ", topTags)}."
                );
            }

            return new FeedbackSummaryDto(
                text.ToString(),
                $"Based on private feedback submitted between {periodLabel}.",
                metrics.NeedsAttentionCount
            );
        }

        private static void TryAddRow(
            List<WhatChangedRowDto> rows,
            string area,
            int current,
            int? prior,
            string up,
            string down,
            string absolute
        )
        {
            if (prior is int priorValue && priorValue > 0)
            {
                var percent = (int)Math.Round(
                    (current - priorValue) * 100.0 / priorValue,
                    MidpointRounding.AwayFromZero
                );
                if (percent == 0)
                {
                    return;
                }

                var sign = percent > 0 ? "+" : string.Empty;
                rows.Add(
                    new WhatChangedRowDto(
                        area,
                        $"{sign}{percent}%",
                        percent > 0 ? up : down
                    )
                );
                return;
            }

            if (current <= 0)
            {
                return;
            }

            rows.Add(
                new WhatChangedRowDto(
                    area,
                    $"{current.ToString(CultureInfo.InvariantCulture)} total",
                    absolute
                )
            );
        }

        private static IReadOnlyList<string> TopDetectedTags(
            IReadOnlyDictionary<string, int> counts,
            int take
        )
        {
            if (counts.Count == 0)
            {
                return Array.Empty<string>();
            }

            return counts
                .Where(pair => pair.Value > 0 && !string.IsNullOrWhiteSpace(pair.Key))
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Take(take)
                .Select(pair => pair.Key.Trim())
                .ToList();
        }
    }
}
