using TummlyBackend.DTOs.Feedback;

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
            string? confirmedInternalActionCategory = null,
            string? confirmedInternalActionNote = null,
            CancellationToken cancellationToken = default
        );
    }
}
