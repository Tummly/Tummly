using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestNotesService
    {
        /// <summary>
        /// Returns null when the Location Guest is missing for the Owned location.
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
    }
}
