using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and local demos — no live Azure/OpenAI.
    /// </summary>
    public sealed class FakeFeedbackClassificationProvider
        : IFeedbackClassificationProvider
    {
        private FeedbackClassificationResult _nextResult =
            new FeedbackClassificationResult.Succeeded(
                FeedbackSentiment.Neutral,
                Array.Empty<DetectedIssue>()
            );

        public void SucceedWith(
            FeedbackSentiment sentiment,
            params DetectedIssue[] detectedIssues
        )
        {
            _nextResult = new FeedbackClassificationResult.Succeeded(
                sentiment,
                detectedIssues
            );
        }

        public void Fail()
        {
            _nextResult = new FeedbackClassificationResult.Failed();
        }

        public Task<FeedbackClassificationResult> ClassifyAsync(
            string comment,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_nextResult);
        }
    }
}
