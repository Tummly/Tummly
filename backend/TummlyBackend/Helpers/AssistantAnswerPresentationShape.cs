using System.Globalization;
using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shapes grounded live answers into presentation slots (metrics, short
    /// note, chart, CTAs) and trims action prose from the body.
    /// </summary>
    public static class AssistantAnswerPresentationShape
    {
        public const int MaxBodyChars = 900;

        public const int MaxNextSteps = 3;

        private static readonly Regex ActionProseLine = new(
            @"^\s*(-|\*|•|\d+[.)])?\s*"
            + @"(you can |please |to |try to )?"
            + @"(view feedback|open feedback|prepare recovery|review campaign|"
            + @"edit draft|see the feedback|check the feedback).*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        private static readonly Regex FeedbackReferenceCode = new(
            @"\bFDB-\d+\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        private static readonly Regex FeedbackReferencePrefix = new(
            @"^\s*((?:-|\*|•|\d+[.)])\s*)?"
            + @"FDB-\d+\s*[—–\-:]?\s*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        public sealed record ShapedAnswer(
            string Body,
            AssistantMessagePresentation? Presentation,
            IReadOnlyList<AssistantActionDto> Actions
        );

        public static ShapedAnswer ShapeLiveAnswer(
            string userMessage,
            string? title,
            string body,
            IReadOnlyList<AssistantActionDto> actions,
            AssistantAnswerChart? chart,
            AssistantRecommendedNextStep? recommendedNextStep,
            AssistantRetrievedEvidence evidence
        )
        {
            var cleanedBody = StripActionProse(body);
            cleanedBody = ScrubFeedbackReferences(cleanedBody);
            cleanedBody = ScrubTrailingEllipsis(cleanedBody);
            // Do not hard-cut the body — the side panel collapses long answers
            // with Show details. Mid-sentence caps produced "...." in chat.

            var chartAttached = AssistantAnswerChartGrounding.AutoAttachForFeedbackAsk(
                chart,
                evidence,
                userMessage
            );

            IReadOnlyList<AssistantAnswerMetric>? metrics = null;
            string? shortNote = null;
            if (LooksLikeFeedbackSummarise(userMessage))
            {
                metrics = BuildFeedbackMetrics(evidence.Feedback);
                shortNote = FirstProseSentence(cleanedBody)
                    ?? (string.IsNullOrWhiteSpace(title) ? null : title.Trim());
            }
            else if (LooksLikeSimpleCountAsk(userMessage))
            {
                cleanedBody = PreferOneSentenceCount(cleanedBody, evidence);
                shortNote = null;
                metrics = null;
                chartAttached = null;
            }

            var ctaActions = PreferCtaActions(actions, evidence, userMessage);
            var next = EnsureRecommendedNextStep(
                recommendedNextStep,
                ctaActions,
                userMessage
            );

            var presentation = BuildPresentation(
                chartAttached,
                next,
                metrics,
                shortNote
            );

            return new ShapedAnswer(cleanedBody, presentation, ctaActions);
        }

        public static ShapedAnswer ShapeAdvisoryAnswer(
            string body,
            AssistantAnswerChart? chart,
            RestaurantContextSnapshot snapshot
        )
        {
            var cleanedBody = StripActionProse(body);
            cleanedBody = ScrubFeedbackReferences(cleanedBody);
            cleanedBody = AssistantAdvisoryReasonValidate.ScrubTechnicalProse(cleanedBody);
            cleanedBody = AssistantOperatorProse.Scrub(cleanedBody);
            cleanedBody = ScrubTrailingEllipsis(cleanedBody);
            var metrics = BuildAdvisoryMetrics(snapshot);
            var shortNote = FirstProseSentence(cleanedBody);
            // Cap advisory recommendation lines to MaxNextSteps in the body.
            cleanedBody = CapAdvisoryNextSteps(cleanedBody);

            var presentation = BuildPresentation(
                chart,
                null,
                metrics,
                shortNote
            );

            return new ShapedAnswer(cleanedBody, presentation, []);
        }

        public static string StripActionProse(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return body;
            }

            var lines = body.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n');
            var kept = new List<string>(lines.Length);
            foreach (var line in lines)
            {
                if (ActionProseLine.IsMatch(line))
                {
                    continue;
                }

                kept.Add(line);
            }

            return string.Join("\n", kept).Trim();
        }

        public static string ScrubFeedbackReferences(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return body;
            }

            var lines = body.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = FeedbackReferencePrefix.Replace(lines[i], "$1");
                lines[i] = FeedbackReferenceCode.Replace(lines[i], string.Empty);
                lines[i] = Regex.Replace(
                    lines[i],
                    @"\s{2,}",
                    " ",
                    RegexOptions.CultureInvariant
                ).TrimEnd();
            }

            return string.Join("\n", lines).Trim();
        }

        /// <summary>
        /// Removes dangling "..." / "…" left by models or older length caps.
        /// </summary>
        public static string ScrubTrailingEllipsis(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return body;
            }

            var lines = body.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = Regex.Replace(
                    lines[i],
                    @"(\s*\.{2,}|…)+\s*$",
                    string.Empty,
                    RegexOptions.CultureInvariant
                ).TrimEnd();
            }

            return string.Join("\n", lines).Trim();
        }

        public static string CapBodyLength(string body)
        {
            // Kept for tests / callers; shaping no longer truncates mid-answer.
            if (string.IsNullOrEmpty(body) || body.Length <= MaxBodyChars)
            {
                return body;
            }

            var cut = body.AsSpan(0, MaxBodyChars);
            var lastBreak = cut.LastIndexOf('\n');
            if (lastBreak < MaxBodyChars / 2)
            {
                lastBreak = cut.LastIndexOf(' ');
            }

            if (lastBreak <= 0)
            {
                lastBreak = MaxBodyChars;
            }

            return body.AsSpan(0, lastBreak).TrimEnd().ToString();
        }

        private static AssistantMessagePresentation? BuildPresentation(
            AssistantAnswerChart? chart,
            AssistantRecommendedNextStep? next,
            IReadOnlyList<AssistantAnswerMetric>? metrics,
            string? shortNote
        )
        {
            if (chart is null
                && next is null
                && (metrics is null || metrics.Count == 0)
                && string.IsNullOrWhiteSpace(shortNote))
            {
                return null;
            }

            return new AssistantMessagePresentation(
                chart,
                next,
                metrics is { Count: > 0 } ? metrics : null,
                string.IsNullOrWhiteSpace(shortNote) ? null : shortNote.Trim()
            );
        }

        private static IReadOnlyList<AssistantAnswerMetric> BuildFeedbackMetrics(
            AssistantFeedbackEvidence feedback
        )
        {
            var metrics = new List<AssistantAnswerMetric>
            {
                new(
                    "Feedback",
                    feedback.TotalCount.ToString("N0", CultureInfo.InvariantCulture)
                ),
            };

            if (feedback.NeedsAttention > 0)
            {
                metrics.Add(
                    new(
                        "Needs attention",
                        feedback.NeedsAttention.ToString(
                            "N0",
                            CultureInfo.InvariantCulture
                        )
                    )
                );
            }

            if (feedback.SucceededNegative > 0)
            {
                metrics.Add(
                    new(
                        "Negative",
                        feedback.SucceededNegative.ToString(
                            "N0",
                            CultureInfo.InvariantCulture
                        )
                    )
                );
            }

            return metrics;
        }

        private static IReadOnlyList<AssistantAnswerMetric> BuildAdvisoryMetrics(
            RestaurantContextSnapshot snapshot
        )
        {
            var metrics = new List<AssistantAnswerMetric>();
            AddMetric(metrics, "Covers", snapshot.Account.Covers.Current);
            AddMetric(metrics, "New guests", snapshot.Guests.NewGuestCount.Current);
            if (snapshot.Feedback.Flagged.Count > 0)
            {
                metrics.Add(
                    new(
                        "Flagged",
                        snapshot.Feedback.Flagged.Count.ToString(
                            "N0",
                            CultureInfo.InvariantCulture
                        )
                    )
                );
            }

            AddMetric(
                metrics,
                "Funnel completes",
                snapshot.Capture.FunnelCompleteCount.Current
            );
            return metrics.Take(6).ToList();
        }

        private static void AddMetric(
            List<AssistantAnswerMetric> metrics,
            string label,
            decimal value
        )
        {
            if (value <= 0m)
            {
                return;
            }

            metrics.Add(
                new(
                    label,
                    value.ToString("0.##", CultureInfo.InvariantCulture)
                )
            );
        }

        private static IReadOnlyList<AssistantActionDto> PreferCtaActions(
            IReadOnlyList<AssistantActionDto> actions,
            AssistantRetrievedEvidence evidence,
            string userMessage
        )
        {
            if (actions.Count > 0)
            {
                return actions;
            }

            if (!LooksLikeFeedbackSummarise(userMessage)
                || evidence.Feedback.TotalCount <= 0)
            {
                return actions;
            }

            return
            [
                new AssistantActionDto
                {
                    Type = "view-feedback-set",
                    Label = "View Feedback",
                },
            ];
        }

        private static AssistantRecommendedNextStep? EnsureRecommendedNextStep(
            AssistantRecommendedNextStep? existing,
            IReadOnlyList<AssistantActionDto> actions,
            string userMessage
        )
        {
            if (existing is not null)
            {
                return existing;
            }

            if (!LooksLikeFeedbackSummarise(userMessage))
            {
                return null;
            }

            var view = actions.FirstOrDefault(action =>
                string.Equals(action.Type, "view-feedback-set", StringComparison.Ordinal)
            );
            var prepare = actions.FirstOrDefault(action =>
                string.Equals(action.Type, "prepare-recovery", StringComparison.Ordinal)
            );
            if (view is null && prepare is null)
            {
                return null;
            }

            return new AssistantRecommendedNextStep(
                "Review the Feedback set or prepare recovery responses.",
                view,
                prepare
            );
        }

        private static bool LooksLikeFeedbackSummarise(string userMessage)
        {
            var ask = AssistantAskIntent.ClassifyGrounded(userMessage);
            var lower = userMessage.Trim().ToLowerInvariant();
            return ask is AssistantGroundedAsk.Summarise or AssistantGroundedAsk.ListFeedback
                && (lower.Contains("feedback", StringComparison.Ordinal)
                    || ask == AssistantGroundedAsk.ListFeedback);
        }

        private static bool LooksLikeSimpleCountAsk(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            return lower.Contains("how many", StringComparison.Ordinal)
                || lower.StartsWith("count ", StringComparison.Ordinal);
        }

        private static string PreferOneSentenceCount(
            string body,
            AssistantRetrievedEvidence evidence
        )
        {
            var first = FirstProseSentence(body);
            if (first is not null && first.Length <= 160)
            {
                return first;
            }

            if (!evidence.Guests.IsEmpty)
            {
                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"You have {evidence.Guests.TotalCount:N0} guests."
                );
            }

            if (!evidence.Feedback.IsEmpty)
            {
                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"You have {evidence.Feedback.TotalCount:N0} Feedback submissions."
                );
            }

            return CapBodyLength(body);
        }

        private static string? FirstProseSentence(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            foreach (var line in body.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0
                    || trimmed.StartsWith('#')
                    || trimmed.StartsWith('-')
                    || trimmed.StartsWith('*')
                    || Regex.IsMatch(trimmed, @"^\d+[.)]\s"))
                {
                    continue;
                }

                var end = trimmed.IndexOfAny(['.', '!', '?']);
                if (end > 0 && end < trimmed.Length - 1)
                {
                    return trimmed[..(end + 1)].Trim();
                }

                return trimmed;
            }

            return null;
        }

        private static string CapAdvisoryNextSteps(string body)
        {
            const string heading = "## Recommended next steps";
            var index = body.IndexOf(heading, StringComparison.Ordinal);
            if (index < 0)
            {
                return body;
            }

            var head = body[..(index + heading.Length)];
            var rest = body[(index + heading.Length)..];
            var lines = rest.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n');
            var kept = new List<string> { "" };
            var bullets = 0;
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
                {
                    if (bullets >= MaxNextSteps)
                    {
                        continue;
                    }

                    bullets++;
                }

                kept.Add(line);
            }

            return head + string.Join("\n", kept);
        }
    }
}
