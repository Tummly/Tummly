using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Fakeable provider that classifies a Feedback comment.
    /// Phase 1 default is a fake; Azure Structured Outputs lands later.
    /// </summary>
    public interface IFeedbackClassificationProvider
    {
        Task<FeedbackClassificationResult> ClassifyAsync(
            string comment,
            CancellationToken cancellationToken = default
        );
    }
}
