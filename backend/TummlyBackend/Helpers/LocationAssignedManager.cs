using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Settings Locations "Location manager" column — derived from the active
    /// permission role Location Manager NamedList membership for that Owned
    /// location (one manager per location).
    /// </summary>
    public static class LocationAssignedManager
    {
        public const string ConflictMessage =
            "This location already has a Location Manager.";

        public sealed record Assigned(int UserId, string FullName);

        /// <summary>
        /// Map Owned location id → assigned Location Manager. When legacy data
        /// has more than one, keep the lowest UserId.
        /// </summary>
        public static IReadOnlyDictionary<int, Assigned> MapByLocationId(
            IEnumerable<RestaurantMembership> activeLocationManagers
        )
        {
            var map = new Dictionary<int, Assigned>();
            foreach (var membership in activeLocationManagers)
            {
                if (
                    membership.Status != MembershipStatus.Active
                    || membership.PermissionRole
                        != PermissionRoles.LocationManager
                    || membership.User == null
                )
                {
                    continue;
                }

                var name = membership.User.FullName?.Trim() ?? string.Empty;
                if (name.Length == 0)
                {
                    continue;
                }

                foreach (
                    var locationId in MembershipLocationScope.ParseNamedIds(
                        membership.NamedLocationIdsJson
                    )
                )
                {
                    if (
                        map.TryGetValue(locationId, out var existing)
                        && existing.UserId <= membership.UserId
                    )
                    {
                        continue;
                    }

                    map[locationId] = new Assigned(membership.UserId, name);
                }
            }

            return map;
        }

        /// <summary>
        /// Reject when another active Location Manager membership or a live
        /// Location Manager invite already covers any of <paramref name="locationIds"/>.
        /// </summary>
        public static async Task<string?> ValidateExclusiveAsync(
            ApplicationDbContext context,
            int restaurantId,
            IReadOnlyList<int> locationIds,
            int? excludeMembershipId = null,
            int? excludeInvitationId = null
        )
        {
            if (locationIds.Count == 0)
            {
                return null;
            }

            var wanted = locationIds.Distinct().ToHashSet();

            var managers = await context.RestaurantMemberships
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                    && row.PermissionRole == PermissionRoles.LocationManager
                    && (
                        excludeMembershipId == null
                        || row.Id != excludeMembershipId.Value
                    )
                )
                .Select(row => row.NamedLocationIdsJson)
                .ToListAsync();

            foreach (var json in managers)
            {
                if (
                    MembershipLocationScope
                        .ParseNamedIds(json)
                        .Any(wanted.Contains)
                )
                {
                    return ConflictMessage;
                }
            }

            var now = DateTime.UtcNow;
            var invites = await context.TeamInvitations
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.ExpiresAt > now
                    && row.PermissionRole == PermissionRoles.LocationManager
                    && (
                        excludeInvitationId == null
                        || row.Id != excludeInvitationId.Value
                    )
                )
                .Select(row => row.NamedLocationIdsJson)
                .ToListAsync();

            foreach (var json in invites)
            {
                if (
                    MembershipLocationScope
                        .ParseNamedIds(json)
                        .Any(wanted.Contains)
                )
                {
                    return ConflictMessage;
                }
            }

            return null;
        }
    }
}
