namespace TummlyBackend.DTOs.Locations
{
    public abstract record AddOwnedLocationResult
    {
        public sealed record Created(
            int LocationId,
            PremisesDuplicateWarning? PremisesWarning = null
        ) : AddOwnedLocationResult;

        public sealed record CapReached(int Cap, int Current, string? Message = null)
            : AddOwnedLocationResult;

        public sealed record InvalidRequest(string Message) : AddOwnedLocationResult;

        public sealed record FailClosed : AddOwnedLocationResult;
    }
}
