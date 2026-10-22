namespace TummlyBackend.DTOs.BillingCredits
{
    /// <summary>
    /// Plan limit with current usage (ticket 23 / entitlement audit).
    /// </summary>
    public sealed class PlanEntitlementLimitDto
    {
        public int Cap { get; set; }

        public int Current { get; set; }

        public bool AtCap { get; set; }

        /// <summary>False when pricebook or billing account is unavailable.</summary>
        public bool Available { get; set; } = true;
    }

    public sealed class PlanEntitlementsAccountSnapshotDto
    {
        public PlanEntitlementLimitDto Locations { get; set; } = new();

        public PlanEntitlementLimitDto TeamMembers { get; set; } = new();

        public PlanEntitlementLimitDto ActiveOffers { get; set; } = new();

        /// <summary>
        /// Custom v2 QR pack pool (account-wide). Cap = quantities.qrPlacementsAccountExtra;
        /// Current = Σ max(0, active_l − per-Location QR cap). Null when not Custom / no pool.
        /// </summary>
        public PlanEntitlementLimitDto? ActiveQrPlacementsAccountExtra { get; set; }
    }

    public sealed class PlanEntitlementsLocationSnapshotDto
    {
        public PlanEntitlementLimitDto ActiveQrPlacements { get; set; } = new();

        public PlanEntitlementLimitDto PublishedGuestForms { get; set; } = new();

        public PlanEntitlementLimitDto DraftGuestForms { get; set; } = new();
    }

    public sealed class PlanEntitlementsSnapshotDto
    {
        public PlanEntitlementsAccountSnapshotDto Account { get; set; } = new();

        public PlanEntitlementsLocationSnapshotDto? Location { get; set; }
    }
}
