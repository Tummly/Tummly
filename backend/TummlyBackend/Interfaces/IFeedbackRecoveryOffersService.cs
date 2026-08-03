using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackRecoveryOffersService
    {
        /// <summary>
        /// Atomically records guest-response + recovery-offer facts and generates
        /// a unique redemption code. Does not change workflow status.
        /// Returns null when Feedback is missing.
        /// </summary>
        Task<SendAndIssueFeedbackRecoveryOfferResultDto?> SendAndIssueAsync(
            int feedbackId,
            int authorUserId,
            SendAndIssueFeedbackRecoveryOfferRequest request,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<FeedbackRecoveryOfferItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
