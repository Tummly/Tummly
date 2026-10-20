namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminCommercialCatalogVersionDto
    {
        public Guid Id { get; set; }

        public string PublicId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? PublishedAtUtc { get; set; }

        public string? Notes { get; set; }

        public int VatRateBps { get; set; }

        public List<AdminCommercialPlanDto> Plans { get; set; } = [];

        public List<AdminCommercialTopUpPackDto> TopUpPacks { get; set; } = [];

        public AdminCommercialExtraLocationDto? ExtraLocation { get; set; }

        public List<AdminCommercialCustomUnitRateDto> CustomUnitRates { get; set; } =
            [];
    }

    public sealed class AdminCommercialPlanDto
    {
        public Guid Id { get; set; }

        public string PlanKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public int MonthlyNetPence { get; set; }

        public int AnnualNetPence { get; set; }

        public int IncludedLocations { get; set; }

        public int IncludedTeamMembers { get; set; }

        public int ActiveOffersAccount { get; set; }

        public int ActiveQrPlacementsPerLocation { get; set; }

        public int PublishedGuestFormsPerLocation { get; set; }

        public int DraftGuestFormsPerLocation { get; set; }

        public int? CreditsMonthlyAi { get; set; }

        public int? CreditsMonthlyEmail { get; set; }

        public int? CreditsMonthlySms { get; set; }

        public int? CreditsOneTimeAi { get; set; }

        public int? CreditsOneTimeEmail { get; set; }

        public int? CreditsOneTimeSms { get; set; }

        public string? LookupKeyMonthly { get; set; }

        public string? LookupKeyAnnual { get; set; }

        public string? RevolutPlanVariationIdMonthly { get; set; }

        public string? RevolutPlanVariationIdAnnual { get; set; }

        public int? SelfServeMaxLocations { get; set; }
    }

    public sealed class AdminCommercialTopUpPackDto
    {
        public Guid Id { get; set; }

        public string Channel { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int NetPence { get; set; }

        public string LookupKey { get; set; } = string.Empty;

        public bool ApprovalRequired { get; set; }
    }

    public sealed class AdminCommercialExtraLocationDto
    {
        public Guid Id { get; set; }

        public int MonthlyNetPence { get; set; }

        public int AnnualNetPence { get; set; }

        public int UsersAdded { get; set; }

        public int CreditsMonthlyAi { get; set; }

        public int CreditsMonthlyEmail { get; set; }

        public int CreditsMonthlySms { get; set; }

        public string? LookupKeyMonthly { get; set; }

        public string? LookupKeyAnnual { get; set; }

        public string? RevolutPlanVariationIdMonthly { get; set; }

        public string? RevolutPlanVariationIdAnnual { get; set; }
    }

    public sealed class AdminCommercialCustomUnitRateDto
    {
        public Guid Id { get; set; }

        public string RateKey { get; set; } = string.Empty;

        public int UnitQuantity { get; set; }

        public int MonthlyNetPence { get; set; }

        public int? AnnualNetPence { get; set; }
    }

    public sealed class AdminCommercialCatalogSaveDraftRequest
    {
        public string? Notes { get; set; }

        public int? VatRateBps { get; set; }

        public List<AdminCommercialPlanDto>? Plans { get; set; }

        public List<AdminCommercialTopUpPackDto>? TopUpPacks { get; set; }

        public AdminCommercialExtraLocationDto? ExtraLocation { get; set; }

        public List<AdminCommercialCustomUnitRateDto>? CustomUnitRates { get; set; }
    }

    public sealed class AdminCommercialCatalogPublishRequest
    {
        public string? Notes { get; set; }

        /// <summary>New public id for the published version. Empty = auto.</summary>
        public string? PublicId { get; set; }
    }

    public sealed class PublicCustomUnitRatesDto
    {
        public string CatalogPublicId { get; set; } = string.Empty;

        public int VatRateBps { get; set; }

        public int MaxLocations { get; set; } = 500;

        public List<AdminCommercialCustomUnitRateDto> UnitRates { get; set; } = [];
    }
}
