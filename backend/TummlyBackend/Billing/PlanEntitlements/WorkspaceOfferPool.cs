namespace TummlyBackend.Billing.PlanEntitlements
{
    /// <summary>
    /// Workspace-wide active Offer allowance (spec §6, ADM-04-03).
    /// <c>allowance = base plan Offers + 3 x additional paid Group Locations</c>.
    /// </summary>
    public static class WorkspaceOfferPool
    {
        /// <summary>
        /// Spec §6: one Group Location ordinal 6-500 adds +3 active Offers.
        /// The count is the same one that drives the Group seat (+2) and QR (+10) rules.
        /// </summary>
        public const int AdditionalPaidLocationOffers = 3;

        /// <summary>
        /// Offer allowance from its two parts. Negative inputs count as zero.
        /// </summary>
        /// <param name="baseAllowance">
        /// Base plan allowance (pricebook <c>ActiveOffersAccount</c>; Group 25).
        /// </param>
        /// <param name="additionalPaidLocations">
        /// Paid Group Locations beyond the five the Group base includes.
        /// </param>
        public static int Compute(int baseAllowance, int additionalPaidLocations)
        {
            return checked(
                Math.Max(0, baseAllowance)
                + AdditionalPaidLocationOffers
                    * Math.Max(0, additionalPaidLocations)
            );
        }
    }
}
