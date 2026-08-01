using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class FeedbackWorkflowStatusMapping
    {
        public static string ToWire(FeedbackWorkflowStatus status)
            => status switch
            {
                FeedbackWorkflowStatus.New => "new",
                FeedbackWorkflowStatus.InProgress => "in_progress",
                FeedbackWorkflowStatus.Resolved => "resolved",
                _ => "new",
            };

        public static bool TryParseWire(
            string? wire,
            out FeedbackWorkflowStatus status
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "new":
                    status = FeedbackWorkflowStatus.New;
                    return true;
                case "in_progress":
                    status = FeedbackWorkflowStatus.InProgress;
                    return true;
                case "resolved":
                    status = FeedbackWorkflowStatus.Resolved;
                    return true;
                default:
                    status = default;
                    return false;
            }
        }

        /// <summary>
        /// Needs attention = Succeeded Negative sentiment ∧ workflow ≠ Resolved.
        /// </summary>
        public static bool NeedsAttention(Feedback feedback)
            => feedback.ClassificationStatus == ClassificationStatus.Succeeded
                && feedback.Sentiment == FeedbackSentiment.Negative
                && feedback.WorkflowStatus != FeedbackWorkflowStatus.Resolved;
    }
}
