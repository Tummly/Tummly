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

        private Exception? _throwOnClassify;

        public void SucceedWith(
            FeedbackSentiment sentiment,
            params DetectedIssue[] detectedIssues
        )
        {
            _throwOnClassify = null;
            _nextResult = new FeedbackClassificationResult.Succeeded(
                sentiment,
                detectedIssues
            );
        }

        public void Fail()
        {
            _throwOnClassify = null;
            _nextResult = new FeedbackClassificationResult.Failed();
        }

        /// <summary>
        /// Next <see cref="ClassifyAsync"/> throws (escaped provider failure).
        /// </summary>
        public void ThrowOnClassify(Exception? exception = null)
        {
            _throwOnClassify =
                exception ?? new InvalidOperationException("Fake provider boom");
        }

        /// <summary>
        /// Returns Succeeded with a null issues list — exercises post-provider mapping.
        /// </summary>
        public void SucceedWithNullIssues()
        {
            _throwOnClassify = null;
            _nextResult = new FeedbackClassificationResult.Succeeded(
                FeedbackSentiment.Neutral,
                null!
            );
        }

        public Task<FeedbackClassificationResult> ClassifyAsync(
            string comment,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_throwOnClassify is not null)
            {
                throw _throwOnClassify;
            }

            return Task.FromResult(_nextResult);
        }
    }
}
