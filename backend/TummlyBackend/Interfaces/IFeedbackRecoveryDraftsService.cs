using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackRecoveryDraftsService
    {
        /// <summary>
        /// Builds draft adapter inputs from Feedback (no raw contact) and
        /// returns provider output. Null when Feedback is missing.
        /// </summary>
        Task<PrepareFeedbackRecoveryDraftResultDto?> PrepareAsync(
            int feedbackId,
            string channel,
            string purpose,
            string tone,
            string? includeNotes,
            string mode,
            string? currentBody,
            string? currentSubject,
            FeedbackRecoveryOfferPayloadDto? confirmedOffer = null,
            CancellationToken cancellationToken = default
        );
    }
}
