namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Hard-deletes a <c>Location Guest</c> after Owned-location authz.
    /// See CONTEXT: <b>Location Guest delete</b>.
    /// </summary>
    /// <remarks>
    /// Outcomes: <see cref="LocationGuestDeleteStatus.Deleted"/> after success;
    /// <see cref="LocationGuestDeleteStatus.NotFound"/> when the location is
    /// missing or the Location Guest is not at that location (after ownership
    /// succeeded); <see cref="LocationGuestDeleteStatus.Forbidden"/> when the
    /// location exists but the user does not own it.
    /// Cascade: notes, Guest-tag memberships, and Location Guest activity
    /// events are removed. Feedback rows are unlinked
    /// (<c>LocationGuestId</c> null); Feedback PII snapshots remain.
    /// An orphan Master Guest with no remaining Location Guests is removed.
    /// Assistant quotes stay: Location Guest delete does not change stored
    /// Assistant messages. Snapshot Name and excerpt remain.
    /// </remarks>
    public interface ILocationGuestDeleteService
    {
        Task<LocationGuestDeleteOutcome> DeleteAsync(
            int userId,
            int locationGuestId,
            int locationId,
            CancellationToken cancellationToken = default
        );
    }

    public enum LocationGuestDeleteStatus
    {
        Deleted,
        NotFound,
        Forbidden,
    }

    public sealed class LocationGuestDeleteOutcome
    {
        public LocationGuestDeleteStatus Status { get; init; }

        public string? ErrorMessage { get; init; }

        public static LocationGuestDeleteOutcome Deleted() => new()
        {
            Status = LocationGuestDeleteStatus.Deleted,
        };

        public static LocationGuestDeleteOutcome NotFound() => new()
        {
            Status = LocationGuestDeleteStatus.NotFound,
            ErrorMessage = "Guest not found.",
        };

        public static LocationGuestDeleteOutcome LocationNotFound() => new()
        {
            Status = LocationGuestDeleteStatus.NotFound,
            ErrorMessage = "Location not found.",
        };

        public static LocationGuestDeleteOutcome Forbidden() => new()
        {
            Status = LocationGuestDeleteStatus.Forbidden,
            ErrorMessage = "You do not have access to this location.",
        };
    }
}
