using Microsoft.AspNetCore.Http;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Minimum Offers Area permission for catalog / redeem HTTP routes.
    /// Trust boundary for write — FE <c>offersAccess</c> only hides chrome.
    /// </summary>
    public static class OffersAreaPermission
    {
        /// <summary>
        /// Redeem Check/Mark: Scoped. Other GET: View. Mutations: Manage.
        /// </summary>
        public static PermissionLevel MinimumForHttpRequest(
            string? path,
            string method
        )
        {
            var safePath = path ?? string.Empty;
            if (safePath.Contains(
                    "/redeem",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return PermissionLevel.Scoped;
            }

            if (HttpMethods.IsGet(method))
            {
                return PermissionLevel.View;
            }

            return PermissionLevel.Manage;
        }
    }
}
