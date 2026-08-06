using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackRespondAndRecordService
    {
        /// <summary>
        /// Atomically records guest-response + internal-action facts.
        /// Does not change workflow status. Email channel enqueues Guest
        /// response email delivery; SMS stays fact-only.
        /// Returns null when Feedback is missing.
        /// </summary>
        Task<RespondAndRecordInternalActionResultDto?> SendAndRecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackGuestResponseChannel channel,
            FeedbackInternalActionCategory category,
            string note,
            string? subject,
            string body,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken = default
        );
    }
}
