using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackRecoveryCompletionsService
    {
        /// <summary>
        /// One-click recovery completion: status → Resolved + status-change +
        /// recovery-completion fact (no close-out reasons).
        /// Returns null when Feedback is missing.
        /// Throws FeedbackAlreadyResolvedException when already Resolved.
        /// </summary>
        Task<CompleteFeedbackRecoveryResultDto?> CompleteAsync(
            int feedbackId,
            int authorUserId,
            FeedbackRecoveryIntent intent,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<FeedbackRecoveryCompletionItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
