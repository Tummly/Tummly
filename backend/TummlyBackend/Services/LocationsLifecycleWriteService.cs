using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class LocationsLifecycleWriteService
        : ILocationsLifecycleWriteService
    {
        private static readonly HashSet<string> ManagerEligibleRoles =
        [
            PermissionRoles.Owner,
            PermissionRoles.Admin,
            PermissionRoles.AreaManager,
            PermissionRoles.LocationManager,
        ];

        private readonly ApplicationDbContext _context;

        public LocationsLifecycleWriteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LocationLifecycleWriteResult> ActivateDraftAsync(
            int restaurantId,
            int locationId,
            int actorUserId
        )
        {
            var location = await LoadOwnedAsync(restaurantId, locationId);
            if (location == null)
            {
                return new LocationLifecycleWriteResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Draft)
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Only draft locations can be activated."
                );
            }

            var missing = ValidateActivateFields(location);
            if (missing != null)
            {
                return missing;
            }

            var from = location.LifecycleStatus.ToString();
            location.LifecycleStatus = LocationLifecycleStatus.Active;
            await EmitLifecycleAsync(
                restaurantId,
                locationId,
                actorUserId,
                from,
                LocationLifecycleStatus.Active.ToString(),
                $"Activated location “{location.LocationName}”."
            );
            await _context.SaveChangesAsync();
            return new LocationLifecycleWriteResult.Ok();
        }

        public async Task<LocationLifecycleWriteResult> DeleteDraftAsync(
            int restaurantId,
            int locationId,
            int actorUserId
        )
        {
            var location = await LoadOwnedAsync(restaurantId, locationId);
            if (location == null)
            {
                return new LocationLifecycleWriteResult.NotFound();
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Draft)
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Only draft locations can be deleted."
                );
            }

            if (await HasHistoryAsync(locationId))
            {
                return new LocationLifecycleWriteResult.Conflict(
                    "This draft cannot be deleted because it has guest or history records."
                );
            }

            var name = location.LocationName;
            var linkedActivities = await _context.LocationActivities
                .Where(row => row.LocationId == locationId)
                .ToListAsync();
            foreach (var row in linkedActivities)
            {
                row.LocationId = null;
            }

            _context.RestaurantLocations.Remove(location);
            await EmitLifecycleAsync(
                restaurantId,
                locationId: null,
                actorUserId,
                LocationLifecycleStatus.Draft.ToString(),
                "Deleted",
                $"Deleted draft location “{name}”."
            );
            await _context.SaveChangesAsync();
            return new LocationLifecycleWriteResult.Ok();
        }

        public async Task<LocationLifecycleWriteResult> SetManagerAsync(
            int restaurantId,
            int locationId,
            int actorUserId,
            int? managerUserId
        )
        {
            var location = await LoadOwnedAsync(restaurantId, locationId);
            if (location == null)
            {
                return new LocationLifecycleWriteResult.NotFound();
            }

            if (managerUserId == null)
            {
                var fromClear = location.ManagerUserId?.ToString();
                location.ManagerUserId = null;
                await EmitManagerAsync(
                    restaurantId,
                    locationId,
                    actorUserId,
                    fromClear,
                    null,
                    $"Cleared manager for “{location.LocationName}”."
                );
                await _context.SaveChangesAsync();
                return new LocationLifecycleWriteResult.Ok();
            }

            var membership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.UserId == managerUserId.Value
                    && row.Status == MembershipStatus.Active
                );

            if (membership == null)
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Manager must be an active restaurant member."
                );
            }

            if (!ManagerEligibleRoles.Contains(membership.PermissionRole))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Manager role must be Owner, Admin, Area Manager, or Location Manager."
                );
            }

            if (
                membership.LocationScope == LocationScopeKind.NamedList
                && !MembershipLocationScope
                    .ParseNamedIds(membership.NamedLocationIdsJson)
                    .Contains(locationId)
            )
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Manager location scope must include this location."
                );
            }

            var from = location.ManagerUserId?.ToString();
            location.ManagerUserId = managerUserId;
            await EmitManagerAsync(
                restaurantId,
                locationId,
                actorUserId,
                from,
                managerUserId.Value.ToString(),
                $"Set manager for “{location.LocationName}”."
            );
            await _context.SaveChangesAsync();
            return new LocationLifecycleWriteResult.Ok();
        }

        private async Task<RestaurantLocation?> LoadOwnedAsync(
            int restaurantId,
            int locationId
        )
        {
            return await _context.RestaurantLocations
                .FirstOrDefaultAsync(row =>
                    row.Id == locationId && row.RestaurantId == restaurantId
                );
        }

        private static LocationLifecycleWriteResult.InvalidRequest? ValidateActivateFields(
            RestaurantLocation location
        )
        {
            if (string.IsNullOrWhiteSpace(location.LocationName))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Location name is required to activate."
                );
            }

            if (string.IsNullOrWhiteSpace(location.Address))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Address is required to activate."
                );
            }

            if (string.IsNullOrWhiteSpace(location.City))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "City is required to activate."
                );
            }

            if (string.IsNullOrWhiteSpace(location.Postcode))
            {
                return new LocationLifecycleWriteResult.InvalidRequest(
                    "Postcode is required to activate."
                );
            }

            return null;
        }

        private async Task<bool> HasHistoryAsync(int locationId)
        {
            if (
                await _context.LocationGuests.AnyAsync(row =>
                    row.RestaurantLocationId == locationId
                )
            )
            {
                return true;
            }

            if (
                await _context.Feedbacks.AnyAsync(row =>
                    row.RestaurantLocationId == locationId
                )
            )
            {
                return true;
            }

            if (
                await _context.Campaigns.AnyAsync(row =>
                    row.RestaurantLocationId == locationId
                )
            )
            {
                return true;
            }

            return false;
        }

        private async Task EmitLifecycleAsync(
            int restaurantId,
            int? locationId,
            int actorUserId,
            string? from,
            string to,
            string description
        )
        {
            var actorDisplayName = await ActorDisplayNameAsync(actorUserId);
            _context.LocationActivities.Add(
                new LocationActivity
                {
                    RestaurantId = restaurantId,
                    LocationId = locationId,
                    ActorUserId = actorUserId,
                    ActorDisplayName = actorDisplayName,
                    Kind = LocationActivityKinds.LifecycleChanged,
                    Description = description,
                    FromValue = from,
                    ToValue = to,
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }

        private async Task EmitManagerAsync(
            int restaurantId,
            int locationId,
            int actorUserId,
            string? from,
            string? to,
            string description
        )
        {
            var actorDisplayName = await ActorDisplayNameAsync(actorUserId);
            _context.LocationActivities.Add(
                new LocationActivity
                {
                    RestaurantId = restaurantId,
                    LocationId = locationId,
                    ActorUserId = actorUserId,
                    ActorDisplayName = actorDisplayName,
                    Kind = LocationActivityKinds.ManagerChanged,
                    Description = description,
                    FromValue = from,
                    ToValue = to,
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }

        private async Task<string?> ActorDisplayNameAsync(int actorUserId)
        {
            var name = await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == actorUserId)
                .Select(row => row.FullName)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }
    }
}
