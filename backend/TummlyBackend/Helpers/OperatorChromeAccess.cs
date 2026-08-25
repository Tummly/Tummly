using System.Security.Claims;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class OperatorChromeAccess
    {
        public static async Task<string> TeamPermissionsAsync(
            IRestaurantPermissionHelper permissions,
            ClaimsPrincipal user
        )
        {
            var view = await permissions.AuthorizeAsync(
                user,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.View
            );
            if (view.Status != RestaurantPermissionStatus.Allowed)
            {
                return "none";
            }

            var manage = await permissions.AuthorizeAsync(
                user,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            return manage.Status == RestaurantPermissionStatus.Allowed
                ? "manage"
                : "view";
        }
    }
}
