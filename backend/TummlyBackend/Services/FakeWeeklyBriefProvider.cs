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
        }

        public void SucceedWith(WeeklyBriefBody body)
        {
            _mode = Mode.SucceedWith;
            _throwOnGenerate = null;
            _nextBody = body;
        }

        public void Fail(bool retryable = true)
        {
            _mode = Mode.Fail;
            _throwOnGenerate = null;
            _failRetryable = retryable;
            _nextBody = null;
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

            return Task.FromResult<WeeklyBriefProviderResult>(
                new WeeklyBriefProviderResult.Succeeded(body)
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
    }
}
