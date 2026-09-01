namespace TummlyBackend.DTOs.Locations
{
    public sealed class LocationLifecycleCommand
    {
        public required int UserId { get; init; }

        public required int RestaurantId { get; init; }

        public required int LocationId { get; init; }
    }

    public enum LocationLifecycleResultKind
    {
        Ok,
        NotFound,
        InvalidTransition,
    }

    public sealed class LocationLifecycleResult
    {
        public LocationLifecycleResultKind Kind { get; init; }

        public string? Message { get; init; }

        public string? LifecycleStatus { get; init; }

        public static LocationLifecycleResult Ok(string lifecycleStatus) =>
            new()
            {
                Kind = LocationLifecycleResultKind.Ok,
                LifecycleStatus = lifecycleStatus,
            };

        public static LocationLifecycleResult NotFound(
            string message = "Location not found."
        ) =>
            new()
            {
                Kind = LocationLifecycleResultKind.NotFound,
                Message = message,
            };

        public static LocationLifecycleResult InvalidTransition(string message) =>
            new()
            {
                Kind = LocationLifecycleResultKind.InvalidTransition,
                Message = message,
            };
    }
}
