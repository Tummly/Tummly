namespace TummlyBackend.Interfaces
{
    public interface ILocationGuestDeleteService
    {
        Task<LocationGuestDeleteOutcome> DeleteAsync(
            int locationGuestId,
            int locationId,
            CancellationToken cancellationToken = default
        );
    }

    public enum LocationGuestDeleteStatus
    {
        Deleted,
        NotFound,
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
    }
}
