namespace TummlyBackend.DTOs.Locations
{
    public sealed class ImportOwnedLocationsRequest
    {
        public List<AddOwnedLocationRequest> Rows { get; set; } = [];
    }
}
