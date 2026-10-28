namespace TummlyBackend.DTOs.Shop
{
    public sealed class ShopInterestProductDto
    {
        public string ProductKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        /// <summary>
        /// True when this operator already registered interest for the Shop Location.
        /// </summary>
        public bool InterestRegistered { get; set; }
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

        /// <summary>
        /// Masked by default (for example <c>ja***@domain.com</c>). Never the raw address.
        /// </summary>
        public string UserEmailMasked { get; set; } = string.Empty;

        /// <summary>
        /// <see cref="ShopInterestRegistrationStates.Active"/> or
        /// <see cref="ShopInterestRegistrationStates.Withdrawn"/>.
        /// </summary>
        public string State { get; set; } = ShopInterestRegistrationStates.Active;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? WithdrawnAtUtc { get; set; }
    }

    public static class ShopInterestRegistrationStates
    {
        public const string Active = "Active";

        public const string Withdrawn = "Withdrawn";
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
