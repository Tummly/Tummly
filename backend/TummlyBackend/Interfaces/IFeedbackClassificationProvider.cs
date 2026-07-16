using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Fakeable provider that classifies a Feedback comment.
    /// Production path: Azure OpenAI Structured Outputs (mini-tier).
    /// </summary>
    public interface IFeedbackClassificationProvider
    {
        Task<FeedbackClassificationResult> ClassifyAsync(
            string comment,
            CancellationToken cancellationToken = default
        );
    }
}
