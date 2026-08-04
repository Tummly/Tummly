using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackClassificationCorrectionsService
    {
        /// <summary>
        /// Records an operator sentiment correction fact. Returns null when Feedback is missing.
        /// Throws InvalidOperationException when the author user is missing.
        /// </summary>
        Task<FeedbackClassificationCorrectionItemDto?> RecordAsync(
            int feedbackId,
            int authorUserId,
            FeedbackSentiment fromSentiment,
            FeedbackSentiment toSentiment,
            FeedbackClassificationCorrectionReason reason,
            string? noteBody,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Lists classification corrections for a Feedback newest-first (capped).
        /// </summary>
        Task<IReadOnlyList<FeedbackClassificationCorrectionItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
