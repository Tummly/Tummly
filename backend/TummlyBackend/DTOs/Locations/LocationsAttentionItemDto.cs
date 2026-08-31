namespace TummlyBackend.DTOs.Locations
{
    public sealed class LocationsAttentionItemDto
    {
        public string Id { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public IReadOnlyList<int> LocationIds { get; init; } = [];
    }
}
