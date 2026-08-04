using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackCloseOutsService
    {
        /// <summary>
        /// Atomically closes out Feedback: status → Resolved, status-change fact,
        /// optional internal note (Other), close-out fact.
        /// Returns null when Feedback is missing.
        /// Throws ArgumentException for invalid intent/reason/note rules.
        /// Throws InvalidOperationException when already Resolved or author missing.
        /// </summary>
        Task<FeedbackCloseOutResultDto?> CloseOutAsync(
            int feedbackId,
            int authorUserId,
            FeedbackCloseOutIntent intent,
            FeedbackCloseOutReason reason,
            string? noteBody,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Lists close-out facts for a Feedback newest-first (capped).
        /// </summary>
        Task<IReadOnlyList<FeedbackCloseOutItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
