using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackGuestPreviewSendTestService
    {
        /// <summary>
        /// Sends the current Guest preview draft as a Guest response email to the
        /// signed-in operator. Does not create a guest-response fact and does not
        /// message the guest. When <paramref name="offer"/> is set, includes the
        /// offer block with a sample code only (no live Recovery offer). Failures
        /// propagate synchronously (no retry queue). Returns null when Feedback
        /// is missing.
        /// </summary>
        Task<bool?> SendAsync(
            int feedbackId,
            int operatorUserId,
            string? subject,
            string body,
            GuestPreviewTestOfferDto? offer = null,
            CancellationToken cancellationToken = default
        );
    }
}
