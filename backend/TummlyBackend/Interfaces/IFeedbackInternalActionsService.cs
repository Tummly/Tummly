using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackInternalActionsService
    {
        /// <summary>
        /// Records an internal-action fact. Does not change workflow status.
        /// Returns null when Feedback is missing.
        /// </summary>
        Task<RecordFeedbackInternalActionResultDto?> RecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackInternalActionCategory category,
            string note,
            FeedbackRecoveryIntent intent,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<FeedbackInternalActionItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
