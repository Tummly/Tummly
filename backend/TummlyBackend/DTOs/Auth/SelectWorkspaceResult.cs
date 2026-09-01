namespace TummlyBackend.DTOs.Auth
{
    public sealed class SelectWorkspaceResult
    {
        public int RestaurantId { get; init; }

        public int LocationId { get; init; }

        public string AccountType { get; init; } = string.Empty;
    }
}
