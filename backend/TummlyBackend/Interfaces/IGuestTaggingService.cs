using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public sealed record GuestTagPickerItem(
        int Id,
        string Name,
        int GuestCount,
        bool AiSourced
    );

    public interface IGuestTaggingService
    {
        /// <summary>
        /// Idempotent operator create by normalized name
        /// (<c>AI-sourced=false</c>, <c>DetectedTagKey=null</c>).
        /// </summary>
        Task<GuestTag> CreateByNameAsync(
            int restaurantId,
            string name,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Restaurant catalog with per-tag Location Guest counts for
        /// the given location scope.
        /// </summary>
        Task<IReadOnlyList<GuestTagPickerItem>> ListForLocationScopeAsync(
            int restaurantId,
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Additive memberships only. Guests must belong to
        /// <paramref name="locationIds"/>; tags to <paramref name="restaurantId"/>.
        /// </summary>
        Task ApplyAdditiveAsync(
            int restaurantId,
            IReadOnlyList<int> locationIds,
            IReadOnlyList<int> locationGuestIds,
            IReadOnlyList<int> guestTagIds,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Current Guest tag memberships for the given Location Guests
        /// (must belong to <paramref name="locationIds"/>).
        /// </summary>
        Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetMembershipsForGuestsAsync(
            int restaurantId,
            IReadOnlyList<int> locationIds,
            IReadOnlyList<int> locationGuestIds,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// AI ensure: DetectedTagKey → normalized display label → create.
        /// Never flips AI-sourced / renames / stamps key on operator-created rows.
        /// </summary>
        Task<GuestTag> EnsureFromDetectedTagAsync(
            int restaurantId,
            DetectedTag detectedTag,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Union Succeeded Feedback Detected Tags onto its Location Guest.
        /// No-op when guest FK null, not Succeeded, or empty tag set.
        /// </summary>
        Task UnionDetectedTagsFromFeedbackAsync(
            Feedback feedback,
            CancellationToken cancellationToken = default
        );
    }
}
