namespace TummlyBackend.DTOs.Shop
{
    public sealed class ShopOrdersListQuery
    {
        public int RestaurantId { get; init; }

        public int ShellLocationId { get; init; }

        public required IReadOnlyList<int> LocationIds { get; init; }

        public string? Q { get; init; }

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public string Sort { get; init; } = "newest";

        public IReadOnlyList<string> FulfilmentStatus { get; init; } =
            Array.Empty<string>();

        public IReadOnlyList<string> PaymentStatus { get; init; } =
            Array.Empty<string>();

        public IReadOnlyList<string> MaterialType { get; init; } =
            Array.Empty<string>();

        public string? OrderDatePreset { get; init; }

        public DateTime? OrderDateFrom { get; init; }

        public DateTime? OrderDateTo { get; init; }

        public int UtcOffsetMinutes { get; init; }
    }
}
