namespace ProductDemoSeed;

internal static class DemoSeedConstants
{
    public const string OwnerEmail = "mavero.demo@qa-seed.tummly.invalid";
    public const string SeedEmailDomain = "qa-seed.tummly.invalid";
    public const string RestaurantName = "Mavero Kitchen";
    public const string LocationName = "Camden High Street";
    public const string LocationAddress = "Camden High Street, London";
    public const string LocationPostcode = "NW1";
    public const string LocationCity = "London";
    public const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";
    public const string OwnerFullName = "Mavero Demo Owner";

    /// <summary>Canonical Last-30-day anchors (spec §5–6).</summary>
    public const int TargetFeedback = 426;
    public const int TargetGuests = 302;
    public const int TargetMarketingEligible = 218;
    public const int TargetScans = 1842;
    public const int TargetFormOpens = 1516;
    public const int TargetNeedsAttention = 44;
    public const int TargetRecoveriesStarted = 38;
    public const int TargetRecoveriesCompleted = 31;
    public const int TargetAiActionsUsed = 64;
    public const string BrandLogoObjectKey = "brand-logos/mavero-kitchen-demo.jpg";
    public const string BrandLogoContentType = "image/jpeg";
    public const string StaffEmail = "mavero.staff@qa-seed.tummly.invalid";

    public static readonly DateTime AnchorWeekStartUtc =
        new(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);

    public static readonly DateTime SeedNowUtc =
        new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
}
