namespace TummlyBackend.Billing.PlanEntitlements
{
    /// <summary>
    /// Workspace-wide active-QR pool arithmetic (Commercial V4, 25 Oct 2026).
    /// One pool per Workspace, no per-Location limit:
    /// <c>pool = base plan allowance + 10 x additional paid Group Locations
    /// + purchased QR extras</c>.
    /// </summary>
    public static class WorkspaceQrPool
    {
        /// <summary>
        /// Commercial V4, 25 Oct 2026: each additional paid Group Location adds
        /// 10 active QR placements to the shared pool. This replaces the v3.0
        /// pack value <c>additional_group_location.active_qr_placements: 50</c>,
        /// which is not read.
        /// </summary>
        public const int AdditionalPaidLocationQrPlacements = 10;

        /// <summary>
        /// Pool size from its three V4 parts. Negative inputs count as zero.
        /// </summary>
        /// <param name="baseAllowance">
        /// Base plan allowance (pricebook <c>ActiveQrPlacementsPerLocation</c>
        /// read as the Workspace base: Free 0, Pilot 5, Starter 10, Growth 25,
        /// Group 50).
        /// </param>
        /// <param name="additionalPaidLocations">
        /// Paid Locations beyond the Locations the base plan includes.
        /// </param>
        /// <param name="purchasedExtras">Purchased extra active QR slots.</param>
        public static int Compute(
            int baseAllowance,
            int additionalPaidLocations,
            int purchasedExtras
        )
        {
            return checked(
                Math.Max(0, baseAllowance)
                + AdditionalPaidLocationQrPlacements
                    * Math.Max(0, additionalPaidLocations)
                + Math.Max(0, purchasedExtras)
            );
        }
    }
}
