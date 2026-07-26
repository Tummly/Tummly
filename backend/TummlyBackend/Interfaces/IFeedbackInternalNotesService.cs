using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Interfaces
{
    public interface IFeedbackInternalNotesService
    {
        /// <summary>
        /// Creates a Feedback internal note. Returns null when Feedback is missing.
        /// Throws ArgumentException for invalid body.
        /// Throws InvalidOperationException when the author user is missing.
        /// </summary>
        Task<FeedbackInternalNoteItemDto?> CreateAsync(
            int feedbackId,
            int authorUserId,
            string body,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Lists notes for a Feedback newest-first (capped).
        /// </summary>
        Task<IReadOnlyList<FeedbackInternalNoteItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );
    }
}
