using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackRecoveryOffersService
    {
        /// <summary>
        /// Atomically records guest-response + catalog Offer issue from durable
        /// Recovery attach. Does not write new FeedbackRecoveryOffer rows.
        /// Does not change workflow status. Returns null when Feedback is missing.
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
