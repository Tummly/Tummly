using System.Globalization;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Builds Feedback-by-hour series from retrieved rows and grounds model
    /// chart proposals against that evidence (fail closed).
    /// </summary>
    public static class AssistantAnswerChartGrounding
    {
        public const string BarKind = "bar";

        public const string DefaultFeedbackHourlyTitle =
            "Feedback by hour";

        public const string DefaultXLabel = "Hour";

        public static IReadOnlyList<AssistantAnswerChartPoint> BuildFeedbackByHour(
            AssistantFeedbackEvidence feedback
        )
        {
            var counts = new int[24];
            foreach (var row in feedback.Rows)
            {
                counts[row.CreatedAt.Hour]++;
            }

            var points = new List<AssistantAnswerChartPoint>(24);
            for (var hour = 0; hour < 24; hour++)
            {
                points.Add(
                    new AssistantAnswerChartPoint(FormatHourLabel(hour), counts[hour])
                );
            }

            return points;
        }

        public static bool HasChartableSeries(
            IReadOnlyList<AssistantAnswerChartPoint> series
        )
            => series.Count > 0 && series.Any(point => point.Value > 0);

        /// <summary>
        /// Keep a model chart only when every series point matches evidence.
        /// Otherwise rebuild from evidence when chartable, else null.
        /// </summary>
        public static AssistantAnswerChart? Ground(
            AssistantAnswerChart? proposed,
            AssistantRetrievedEvidence evidence
        )
        {
            var allowed = BuildFeedbackByHour(evidence.Feedback);
            if (!HasChartableSeries(allowed))
            {
                return null;
            }

            if (proposed is null)
            {
                return null;
            }

            var title = AssistantOperatorProse.Scrub(proposed.Title).Trim();
            if (title.Length == 0)
            {
                title = DefaultFeedbackHourlyTitle;
            }

            var xLabel = string.IsNullOrWhiteSpace(proposed.XLabel)
                ? DefaultXLabel
                : AssistantOperatorProse.Scrub(proposed.XLabel).Trim();
            if (xLabel.Length == 0)
            {
                xLabel = DefaultXLabel;
            }

            if (SeriesMatches(proposed.Series, allowed))
            {
                return new AssistantAnswerChart(
                    title,
                    BarKind,
                    xLabel,
                    allowed
                );
            }

            // Model invented buckets — rebuild from evidence, keep title.
            return FromEvidence(allowed, title, xLabel);
        }

        /// <summary>
        /// Attach a Feedback-by-hour chart when grounded evidence has counts and
        /// the ask is a Feedback summarise / list (model may leave chart null).
        /// </summary>
        public static AssistantAnswerChart? AutoAttachForFeedbackAsk(
            AssistantAnswerChart? groundedOrNull,
            AssistantRetrievedEvidence evidence,
            string userMessage
        )
        {
            if (groundedOrNull is not null)
            {
                return groundedOrNull;
            }

            if (!LooksLikeFeedbackPresentationAsk(userMessage))
            {
                return null;
            }

            var allowed = BuildFeedbackByHour(evidence.Feedback);
            if (!HasChartableSeries(allowed))
            {
                return null;
            }

            return FromEvidence(allowed, DefaultFeedbackHourlyTitle, DefaultXLabel);
        }

        private static AssistantAnswerChart FromEvidence(
            IReadOnlyList<AssistantAnswerChartPoint> series,
            string title,
            string xLabel
        )
            => new(title, BarKind, xLabel, series);

        private static bool LooksLikeFeedbackPresentationAsk(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return false;
            }

            var ask = AssistantAskIntent.ClassifyGrounded(userMessage);
            if (ask is AssistantGroundedAsk.Summarise
                or AssistantGroundedAsk.ListFeedback)
            {
                return lower.Contains("feedback", StringComparison.Ordinal)
                    || lower.Contains("summar", StringComparison.Ordinal)
                    || ask == AssistantGroundedAsk.ListFeedback;
            }

            return lower.Contains("feedback", StringComparison.Ordinal);
        }

        public static bool SeriesMatches(
            IReadOnlyList<AssistantAnswerChartPoint> proposed,
            IReadOnlyList<AssistantAnswerChartPoint> allowed
        )
        {
            if (proposed.Count != allowed.Count)
            {
                return false;
            }

            for (var i = 0; i < allowed.Count; i++)
            {
                if (!string.Equals(
                        proposed[i].Label,
                        allowed[i].Label,
                        StringComparison.Ordinal
                    )
                    || proposed[i].Value != allowed[i].Value)
                {
                    return false;
                }
            }

            return true;
        }

        public static string FormatHourLabel(int hour)
        {
            var period = hour < 12 ? "AM" : "PM";
            var display = hour % 12;
            if (display == 0)
            {
                display = 12;
            }

            return string.Create(
                CultureInfo.InvariantCulture,
                $"{display} {period}"
            );
        }
    }
}
