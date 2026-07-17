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

        public sealed record Failed : FeedbackClassificationResult;
    }
}
