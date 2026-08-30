namespace TummlyBackend.DTOs.Locations
{
    public abstract record AddOwnedLocationResult
    {
        public sealed record Created(int LocationId) : AddOwnedLocationResult;

        public sealed record CapReached(int Cap, int Current) : AddOwnedLocationResult;

        public sealed record FailClosed : AddOwnedLocationResult;
    }
}
