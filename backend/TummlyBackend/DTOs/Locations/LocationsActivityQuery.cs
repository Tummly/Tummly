namespace TummlyBackend.DTOs.Locations
{
    public sealed class LocationsActivityQuery
    {
        public int RestaurantId { get; init; }

        public IReadOnlyList<int> LocationIds { get; init; } = [];
    }
}
