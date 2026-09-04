using System.Globalization;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Phase-1 Weekly brief meta + executive summary for the shared ready envelope
    /// (Reports Figma page; Home ignores unused fields).
    /// </summary>
    /// <remarks>
    /// Confidence thresholds (activityScore = GuestsJoined + QrScanEvents +
    /// FeedbackCount + ClaimsInWeek + RedemptionsInWeek + CampaignsSentInWeek;
    /// domainsWithData = count of Capture/Feedback/Offers/Campaigns with HasData):
    /// <list type="bullet">
    /// <item>
    /// High: domainsWithData &gt;= 3 AND activityScore &gt;= 20 →
    /// "Based on enough activity to show useful patterns."
    /// </item>
    /// <item>
    /// Medium: domainsWithData &gt;= 2 OR activityScore &gt;= 8 →
    /// "Based on moderate activity; patterns may be early."
    /// </item>
    /// <item>
    /// Low: else → "Based on limited activity; treat patterns as directional."
    /// </item>
    /// </list>
    /// Period: workspace week <c>weekday:yyyy-MM-dd</c> → en-GB coverage
    /// "6–12 July" (start through start+6 days, Europe/London calendar dates).
    /// Legacy <c>yyyy-Www</c> → "Week {ww}, {yyyy}".
    /// </remarks>
    public static class WeeklyBriefPhase1Meta
    {
        public const string ConfidenceHighCopy =
            "Based on enough activity to show useful patterns.";

        public const string ConfidenceMediumCopy =
            "Based on moderate activity; patterns may be early.";

        public const string ConfidenceLowCopy =
            "Based on limited activity; treat patterns as directional.";

        public const int HighDomainsMin = 3;
        public const int HighActivityMin = 20;
        public const int MediumDomainsMin = 2;
        public const int MediumActivityMin = 8;

        public sealed record MetaDto(
            string Period,
            IReadOnlyList<string> DataSources,
            string Confidence,
            string ConfidenceLevel
        );

        public sealed record Phase1ReadyFields(
            MetaDto Meta,
            string ExecutiveSummary
        );

        public static Phase1ReadyFields Build(
            WeeklyBriefBody body,
            WeeklyBriefMetrics metrics,
            string weekKey
        )
        {
            return new Phase1ReadyFields(
                BuildMeta(body, metrics, weekKey),
                BuildExecutiveSummary(body)
            );
        }

        public static MetaDto BuildMeta(
            WeeklyBriefBody body,
            WeeklyBriefMetrics metrics,
            string weekKey
        )
        {
            var dataSources = BuildDataSources(body);
            var (level, copy) = ResolveConfidence(body, metrics);
            return new MetaDto(
                FormatPeriod(weekKey),
                dataSources,
                copy,
                level
            );
        }

        public static string BuildExecutiveSummary(WeeklyBriefBody body)
        {
            var parts = new List<string>();
            var headline = body.Headline?.Trim();
            if (!string.IsNullOrEmpty(headline))
            {
                parts.Add(headline);
            }

            AppendSectionSummary(parts, body.Capture);
            AppendSectionSummary(parts, body.Feedback);
            AppendSectionSummary(parts, body.Offers);
            AppendSectionSummary(parts, body.Campaigns);

            return string.Join(' ', parts);
        }

        public static IReadOnlyList<string> BuildDataSources(WeeklyBriefBody body)
        {
            var sources = new List<string>(4);
            if (body.Capture.HasData)
            {
                sources.Add("Capture");
            }

            if (body.Feedback.HasData)
            {
                sources.Add("Feedback");
            }

            if (body.Offers.HasData)
            {
                sources.Add("Offers");
            }

            if (body.Campaigns.HasData)
            {
                sources.Add("Campaigns");
            }

            return sources;
        }

        /// <summary>
        /// Resolve confidence tier from domains-with-data and activity score.
        /// </summary>
        public static (string Level, string Copy) ResolveConfidence(
            WeeklyBriefBody body,
            WeeklyBriefMetrics metrics
        )
        {
            var domainsWithData = CountDomainsWithData(body);
            var activityScore = ActivityScore(metrics);

            if (
                domainsWithData >= HighDomainsMin
                && activityScore >= HighActivityMin
            )
            {
                return ("high", ConfidenceHighCopy);
            }

            if (
                domainsWithData >= MediumDomainsMin
                || activityScore >= MediumActivityMin
            )
            {
                return ("medium", ConfidenceMediumCopy);
            }

            return ("low", ConfidenceLowCopy);
        }

        public static int ActivityScore(WeeklyBriefMetrics metrics)
            => metrics.GuestsJoined
                + metrics.QrScanEvents
                + metrics.FeedbackCount
                + metrics.ClaimsInWeek
                + metrics.RedemptionsInWeek
                + metrics.CampaignsSentInWeek;

        public static int CountDomainsWithData(WeeklyBriefBody body)
            => BuildDataSources(body).Count;

        /// <summary>
        /// Format period label from workspace or legacy week key.
        /// </summary>
        public static string FormatPeriod(string weekKey)
        {
            if (string.IsNullOrWhiteSpace(weekKey))
            {
                return weekKey;
            }

            var trimmed = weekKey.Trim();
            var colon = trimmed.IndexOf(':');
            if (
                colon > 0
                && DateOnly.TryParseExact(
                    trimmed[(colon + 1)..],
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var start
                )
            )
            {
                var end = start.AddDays(6);
                return FormatCoverageRange(start, end);
            }

            // Legacy ISO yyyy-Www → stable "Week {ww}, {yyyy}".
            if (
                trimmed.Length == 8
                && trimmed[4] == '-'
                && trimmed[5] == 'W'
                && int.TryParse(
                    trimmed.AsSpan(0, 4),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var year
                )
                && int.TryParse(
                    trimmed.AsSpan(6, 2),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var week
                )
            )
            {
                return $"Week {week}, {year}";
            }

            return trimmed;
        }

        private static string FormatCoverageRange(DateOnly start, DateOnly end)
        {
            var enGb = CultureInfo.GetCultureInfo("en-GB");
            var startDay = start.Day.ToString(CultureInfo.InvariantCulture);
            var endDay = end.Day.ToString(CultureInfo.InvariantCulture);
            var startMonth = start.ToString("MMMM", enGb);
            var endMonth = end.ToString("MMMM", enGb);

            if (start.Year == end.Year && start.Month == end.Month)
            {
                return $"{startDay}–{endDay} {startMonth}";
            }

            if (start.Year == end.Year)
            {
                return $"{startDay} {startMonth} – {endDay} {endMonth}";
            }

            return $"{startDay} {startMonth} {start.Year} – {endDay} {endMonth} {end.Year}";
        }

        private static void AppendSectionSummary(
            List<string> parts,
            WeeklyBriefSection section
        )
        {
            if (!section.HasData)
            {
                return;
            }

            var summary = section.Summary?.Trim();
            if (!string.IsNullOrEmpty(summary))
            {
                parts.Add(summary);
            }
        }
    }
}
