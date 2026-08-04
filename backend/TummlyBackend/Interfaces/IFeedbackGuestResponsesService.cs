using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackGuestResponsesService
    {
        /// <summary>
        /// Records a guest-response fact. Does not change workflow status.
        /// Channel delivery is stubbed — fact persistence is the send outcome.
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
