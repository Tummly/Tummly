using System.Security.Claims;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class OperatorChromeAccess
    {
        /// <summary>
        /// View/manage chrome for one Operator Area. Explicit "none" when View
        /// is denied. Scoped and Manage both count as at least view.
        /// </summary>
        public static async Task<string> ForAreaAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user,
            string areaId
        )
        {
            var view = await permissions.AuthorizeAsync(
                user,
                areaId,
                PermissionLevel.View
            );
            if (view.Status != RestaurantPermissionStatus.Allowed)
            {
                return "none";
            }

            var manage = await permissions.AuthorizeAsync(
                user,
                areaId,
                PermissionLevel.Manage
            );
            return manage.Status == RestaurantPermissionStatus.Allowed
                ? "manage"
                : "view";
        }

        public static Task<string> TeamPermissionsAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.TeamPermissions);

        public static Task<string> BillingCreditsAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.BillingCredits);

        public static Task<string> OffersAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.Offers);

        public static Task<string> PrivacyConsentAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.PrivacyConsent);

        public static Task<string> GuestsAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.Guests);

        public static Task<string> CaptureAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.Capture);

        public static Task<string> FeedbackAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.Feedback);

        public static Task<string> CampaignsAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.Campaigns);

        public static Task<string> ReportsAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.Reports);

        public static Task<string> TummlyShopAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.TummlyShop);

        public static Task<string> LocationsAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.Locations);

        public static Task<string> AccountWorkspaceAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        ) => ForAreaAsync(permissions, user, OperatorAreaIds.AccountWorkspace);
    }
}
