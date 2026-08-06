using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackGuestResponsesService
    {
        /// <summary>
        /// Records a guest-response fact. Does not change workflow status.
        /// Email channel enqueues Guest response email delivery (Pending →
        /// Accepted via background retry). SMS stays fact-only.
        /// Returns null when Feedback is missing.
        /// </summary>
        Task<SendFeedbackGuestResponseResultDto?> SendAsync(
            int feedbackId,
            int authorUserId,
            FeedbackGuestResponseChannel channel,
            FeedbackRecoveryIntent intent,
            string? subject,
            string body,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<FeedbackGuestResponseItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
