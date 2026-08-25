using System.Text.Json;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class MembershipLocationScope
    {
        public static IReadOnlyList<int> ParseNamedIds(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                var ids = JsonSerializer.Deserialize<int[]>(json);
                return ids ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        public static string SerializeNamedIds(IEnumerable<int> ids)
        {
            return JsonSerializer.Serialize(ids.Distinct().OrderBy(id => id).ToArray());
        }

        public static string? Validate(
            string permissionRole,
            LocationScopeKind scope,
            IReadOnlyList<int> namedIds
        )
        {
            if (permissionRole == PermissionRoles.Owner)
            {
                if (
                    scope != LocationScopeKind.AllLocations
                    || namedIds.Count > 0
                )
                {
                    return "Owner location scope must be All locations.";
                }

                return null;
            }

            if (
                permissionRole == PermissionRoles.AreaManager
                || permissionRole == PermissionRoles.LocationManager
            )
            {
                if (scope != LocationScopeKind.NamedList || namedIds.Count == 0)
                {
                    return "This role requires a named list of at least one Owned location.";
                }

                return null;
            }

            if (scope == LocationScopeKind.AllLocations)
            {
                return namedIds.Count > 0
                    ? "All locations must not carry a named list."
                    : null;
            }

            if (namedIds.Count == 0)
            {
                return "A named Location scope must list at least one Owned location.";
            }

            return null;
        }
    }
}
