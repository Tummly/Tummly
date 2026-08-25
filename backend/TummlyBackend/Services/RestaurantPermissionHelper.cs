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
            var role = user.FindFirstValue(ClaimTypes.Role);
            if (role is "Admin" or "Support")
            {
                return RestaurantPermissionDecision.Deny();
            }

            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out var userId)
            )
            {
                return RestaurantPermissionDecision.Deny();
            }

            var operatorUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (operatorUser == null)
            {
                return RestaurantPermissionDecision.Deny();
            }

            var active = await _context.RestaurantMemberships
                .AsNoTracking()
                .Where(m =>
                    m.UserId == userId
                    && m.Status == MembershipStatus.Active
                )
                .ToListAsync();

            var restaurantId = ResolveRestaurantId(
                operatorUser,
                active.Select(m => m.RestaurantId).ToList()
            );
            if (restaurantId == null && active.Count == 0)
            {
                var hasMembershipRow = await _context.RestaurantMemberships
                    .AsNoTracking()
                    .AnyAsync(m => m.UserId == userId);

                if (!hasMembershipRow)
                {
                    var owned = await _context.Restaurants
                        .AsNoTracking()
                        .Where(r => r.OwnerUserId == userId)
                        .Select(r => r.Id)
                        .ToListAsync();

                    restaurantId = ResolveRestaurantId(operatorUser, owned);
                }
            }

            if (restaurantId == null)
            {
                return RestaurantPermissionDecision.Deny();
            }

            var membership = active.FirstOrDefault(m =>
                m.RestaurantId == restaurantId
            );

            var permissionRole = membership?.PermissionRole ?? PermissionRoles.Owner;
            var cell = DefaultPermissionMatrix.LevelFor(permissionRole, areaId);

            if (!DefaultPermissionMatrix.Meets(cell, minimum))
            {
                return RestaurantPermissionDecision.Deny();
            }

            if (membership != null && NamedListIsEmpty(membership))
            {
                return RestaurantPermissionDecision.Deny();
            }

            return RestaurantPermissionDecision.Allow(restaurantId.Value);
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
            User user,
            IReadOnlyList<int> restaurantIds
        )
        {
            if (restaurantIds.Count == 0)
            {
                return null;
            }

            if (
                user.SelectedRestaurantId is int selected
                && restaurantIds.Contains(selected)
            )
            {
                return selected;
            }

            if (restaurantIds.Count == 1)
            {
                return restaurantIds[0];
            }

            return null;
        }
    }
}
