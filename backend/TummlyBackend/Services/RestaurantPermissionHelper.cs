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

            var restaurantId = ResolveRestaurantId(operatorUser, active);
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

                    restaurantId = ResolveOwnedFallback(operatorUser, owned);
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

            return RestaurantPermissionDecision.Allow(restaurantId.Value);
        }

        private static int? ResolveRestaurantId(
            User user,
            IReadOnlyList<RestaurantMembership> active
        )
        {
            if (active.Count == 0)
            {
                return null;
            }

            if (
                user.SelectedRestaurantId is int selected
                && active.Any(m => m.RestaurantId == selected)
            )
            {
                return selected;
            }

            if (active.Count == 1)
            {
                return active[0].RestaurantId;
            }

            return null;
        }

        private static int? ResolveOwnedFallback(
            User user,
            IReadOnlyList<int> ownedRestaurantIds
        )
        {
            if (ownedRestaurantIds.Count == 0)
            {
                return null;
            }

            if (
                user.SelectedRestaurantId is int selected
                && ownedRestaurantIds.Contains(selected)
            )
            {
                return selected;
            }

            if (ownedRestaurantIds.Count == 1)
            {
                return ownedRestaurantIds[0];
            }

            return null;
        }
    }
}
