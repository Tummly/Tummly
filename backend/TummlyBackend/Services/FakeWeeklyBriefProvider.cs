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

            var body =
                _mode == Mode.SucceedWith && _nextBody is not null
                    ? _nextBody
                    : FixtureFor(input.Metrics);
            var enrichment =
                _mode == Mode.SucceedWith
                    ? _nextEnrichment ?? FixtureEnrichmentFor(input.Metrics, body)
                    : FixtureEnrichmentFor(input.Metrics, body);

            return Task.FromResult<WeeklyBriefProviderResult>(
                new WeeklyBriefProviderResult.Succeeded(body, enrichment)
            );
        }

        /// <summary>
        /// Deterministic CI fixture from the metrics bag (no guest PII).
        /// </summary>
        public static WeeklyBriefBody FixtureFor(WeeklyBriefMetrics metrics)
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
                WatchNext:
                [
                    "Watch feedback Needs attention volume next week.",
                    "Keep an eye on offer claim-to-redemption rate.",
                ]
            );
        }

        /// <summary>
        /// Deterministic phase-2 enrichment fixture from metrics + body headline.
        /// </summary>
        public static WeeklyBriefEnrichment FixtureEnrichmentFor(
            WeeklyBriefMetrics metrics,
            WeeklyBriefBody body
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
                ActionWording: actionWording
            );
        }
    }
}
