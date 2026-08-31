namespace TummlyBackend.DTOs.Locations
{
    public sealed class LocationsListQuery
    {
        public required int RestaurantId { get; init; }

        public required IReadOnlyList<int> LocationIds { get; init; }

        public string? Q { get; init; }

        public string[] Lifecycle { get; init; } = [];

        public string[] Setup { get; init; } = [];

        public string[] City { get; init; } = [];

        public string Sort { get; init; } = "name-asc";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 10;
    }
}
