namespace TummlyBackend.DTOs.Shop
{
    public sealed class ShopInterestProductDto
    {
        public string ProductKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }

    public sealed class RegisterShopInterestRequest
    {
        public string ProductKey { get; set; } = string.Empty;

        public int? LocationId { get; set; }
    }

    public sealed class AdminShopInterestRegistrationDto
    {
        public int Id { get; set; }

        public string ProductKey { get; set; } = string.Empty;

        public string ProductTitle { get; set; } = string.Empty;

        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public int? LocationId { get; set; }

        public string? LocationName { get; set; }

        public int UserId { get; set; }

        public string UserFullName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class AdminShopInterestProductCountDto
    {
        public string ProductKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public sealed class AdminShopInterestListDto
    {
        public IReadOnlyList<AdminShopInterestRegistrationDto> Items { get; set; } =
            Array.Empty<AdminShopInterestRegistrationDto>();

        public IReadOnlyList<AdminShopInterestProductCountDto> CountsByProduct { get; set; } =
            Array.Empty<AdminShopInterestProductCountDto>();
    }
}
