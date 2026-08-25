using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.TeamPermissions;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class TeamPermissionsService : ITeamPermissionsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;

        public TeamPermissionsService(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions
        )
        {
            _context = context;
            _permissions = permissions;
        }

        public async Task<TeamPermissionsPageDto?> GetPageAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await ActorMembershipAsync(
                actorUserId,
                restaurantId
            );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => new TeamPermissionsLocationDto
                {
                    Id = row.Id,
                    Name = row.LocationName,
                })
                .ToListAsync();

            var memberships = await _context.RestaurantMemberships
                .AsNoTracking()
                .Include(row => row.User)
                .Where(row => row.RestaurantId == restaurantId)
                .ToListAsync();

            var namesById = locations.ToDictionary(row => row.Id, row => row.Name);

            var members = memberships
                .Select(row => MapMember(
                    row,
                    restaurant.OwnerUserId,
                    actorRole,
                    actorCanManage,
                    actorUserId,
                    namesById
                ))
                .OrderBy(row => row.Status == "active" ? 0 : 1)
                .ThenBy(row => row.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var active = memberships
                .Where(row => row.Status == MembershipStatus.Active)
                .ToList();

            return new TeamPermissionsPageDto
            {
                ActorCanManage = actorCanManage,
                ActorPermissionRole = actorRole,
                PrivacyConsentHasAccess = DefaultPermissionMatrix.Meets(
                    DefaultPermissionMatrix.LevelFor(
                        actorRole,
                        OperatorAreaIds.PrivacyConsent
                    ),
                    PermissionLevel.View
                ),
                IsSingleLocation = locations.Count <= 1,
                Stats = new TeamPermissionsStatsDto
                {
                    ActiveMembers = active.Count,
                    PendingInvites = 0,
                    LocationManagers = active.Count(row =>
                        row.PermissionRole == PermissionRoles.LocationManager
                    ),
                    LimitedAccessUsers = active.Count(row =>
                        row.LocationScope == LocationScopeKind.NamedList
                    ),
                },
                Locations = locations,
                Members = members,
            };
        }

        public async Task<string?> UpdateRoleAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId,
            string permissionRole
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.Status != MembershipStatus.Active)
            {
                return "Role cannot change while the member is deactivated.";
            }

            if (
                !TeamPermissionsActor.MayAssignRole(
                    loaded.ActorRole,
                    permissionRole
                )
            )
            {
                return "That permission role is not allowed.";
            }

            var namedIds = MembershipLocationScope.ParseNamedIds(
                target.NamedLocationIdsJson
            );
            var scopeError = MembershipLocationScope.Validate(
                permissionRole,
                target.LocationScope,
                namedIds
            );
            if (scopeError != null)
            {
                return scopeError;
            }

            if (target.PermissionRole == permissionRole)
            {
                return null;
            }

            var from = target.PermissionRole;
            target.PermissionRole = permissionRole;
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.RoleChanged,
                from,
                permissionRole
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> UpdateLocationScopeAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId,
            IReadOnlyList<int> allowedLocationIds,
            string locationScope,
            int[] namedLocationIds
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.PermissionRole == PermissionRoles.Owner)
            {
                return "Owner location scope is not editable.";
            }

            if (target.Status != MembershipStatus.Active)
            {
                return "Location scope cannot change while the member is deactivated.";
            }

            var kind = locationScope == "named"
                ? LocationScopeKind.NamedList
                : LocationScopeKind.AllLocations;
            var named = namedLocationIds.Distinct().ToArray();
            var scopeError = MembershipLocationScope.Validate(
                target.PermissionRole,
                kind,
                named
            );
            if (scopeError != null)
            {
                return scopeError;
            }

            if (kind == LocationScopeKind.NamedList)
            {
                var namedDecision =
                    await _permissions.AuthorizeNamedLocationIdsAsync(
                            allowedLocationIds,
                            named
                        );
                if (namedDecision.Status != RestaurantPermissionStatus.Allowed)
                {
                    return namedDecision.Message;
                }
            }

            var from = FormatScope(target);
            target.LocationScope = kind;
            target.NamedLocationIdsJson = kind == LocationScopeKind.NamedList
                ? MembershipLocationScope.SerializeNamedIds(named)
                : "[]";
            var to = FormatScope(target);
            if (from == to)
            {
                return null;
            }

            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.LocationScopeChanged,
                from,
                to
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> DeactivateAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.Status != MembershipStatus.Active)
            {
                return null;
            }

            ReassignKeyContacts(loaded.Restaurant!, target.UserId);
            target.Status = MembershipStatus.Deactivated;
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.MemberDeactivated,
                MembershipStatus.Active.ToString(),
                MembershipStatus.Deactivated.ToString()
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> ReactivateAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.Status != MembershipStatus.Deactivated)
            {
                return null;
            }

            target.Status = MembershipStatus.Active;
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.MemberReactivated,
                MembershipStatus.Deactivated.ToString(),
                MembershipStatus.Active.ToString()
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> RemoveAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            ReassignKeyContacts(loaded.Restaurant!, target.UserId);
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.MemberRemoved,
                target.PermissionRole,
                null
            );
            _context.RestaurantMemberships.Remove(target);
            await _context.SaveChangesAsync();
            return null;
        }

        private async Task<(
            RestaurantMembership? Target,
            Restaurant? Restaurant,
            string ActorRole,
            string? Error
        )> LoadWriteTargetAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return (null, null, PermissionRoles.Owner, "Restaurant not found.");
            }

            var actorMembership = await ActorMembershipAsync(
                actorUserId,
                restaurantId
            );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            var target = await _context.RestaurantMemberships
                .FirstOrDefaultAsync(row =>
                    row.Id == membershipId && row.RestaurantId == restaurantId
                );
            if (target == null)
            {
                return (null, restaurant, actorRole, "Member not found.");
            }

            if (
                !TeamPermissionsActor.MayWriteTarget(
                    actorRole,
                    actorCanManage,
                    actorUserId,
                    restaurant.OwnerUserId,
                    target
                )
            )
            {
                return (null, restaurant, actorRole, "forbidden");
            }

            return (target, restaurant, actorRole, null);
        }

        private async Task<RestaurantMembership?> ActorMembershipAsync(
            int actorUserId,
            int restaurantId
        )
        {
            return await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == actorUserId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
        }

        private static TeamMemberRowDto MapMember(
            RestaurantMembership row,
            int ownerUserId,
            string actorRole,
            bool actorCanManage,
            int actorUserId,
            IReadOnlyDictionary<int, string> namesById
        )
        {
            var named = MembershipLocationScope.ParseNamedIds(
                row.NamedLocationIdsJson
            );
            return new TeamMemberRowDto
            {
                MembershipId = row.Id,
                UserId = row.UserId,
                FullName = row.User.FullName,
                Email = row.User.Email,
                PermissionRole = row.PermissionRole,
                LocationScope =
                    row.LocationScope == LocationScopeKind.NamedList
                        ? "named"
                        : "all",
                NamedLocationIds = named.ToList(),
                LocationAccessLabel = FormatAccessLabel(row, named, namesById),
                Status =
                    row.Status == MembershipStatus.Active
                        ? "active"
                        : "deactivated",
                IsAccountOwner = row.UserId == ownerUserId,
                Actions = TeamPermissionsActor.ActionsFor(
                    actorRole,
                    actorCanManage,
                    actorUserId,
                    ownerUserId,
                    row
                ).ToList(),
            };
        }

        private static string FormatAccessLabel(
            RestaurantMembership row,
            IReadOnlyList<int> named,
            IReadOnlyDictionary<int, string> namesById
        )
        {
            if (row.LocationScope != LocationScopeKind.NamedList)
            {
                return "All locations";
            }

            var names = named
                .Select(id => namesById.TryGetValue(id, out var name) ? name : $"#{id}")
                .ToList();
            if (names.Count == 1)
            {
                return $"{names[0]} only";
            }

            return string.Join(", ", names);
        }

        private static string FormatScope(RestaurantMembership row)
        {
            if (row.LocationScope == LocationScopeKind.AllLocations)
            {
                return "all";
            }

            return row.NamedLocationIdsJson;
        }

        private static void ReassignKeyContacts(Restaurant restaurant, int userId)
        {
            if (restaurant.BillingContactUserId == userId)
            {
                restaurant.BillingContactUserId = restaurant.OwnerUserId;
            }

            if (restaurant.PrivacyContactUserId == userId)
            {
                restaurant.PrivacyContactUserId = restaurant.OwnerUserId;
            }

            if (restaurant.SupportContactUserId == userId)
            {
                restaurant.SupportContactUserId = restaurant.OwnerUserId;
            }
        }

        private void AddActivity(
            int restaurantId,
            int actorUserId,
            RestaurantMembership target,
            string kind,
            string? from,
            string? to
        )
        {
            if (from == to)
            {
                return;
            }

            _context.RestaurantAccessActivities.Add(
                new RestaurantAccessActivity
                {
                    RestaurantId = restaurantId,
                    ActorUserId = actorUserId,
                    TargetUserId = target.UserId,
                    TargetMembershipId = target.Id,
                    Kind = kind,
                    FromValue = from,
                    ToValue = to,
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }
    }
}
