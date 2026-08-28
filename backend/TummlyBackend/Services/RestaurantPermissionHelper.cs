using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class RestaurantPermissionHelper : IRestaurantPermissionHelper
    {
        private readonly ApplicationDbContext _context;

        public RestaurantPermissionHelper(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RestaurantPermissionDecision> AuthorizeAsync(
            ClaimsPrincipal user,
            string areaId,
            PermissionLevel minimum
        )
        {
            var identified = await IdentifyAsync(user);
            if (identified.Deny != null)
            {
                return identified.Deny;
            }

            var access = await ResolveRestaurantAccessAsync(
                identified.UserId,
                identified.User!
            );
            if (access == null)
            {
                return RestaurantPermissionDecision.Deny();
            }

            return EvaluateArea(
                access,
                areaId,
                minimum,
                denyLocation: false
            );
        }

        public async Task<RestaurantPermissionDecision> AuthorizeLocationAsync(
            ClaimsPrincipal user,
            string areaId,
            PermissionLevel minimum,
            int locationId
        )
        {
            var identified = await IdentifyAsync(user);
            if (identified.Deny != null)
            {
                return identified.Deny;
            }

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(row => row.Restaurant)
                .FirstOrDefaultAsync(row => row.Id == locationId);

            if (location == null)
            {
                return RestaurantPermissionDecision.NotFoundLocation();
            }

            var access = await ResolveRestaurantAccessAsync(
                identified.UserId,
                identified.User!,
                location.RestaurantId
            );
            if (access == null)
            {
                return RestaurantPermissionDecision.DenyLocation();
            }

            var area = EvaluateArea(
                access,
                areaId,
                minimum,
                denyLocation: true
            );
            if (area.Status != RestaurantPermissionStatus.Allowed)
            {
                return area;
            }

            if (!access.LocationIds.Contains(locationId))
            {
                return RestaurantPermissionDecision.DenyLocation();
            }

            return RestaurantPermissionDecision.AllowLocation(
                access.RestaurantId,
                location,
                access.LocationIds
            );
        }

        public async Task<RestaurantPermissionDecision> AuthorizeLocationSetAsync(
            ClaimsPrincipal user,
            string areaId,
            PermissionLevel minimum
        )
        {
            var identified = await IdentifyAsync(user);
            if (identified.Deny != null)
            {
                return identified.Deny;
            }

            var access = await ResolveRestaurantAccessAsync(
                identified.UserId,
                identified.User!
            );
            if (access == null)
            {
                return RestaurantPermissionDecision.Deny();
            }

            var area = EvaluateArea(
                access,
                areaId,
                minimum,
                denyLocation: false
            );
            if (area.Status != RestaurantPermissionStatus.Allowed)
            {
                return area;
            }

            return RestaurantPermissionDecision.AllowSet(
                access.RestaurantId,
                access.LocationIds
            );
        }

        public async Task<RestaurantPermissionDecision> AuthorizeUserAsync(
            int userId,
            string areaId,
            PermissionLevel minimum
        )
        {
            var operatorUser = await LoadOperatorUserAsync(userId);
            var access = await ResolveRestaurantAccessAsync(
                userId,
                operatorUser
            );
            if (access == null)
            {
                return RestaurantPermissionDecision.Deny();
            }

            return EvaluateArea(
                access,
                areaId,
                minimum,
                denyLocation: false
            );
        }

        public async Task<RestaurantPermissionDecision> AuthorizeLocationForUserAsync(
            int userId,
            string areaId,
            PermissionLevel minimum,
            int locationId
        )
        {
            var operatorUser = await LoadOperatorUserAsync(userId);
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(row => row.Restaurant)
                .FirstOrDefaultAsync(row => row.Id == locationId);

            if (location == null)
            {
                return RestaurantPermissionDecision.NotFoundLocation();
            }

            var access = await ResolveRestaurantAccessAsync(
                userId,
                operatorUser,
                location.RestaurantId
            );
            if (access == null)
            {
                return RestaurantPermissionDecision.DenyLocation();
            }

            var area = EvaluateArea(
                access,
                areaId,
                minimum,
                denyLocation: true
            );
            if (area.Status != RestaurantPermissionStatus.Allowed)
            {
                return area;
            }

            if (!access.LocationIds.Contains(locationId))
            {
                return RestaurantPermissionDecision.DenyLocation();
            }

            return RestaurantPermissionDecision.AllowLocation(
                access.RestaurantId,
                location,
                access.LocationIds
            );
        }

        public async Task<RestaurantPermissionDecision> AuthorizeNamedLocationIdsAsync(
            IReadOnlyList<int> allowedLocationIds,
            int[] namedLocationIds
        )
        {
            var distinct = namedLocationIds.Distinct().ToList();
            if (distinct.Count == 0)
            {
                return RestaurantPermissionDecision.AllowSet(
                    0,
                    allowedLocationIds
                );
            }

            var found = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => distinct.Contains(row.Id))
                .Select(row => row.Id)
                .ToListAsync();

            if (found.Count != distinct.Count)
            {
                return RestaurantPermissionDecision.NotFoundLocation();
            }

            if (distinct.Any(id => !allowedLocationIds.Contains(id)))
            {
                return RestaurantPermissionDecision.DenyLocation();
            }

            return RestaurantPermissionDecision.AllowSet(
                0,
                allowedLocationIds
            );
        }

        private async Task<(
            int UserId,
            User? User,
            RestaurantPermissionDecision? Deny
        )> IdentifyAsync(ClaimsPrincipal user)
        {
            var role = user.FindFirstValue(ClaimTypes.Role);
            if (role is "Admin" or "Support")
            {
                return (0, null, RestaurantPermissionDecision.Deny());
            }

            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out var userId)
            )
            {
                return (0, null, RestaurantPermissionDecision.Deny());
            }

            var operatorUser = await LoadOperatorUserAsync(userId);

            if (operatorUser == null)
            {
                return (0, null, RestaurantPermissionDecision.Deny());
            }

            return (userId, operatorUser, null);
        }

        private Task<User?> LoadOperatorUserAsync(int userId)
        {
            return _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == userId);
        }

        private async Task<RestaurantAccess?> ResolveRestaurantAccessAsync(
            int userId,
            User? operatorUser,
            int? requiredRestaurantId = null
        )
        {
            var active = await _context.RestaurantMemberships
                .AsNoTracking()
                .Where(membership =>
                    membership.UserId == userId
                    && membership.Status == MembershipStatus.Active
                )
                .ToListAsync();

            int? restaurantId = requiredRestaurantId;
            RestaurantMembership? membership = null;

            if (requiredRestaurantId != null)
            {
                membership = active.FirstOrDefault(row =>
                    row.RestaurantId == requiredRestaurantId.Value
                );
                if (membership == null)
                {
                    var owned = await _context.Restaurants
                        .AsNoTracking()
                        .AnyAsync(row =>
                            row.Id == requiredRestaurantId.Value
                            && row.OwnerUserId == userId
                        );
                    if (!owned)
                    {
                        return null;
                    }
                }
            }
            else
            {
                restaurantId = ResolveRestaurantId(
                    operatorUser,
                    active.Select(row => row.RestaurantId).ToList()
                );
                if (restaurantId == null)
                {
                    var owned = await _context.Restaurants
                        .AsNoTracking()
                        .Where(row => row.OwnerUserId == userId)
                        .Select(row => row.Id)
                        .ToListAsync();

                    restaurantId = ResolveRestaurantId(operatorUser, owned);
                }

                if (restaurantId == null)
                {
                    return null;
                }

                membership = active.FirstOrDefault(row =>
                    row.RestaurantId == restaurantId.Value
                );
            }

            if (membership != null && NamedListIsEmpty(membership))
            {
                return RestaurantAccess.ForEmptyNamedList(restaurantId!.Value);
            }

            var locationIds = await ListLiveLocationIdsAsync(
                restaurantId!.Value,
                membership
            );
            var adminOverrides = await LoadAdminOverridesAsync(restaurantId.Value);

            var ownerGate = await OwnerGateMessageAsync(
                restaurantId.Value,
                userId
            );

            return new RestaurantAccess(
                restaurantId.Value,
                membership,
                locationIds,
                false,
                adminOverrides,
                ownerGate
            );
        }

        private async Task<
            IReadOnlyDictionary<string, PermissionLevel>
        > LoadAdminOverridesAsync(int restaurantId)
        {
            return await _context.RestaurantAdminPermissionCells
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .ToDictionaryAsync(row => row.AreaId, row => row.Level);
        }

        public const string OwnerPendingWaitMessage =
            "This workspace is waiting for the Account owner to activate.";

        private async Task<string?> OwnerGateMessageAsync(
            int restaurantId,
            int actorUserId
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null || restaurant.OwnerUserId == actorUserId)
            {
                return null;
            }

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);
            if (owner == null)
            {
                return null;
            }

            var subject = ActivationSubject.FromUser(owner);
            if (ActivationState.RequiresActivation(subject))
            {
                return OwnerPendingWaitMessage;
            }

            return null;
        }

        private async Task<IReadOnlyList<int>> ListLiveLocationIdsAsync(
            int restaurantId,
            RestaurantMembership? membership
        )
        {
            var live = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.Id)
                .ToListAsync();

            if (
                membership == null
                || membership.LocationScope != LocationScopeKind.NamedList
            )
            {
                return live;
            }

            var named = MembershipLocationScope
                .ParseNamedIds(membership.NamedLocationIdsJson)
                .ToHashSet();

            return live.Where(named.Contains).ToList();
        }

        private static RestaurantPermissionDecision EvaluateArea(
            RestaurantAccess access,
            string areaId,
            PermissionLevel minimum,
            bool denyLocation
        )
        {
            if (access.OwnerGateMessage != null)
            {
                return RestaurantPermissionDecision.DenyWith(access.OwnerGateMessage);
            }

            if (access.EmptyNamedList)
            {
                return denyLocation
                    ? RestaurantPermissionDecision.DenyLocation()
                    : RestaurantPermissionDecision.Deny();
            }

            var permissionRole =
                access.Membership?.PermissionRole ?? PermissionRoles.Owner;
            var cell = DefaultPermissionMatrix.LevelFor(
                permissionRole,
                areaId,
                access.AdminOverrides
            );

            if (!DefaultPermissionMatrix.Meets(cell, minimum))
            {
                return denyLocation
                    ? RestaurantPermissionDecision.DenyLocation()
                    : RestaurantPermissionDecision.Deny();
            }

            return RestaurantPermissionDecision.AllowSet(
                access.RestaurantId,
                access.LocationIds
            );
        }

        private static bool NamedListIsEmpty(RestaurantMembership membership)
        {
            if (membership.LocationScope != LocationScopeKind.NamedList)
            {
                return false;
            }

            return MembershipLocationScope
                .ParseNamedIds(membership.NamedLocationIdsJson)
                .Count == 0;
        }

        private static int? ResolveRestaurantId(
            User? user,
            IReadOnlyList<int> restaurantIds
        )
        {
            if (restaurantIds.Count == 0)
            {
                return null;
            }

            if (
                user?.SelectedRestaurantId is int selected
                && restaurantIds.Contains(selected)
            )
            {
                return selected;
            }

            return restaurantIds.OrderBy(id => id).First();
        }

        private sealed record RestaurantAccess(
            int RestaurantId,
            RestaurantMembership? Membership,
            IReadOnlyList<int> LocationIds,
            bool EmptyNamedList,
            IReadOnlyDictionary<string, PermissionLevel> AdminOverrides,
            string? OwnerGateMessage = null
        )
        {
            public static RestaurantAccess ForEmptyNamedList(int restaurantId)
            {
                return new RestaurantAccess(
                    restaurantId,
                    null,
                    [],
                    true,
                    new Dictionary<string, PermissionLevel>()
                );
            }
        }
    }
}
