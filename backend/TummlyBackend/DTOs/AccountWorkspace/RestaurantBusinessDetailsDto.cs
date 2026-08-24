namespace TummlyBackend.DTOs.AccountWorkspace
{
    public sealed class RestaurantBusinessDetailsDto
    {
        public string? LegalStructure { get; set; }

        public string? LegalBusinessName { get; set; }

        public string? TradingName { get; set; }

        public string? CompanyNumber { get; set; }

        public string? VatNumber { get; set; }

        public string? CountryOfRegistration { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? TownCity { get; set; }

        public string? County { get; set; }

        public string? Postcode { get; set; }

        public string? Country { get; set; }
    }

    public sealed class UpdateBusinessDetailsRequest
    {
        public string? LegalStructure { get; set; }

        public string? LegalBusinessName { get; set; }

        public string? TradingName { get; set; }

        /// <summary>
        /// UI-only. When true, persist sets TradingName equal to LegalBusinessName.
        /// </summary>
        public bool? SameAsLegalBusinessName { get; set; }

        public string? CompanyNumber { get; set; }

        public string? VatNumber { get; set; }

        public string? CountryOfRegistration { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? TownCity { get; set; }

        public string? County { get; set; }

        public string? Postcode { get; set; }

        public string? Country { get; set; }
    }
}
