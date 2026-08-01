using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackWorkflowStatusChangesService
    {
        /// <summary>
        /// Records an operator workflow-status change fact. Returns null when Feedback is missing.
        /// Throws InvalidOperationException when the author user is missing.
        /// </summary>
        Task<FeedbackWorkflowStatusChangeItemDto?> RecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackWorkflowStatus fromStatus,
            FeedbackWorkflowStatus toStatus,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Lists workflow-status changes for a Feedback newest-first (capped).
        /// </summary>
        Task<IReadOnlyList<FeedbackWorkflowStatusChangeItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
