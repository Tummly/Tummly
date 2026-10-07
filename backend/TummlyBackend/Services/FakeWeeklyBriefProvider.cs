using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and CI — no live Azure/OpenAI.
    /// Default mode returns a deterministic brief shaped from aggregate metrics.
    /// </summary>
    public sealed class FakeWeeklyBriefProvider : IWeeklyBriefProvider
    {
        private enum Mode
        {
            DefaultFixtures,
            SucceedWith,
            Fail,
            Throw,
        }

        private Mode _mode = Mode.DefaultFixtures;
        private WeeklyBriefBody? _nextBody;
        private WeeklyBriefEnrichment? _nextEnrichment;
        private bool _failRetryable = true;
        private Exception? _throwOnGenerate;
        private WeeklyBriefProviderInput? _lastInput;
        private int _callCount;

        public WeeklyBriefProviderInput? LastInput => _lastInput;

        public int CallCount => _callCount;

        public void ResetCallCount()
        {
            _callCount = 0;
            _lastInput = null;
        }

        public void UseDefaultFixtures()
        {
            _mode = Mode.DefaultFixtures;
            _throwOnGenerate = null;
            _nextBody = null;
            _nextEnrichment = null;
        }

        public void SucceedWith(
            WeeklyBriefBody body,
            WeeklyBriefEnrichment? enrichment = null
        )
        {
            _mode = Mode.SucceedWith;
            _throwOnGenerate = null;
            _nextBody = body;
            _nextEnrichment = enrichment;
        }

        public void Fail(bool retryable = true)
        {
            _mode = Mode.Fail;
            _throwOnGenerate = null;
            _failRetryable = retryable;
            _nextBody = null;
            _nextEnrichment = null;
        }

        public void ThrowOnGenerate(Exception? exception = null)
        {
            _mode = Mode.Throw;
            _throwOnGenerate =
                exception
                ?? new InvalidOperationException("Fake weekly brief boom");
        }

        public Task<WeeklyBriefProviderResult> GenerateAsync(
            WeeklyBriefProviderInput input,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            _callCount++;
            _lastInput = input;

            if (_mode == Mode.Throw && _throwOnGenerate is not null)
            {
                throw _throwOnGenerate;
            }

            if (_mode == Mode.Fail)
            {
                return Task.FromResult<WeeklyBriefProviderResult>(
                    new WeeklyBriefProviderResult.Failed(_failRetryable)
                );
            }

            var bag = input.InsightCandidates;
            var body =
                _mode == Mode.SucceedWith && _nextBody is not null
                    ? _nextBody
                    : FixtureFor(input.Metrics, bag);
            var enrichment =
                _mode == Mode.SucceedWith
                    ? _nextEnrichment
                        ?? FixtureEnrichmentFor(input.Metrics, body, bag)
                    : FixtureEnrichmentFor(input.Metrics, body, bag);

            return Task.FromResult<WeeklyBriefProviderResult>(
                new WeeklyBriefProviderResult.Succeeded(body, enrichment)
            );
        }

        /// <summary>
        /// Deterministic CI fixture from the metrics bag (no guest PII).
        /// </summary>
        public static WeeklyBriefBody FixtureFor(
            WeeklyBriefMetrics metrics,
            WeeklyBriefInsightCandidateBag? insightCandidates = null
        )
        {
            var captureHasData =
                metrics.GuestsJoined > 0 || metrics.QrScanEvents > 0;
            var feedbackHasData = metrics.FeedbackCount > 0;
            var offersHasData =
                metrics.ActiveOffers > 0
                || metrics.ClaimsInWeek > 0
                || metrics.RedemptionsInWeek > 0;
            var campaignsHasData =
                metrics.CampaignsSentInWeek > 0
                || metrics.CampaignRecipientsReached > 0;

            return new WeeklyBriefBody(
                Headline: captureHasData || feedbackHasData
                    ? "Steady week across capture and feedback."
                    : "Quiet week — little guest activity.",
                Capture: new WeeklyBriefSection(
                    captureHasData,
                    captureHasData
                        ? $"{metrics.GuestsJoined} guests joined; {metrics.QrScanEvents} QR scans."
                        : WeeklyBriefStructuredOutput.EmptyCaptureSummary,
                    EchoedCounts: null
                ),
                Feedback: new WeeklyBriefSection(
                    feedbackHasData,
                    feedbackHasData
                        ? $"{metrics.FeedbackCount} feedback submissions this week."
                        : WeeklyBriefStructuredOutput.EmptyFeedbackSummary,
                    EchoedCounts: null
                ),
                Offers: new WeeklyBriefSection(
                    offersHasData,
                    offersHasData
                        ? $"{metrics.ClaimsInWeek} claims and {metrics.RedemptionsInWeek} redemptions."
                        : WeeklyBriefStructuredOutput.EmptyOffersSummary,
                    EchoedCounts: null
                ),
                Campaigns: new WeeklyBriefSection(
                    campaignsHasData,
                    campaignsHasData
                        ? $"{metrics.CampaignsSentInWeek} campaigns reached {metrics.CampaignRecipientsReached} recipients."
                        : WeeklyBriefStructuredOutput.EmptyCampaignsSummary,
                    EchoedCounts: null
                ),
                WatchNext: FixtureWatchNext(metrics, insightCandidates)
            );
        }

        /// <summary>
        /// Deterministic phase-2 enrichment fixture from metrics + body headline.
        /// </summary>
        public static WeeklyBriefEnrichment FixtureEnrichmentFor(
            WeeklyBriefMetrics metrics,
            WeeklyBriefBody body,
            WeeklyBriefInsightCandidateBag? insightCandidates = null
        )
        {
            var executiveSummary =
                $"{body.Headline} Guests joined: {metrics.GuestsJoined}; "
                + $"feedback: {metrics.FeedbackCount}; "
                + $"unsubscribes: {metrics.UnsubscribesInWeek}.";

            WeeklyBriefEnrichmentFeedbackSummary? feedbackSummary = null;
            if (metrics.FeedbackCount > 0 || metrics.NeedsAttentionCount > 0)
            {
                var tagBit = metrics.DetectedTagCounts.Count > 0
                    ? $" Top themes: {string.Join(", ", metrics.DetectedTagCounts.Keys.Take(2))}."
                    : string.Empty;
                feedbackSummary = new WeeklyBriefEnrichmentFeedbackSummary(
                    Text:
                        $"{metrics.FeedbackCount} private feedback messages this week."
                        + (metrics.NeedsAttentionCount > 0
                            ? $" {metrics.NeedsAttentionCount} may need follow-up."
                            : string.Empty)
                        + tagBit,
                    Subtitle: "Based on private feedback submitted this week."
                );
            }

            // actionWording only when a control-signal kind fires (NeedsAttention gate).
            var actionWording = new List<WeeklyBriefEnrichmentActionWording>();
            if (metrics.NeedsAttentionCount > 0)
            {
                actionWording.Add(
                    new WeeklyBriefEnrichmentActionWording(
                        WeeklyBriefEnrichmentActionKinds.FeedbackNeedsAttention,
                        $"Follow up with {metrics.NeedsAttentionCount} guests",
                        "These guests shared contact details and may need a response."
                    )
                );
            }

            return new WeeklyBriefEnrichment(
                ExecutiveSummary: executiveSummary,
                FeedbackSummary: feedbackSummary,
                ActionWording: actionWording,
                InsightNarratives: FixtureNarratives(insightCandidates),
                InsightCandidates: insightCandidates?.Candidates
            );
        }

        /// <summary>
        /// Legacy fixed filler strings — must never appear on metrics-shaped /
        /// generate-path fixtures (lock 05 / KOL-RPTA-004).
        /// </summary>
        public const string LegacyFillerWatchNeedsAttention =
            "Watch feedback Needs attention volume next week.";

        public const string LegacyFillerWatchOfferRate =
            "Keep an eye on offer claim-to-redemption rate.";

        private static IReadOnlyList<string> FixtureWatchNext(
            WeeklyBriefMetrics metrics,
            WeeklyBriefInsightCandidateBag? bag
        )
        {
            // Null bag: metrics-shaped only (no fixed unrelated filler).
            if (bag is null)
            {
                if (metrics.NeedsAttentionCount > 0)
                {
                    return ["Watch Needs attention volume next week."];
                }

                return [];
            }

            if (WeeklyBriefInsightNarrativeValidation.IsSoleNoMaterialChange(bag))
            {
                return [];
            }

            var lines = new List<string>();
            foreach (var candidate in bag.Candidates.Take(3))
            {
                lines.Add(
                    candidate.Type switch
                    {
                        WeeklyBriefInsightCandidates.TypeControlSignal
                            when candidate.ActionKind
                                == WeeklyBriefInsightCandidates.ActionFeedbackNeedsAttention
                            => "Watch Needs attention volume next week.",
                        WeeklyBriefInsightCandidates.TypeControlSignal
                            or WeeklyBriefInsightCandidates.TypeFunnelDrop
                            => "Watch the flagged control signal next week.",
                        WeeklyBriefInsightCandidates.TypeMeaningfulChange
                            => "Watch the material metric change next week.",
                        WeeklyBriefInsightCandidates.TypeEmergingTheme
                            => "Watch the emerging feedback theme next week.",
                        WeeklyBriefInsightCandidates.TypeDataQualityIssue
                            => "Watch data coverage improve next week.",
                        WeeklyBriefInsightCandidates.TypeSustainedTrend
                            => "Watch the sustained metric trend next week.",
                        _ => "Watch the week’s insight candidate next week.",
                    }
                );
            }

            return lines.Count > 0
                ? lines
                : ["Watch the week’s insight candidate next week."];
        }

        private static IReadOnlyList<WeeklyBriefInsightNarrative> FixtureNarratives(
            WeeklyBriefInsightCandidateBag? bag
        )
        {
            if (bag is null || bag.Candidates.Count == 0)
            {
                return [];
            }

            var narratives = new List<WeeklyBriefInsightNarrative>(bag.Candidates.Count);
            foreach (var candidate in bag.Candidates)
            {
                var (observation, interpretation, recommendation) =
                    FixtureNarrativeCopy(candidate);
                narratives.Add(
                    new WeeklyBriefInsightNarrative(
                        candidate.Id,
                        observation,
                        interpretation,
                        recommendation
                    )
                );
            }

            return narratives;
        }

        private static (
            string Observation,
            string Interpretation,
            string? Recommendation
        ) FixtureNarrativeCopy(WeeklyBriefInsightCandidate candidate)
        {
            if (
                string.Equals(
                    candidate.Type,
                    WeeklyBriefInsightCandidates.TypeNoMaterialChange,
                    StringComparison.Ordinal
                )
            )
            {
                return (
                    "No insight candidate cleared thresholds this week.",
                    "Treat the week as stable for planning.",
                    null
                );
            }

            if (
                string.Equals(
                    candidate.Type,
                    WeeklyBriefInsightCandidates.TypeControlSignal,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    candidate.Type,
                    WeeklyBriefInsightCandidates.TypeFunnelDrop,
                    StringComparison.Ordinal
                )
            )
            {
                var count = SnapshotInt(candidate, "needsAttentionCount")
                    ?? SnapshotInt(candidate, "count")
                    ?? SnapshotInt(candidate, "scans");
                var observation = count is int n
                    ? $"Control signal cleared with value {n}."
                    : "Control signal cleared for this week.";
                return (
                    observation,
                    "This coincides with an actionable operational risk.",
                    "Open the matching recommended action."
                );
            }

            if (
                string.Equals(
                    candidate.Type,
                    WeeklyBriefInsightCandidates.TypeMeaningfulChange,
                    StringComparison.Ordinal
                )
            )
            {
                var current = SnapshotInt(candidate, "current");
                var prior = SnapshotInt(candidate, "prior");
                var observation =
                    current is int c && prior is int p
                        ? $"Metric moved from {p} to {c}."
                        : "A material week-over-week change cleared.";
                return (
                    observation,
                    "The change may be related to guest activity shifts.",
                    null
                );
            }

            if (
                string.Equals(
                    candidate.Type,
                    WeeklyBriefInsightCandidates.TypeDataQualityIssue,
                    StringComparison.Ordinal
                )
            )
            {
                return (
                    "Activity coverage is limited this week.",
                    "Treat patterns as directional until more domains have data.",
                    null
                );
            }

            return (
                "An insight candidate cleared for this week.",
                "Review the candidate evidence before acting.",
                null
            );
        }

        private static int? SnapshotInt(
            WeeklyBriefInsightCandidate candidate,
            string key
        )
        {
            if (!candidate.Evidence.Snapshot.TryGetValue(key, out var value))
            {
                return null;
            }

            return value switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)Math.Round(d, MidpointRounding.AwayFromZero),
                _ => null,
            };
        }
    }
}
