namespace TummlyBackend.Models
{
    /// <summary>
    /// Result of classifying a Feedback comment via the classification provider.
    /// </summary>
    public abstract record FeedbackClassificationResult
    {
        private FeedbackClassificationResult()
        {
        }

        public sealed record Succeeded(
            FeedbackSentiment Sentiment,
            IReadOnlyList<DetectedIssue> DetectedIssues
        ) : FeedbackClassificationResult;

        /// <param name="Retryable">
        /// When true, delayed auto-requeue may reopen to Pending (ADR-0012).
        /// </param>
        public sealed record Failed(bool Retryable = true)
            : FeedbackClassificationResult;
    }
}
