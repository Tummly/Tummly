using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// One rule for the duplicate-premises review: while a Location is
    /// PendingReview or Rejected it cannot be activated, start a Pilot, get a
    /// complimentary Starter Kit, or hold active QR capacity. Every activation
    /// path asks this gate.
    /// </summary>
    public static class PremisesReviewGate
    {
        /// <summary>
        /// Generic on purpose: the operator is never told why, or where else
        /// the premises is registered.
        /// </summary>
        public const string BlockedMessage =
            "Saved as Draft for review. This location cannot be used until the review is complete.";

        public const string PilotRefusedCode = "premises_under_review";

        public static bool Blocks(PremisesReviewStatus status) =>
            status is PremisesReviewStatus.PendingReview
                or PremisesReviewStatus.Rejected;

        public static bool Blocks(RestaurantLocation location) =>
            Blocks(location.PremisesReviewStatus);

        public static Task<bool> LocationIsBlockedAsync(
            ApplicationDbContext context,
            int locationId,
            CancellationToken cancellationToken = default
        ) =>
            context.RestaurantLocations
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == locationId
                        && (
                            row.PremisesReviewStatus == PremisesReviewStatus.PendingReview
                            || row.PremisesReviewStatus == PremisesReviewStatus.Rejected
                        ),
                    cancellationToken
                );

        /// <summary>
        /// True when the Workspace has a Location still waiting on, or refused
        /// by, the premises review. A Pilot is per Workspace, so it waits too.
        /// </summary>
        public static Task<bool> WorkspaceHasBlockedLocationAsync(
            ApplicationDbContext context,
            int restaurantId,
            CancellationToken cancellationToken = default
        ) =>
            context.RestaurantLocations
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && (
                            row.PremisesReviewStatus == PremisesReviewStatus.PendingReview
                            || row.PremisesReviewStatus == PremisesReviewStatus.Rejected
                        ),
                    cancellationToken
                );

        /// <summary>Wire value for operator Location rows. Null when no review applies.</summary>
        public static string? ToOperatorWire(PremisesReviewStatus status) =>
            status switch
            {
                PremisesReviewStatus.PendingReview => "pending_review",
                PremisesReviewStatus.Rejected => "rejected",
                _ => null,
            };
    }
}
