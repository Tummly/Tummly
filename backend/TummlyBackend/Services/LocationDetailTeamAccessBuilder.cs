using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Location-scoped team access rows for Location detail Team access tab (PRD Q7).
    /// </summary>
    public static class LocationDetailTeamAccessBuilder
    {
        public static bool HasAccessToLocation(
            RestaurantMembership membership,
            int locationId
        )
        {
            if (membership.Status != MembershipStatus.Active)
            {
                return false;
            }

            if (membership.LocationScope == LocationScopeKind.AllLocations)
            {
                return membership.PermissionRole
                    is PermissionRoles.Owner
                        or PermissionRoles.Admin;
            }

            return MembershipLocationScope
                .ParseNamedIds(membership.NamedLocationIdsJson)
                .Contains(locationId);
        }

        public static List<LocationDetailTeamAccessRowDto> Build(
            IEnumerable<RestaurantMembership> scopedMemberships,
            int locationId,
            IReadOnlyDictionary<int, string> locationNamesById,
            IReadOnlyDictionary<int, DateTime> lastActiveByUserId
        )
        {
            return scopedMemberships
                .Select(row =>
                {
                    var named = MembershipLocationScope.ParseNamedIds(
                        row.NamedLocationIdsJson
                    );
                    DateTime? lastActiveAt = null;
                    if (lastActiveByUserId.TryGetValue(row.UserId, out var activeAt))
                    {
                        lastActiveAt = activeAt;
                    }

                    return new LocationDetailTeamAccessRowDto
                    {
                        MembershipId = row.Id,
                        UserId = row.UserId,
                        Name = row.User.FullName,
                        Role = row.PermissionRole,
                        AccessLabel = MembershipLocationScope.FormatAccessLabel(
                            row.LocationScope,
                            named,
                            locationNamesById
                        ),
                        LastActiveAt = lastActiveAt,
                    };
                })
                .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
