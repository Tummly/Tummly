using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Pluggable provider that drafts an editable guest-response message.
    /// Production path: Azure OpenAI chat completions (separate from ClassifyAsync).
    /// </summary>
    public interface IFeedbackRecoveryDraftProvider
    {
        Task<FeedbackRecoveryDraftResult> DraftAsync(
            FeedbackRecoveryDraftInput input,
            CancellationToken cancellationToken = default
        );
    }
}
