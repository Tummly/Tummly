using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestNotesService
    {
        /// <summary>
        /// Lists non-deleted notes. Returns null when the Location Guest is missing
        /// for the Owned location.
        /// </summary>
        Task<GuestNotesListResponse?> ListAsync(
            int locationGuestId,
            int locationId,
            int limit,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Creates a note and emits note-added activity in the same unit of work.
        /// Resolves author display name from <paramref name="authorUserId"/>.
        /// Returns null when the Location Guest is missing for the Owned location.
        /// Throws ArgumentException for invalid body.
        /// Throws InvalidOperationException when the author user is missing.
        /// </summary>
        Task<GuestNoteItemDto?> CreateAsync(
            int locationGuestId,
            int locationId,
            int authorUserId,
            string body,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Updates a non-deleted note body in place (no activity row).
        /// Returns null when the guest, note, or Owned-location scope is missing,
        /// or when the note is already soft-deleted.
        /// Throws ArgumentException for invalid body.
        /// Throws InvalidOperationException when the editor user is missing.
        /// </summary>
        Task<GuestNoteItemDto?> UpdateAsync(
            int locationGuestId,
            int locationId,
            int noteId,
            int editorUserId,
            string body,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Soft-deletes a note and emits note-deleted activity in the same unit of work.
        /// Returns null when the guest, note, or Owned-location scope is missing,
        /// or when the note is already soft-deleted.
        /// Throws InvalidOperationException when the actor user is missing.
        /// </summary>
        Task<SoftDeleteGuestNoteResultDto?> SoftDeleteAsync(
            int locationGuestId,
            int locationId,
            int noteId,
            int actorUserId,
            CancellationToken cancellationToken = default
        );
    }
}
