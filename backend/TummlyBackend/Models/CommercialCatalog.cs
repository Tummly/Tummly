using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TummlyBackend.Models
{
    public static class CommercialCatalogStatuses
    {
        public const string Draft = "Draft";

        public const string Published = "Published";

        public const string Superseded = "Superseded";
    }

    public static class CommercialCustomUnitRateKeys
    {
        public const string Location = "location";

        public const string TeamUser = "team_user";

        public const string QrPlacement = "qr_placement";

        public const string Offer = "offer";

        public const string EmailCredit = "email_credit";

        public const string SmsCredit = "sms_credit";

        public const string AiCredit = "ai_credit";

        public const string ExtraLocationBand2 = "extra_location_band_2";

        public const string ExtraLocationBand3 = "extra_location_band_3";

        public const string ExtraLocationBand4 = "extra_location_band_4";

        public const string ExtraLocationBand5 = "extra_location_band_5";

        public static readonly IReadOnlyList<string> All =
        [
            Location,
            TeamUser,
            QrPlacement,
            Offer,
            EmailCredit,
            SmsCredit,
            AiCredit,
            ExtraLocationBand2,
            ExtraLocationBand3,
            ExtraLocationBand4,
            ExtraLocationBand5,
        ];
    }

    /// <summary>
    /// One commercial catalog version. At most one <see cref="CommercialCatalogStatuses.Published"/>.
    /// </summary>
    public class CommercialCatalogVersion
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Public pricebook-style id stamped on ledgers / accounts (e.g. TUMMLY-UK-GBP-…).
        /// </summary>
        [MaxLength(64)]
        public string PublicId { get; set; } = string.Empty;

        [MaxLength(32)]
        public string Status { get; set; } = CommercialCatalogStatuses.Draft;

        public DateTime? PublishedAtUtc { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int VatRateBps { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public int? PublishedByAdminUserId { get; set; }

        public ICollection<CommercialPlanRow> Plans { get; set; } = [];

        public ICollection<CommercialTopUpPackRow> TopUpPacks { get; set; } = [];

        public ICollection<CommercialExtraLocationRow> ExtraLocations { get; set; } =
            [];

        public ICollection<CommercialCustomUnitRateRow> CustomUnitRates { get; set; } =
            [];
    }

    public class CommercialPlanRow
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CatalogVersionId { get; set; }

        [ForeignKey(nameof(CatalogVersionId))]
        public CommercialCatalogVersion CatalogVersion { get; set; } = null!;

        /// <summary>Lowercase plan key: free, pilot, starter, growth, group.</summary>
        [MaxLength(32)]
        public string PlanKey { get; set; } = string.Empty;

        [MaxLength(32)]
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

        [MaxLength(128)]
        public string? LookupKeyMonthly { get; set; }

        [MaxLength(128)]
        public string? LookupKeyAnnual { get; set; }

        [MaxLength(128)]
        public string? RevolutPlanVariationIdMonthly { get; set; }

        [MaxLength(128)]
        public string? RevolutPlanVariationIdAnnual { get; set; }

        public int? SelfServeMaxLocations { get; set; }
    }

    public class CommercialTopUpPackRow
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CatalogVersionId { get; set; }

        [ForeignKey(nameof(CatalogVersionId))]
        public CommercialCatalogVersion CatalogVersion { get; set; } = null!;

        [MaxLength(16)]
        public string Channel { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int NetPence { get; set; }

        [MaxLength(128)]
        public string LookupKey { get; set; } = string.Empty;

        public bool ApprovalRequired { get; set; }
    }

    public class CommercialExtraLocationRow
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CatalogVersionId { get; set; }

        [ForeignKey(nameof(CatalogVersionId))]
        public CommercialCatalogVersion CatalogVersion { get; set; } = null!;

        public int MonthlyNetPence { get; set; }

        public int AnnualNetPence { get; set; }

        public int UsersAdded { get; set; }

        public int CreditsMonthlyAi { get; set; }

        public int CreditsMonthlyEmail { get; set; }

        public int CreditsMonthlySms { get; set; }

        [MaxLength(128)]
        public string? LookupKeyMonthly { get; set; }

        [MaxLength(128)]
        public string? LookupKeyAnnual { get; set; }

        [MaxLength(128)]
        public string? RevolutPlanVariationIdMonthly { get; set; }

        [MaxLength(128)]
        public string? RevolutPlanVariationIdAnnual { get; set; }
    }

    /// <summary>
    /// À-la-carte unit rate. <see cref="UnitQuantity"/> natural units cost
    /// <see cref="MonthlyNetPence"/> (e.g. 1000 email credits → 120 pence).
    /// </summary>
    public class CommercialCustomUnitRateRow
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CatalogVersionId { get; set; }

        [ForeignKey(nameof(CatalogVersionId))]
        public CommercialCatalogVersion CatalogVersion { get; set; } = null!;

        [MaxLength(32)]
        public string RateKey { get; set; } = string.Empty;

        public int UnitQuantity { get; set; } = 1;

        public int MonthlyNetPence { get; set; }

        public int? AnnualNetPence { get; set; }
    }
}
