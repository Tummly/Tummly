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
        /// Lists non-deleted notes for a Feedback newest-first (capped).
        /// </summary>
        Task<IReadOnlyList<FeedbackInternalNoteItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Lists note activity facts including soft-deleted notes (for derived history).
        /// </summary>
        Task<IReadOnlyList<FeedbackInternalNoteActivityFactDto>> ListActivityFactsForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Updates a non-deleted note body in place (no activity row).
        /// Returns null when Feedback or note is missing, or when soft-deleted.
        /// Throws ArgumentException for invalid body.
        /// Throws InvalidOperationException when the editor user is missing.
        /// </summary>
        Task<FeedbackInternalNoteItemDto?> UpdateAsync(
            int feedbackId,
            int noteId,
            int editorUserId,
            string body,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Soft-deletes a note. Returns null when Feedback or note is missing,
        /// or when already soft-deleted.
        /// Throws InvalidOperationException when the actor user is missing.
        /// </summary>
        Task<SoftDeleteFeedbackInternalNoteResultDto?> SoftDeleteAsync(
            int feedbackId,
            int noteId,
            int actorUserId,
            CancellationToken cancellationToken = default
        );
    }
}
