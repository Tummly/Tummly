using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OwnedLocation;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <remarks>
    /// Assistant quotes stay: this delete does not change stored Assistant
    /// messages. Snapshot Name and excerpt remain.
    /// </remarks>
    public class LocationGuestDeleteService : ILocationGuestDeleteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IAdminAuditService _audit;
        private readonly IGuestContactSuppressionService _suppressions;

        public LocationGuestDeleteService(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            IAdminAuditService audit,
            IGuestContactSuppressionService suppressions
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _audit = audit;
            _suppressions = suppressions;
        }

        public async Task<LocationGuestDeleteOutcome> DeleteAsync(
            int userId,
            int locationGuestId,
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var owned = await _ownedLocation.ResolveAsync(userId, locationId);

            switch (owned.Status)
            {
                case OwnedLocationResolveStatus.Found:
                    break;
                case OwnedLocationResolveStatus.NotFound:
                    return LocationGuestDeleteOutcome.LocationNotFound();
                case OwnedLocationResolveStatus.Forbidden:
                    return LocationGuestDeleteOutcome.Forbidden();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(owned),
                        owned.Status,
                        "Unexpected owned-location resolve status."
                    );
            }

            var locationGuest = await _context.LocationGuests
                .FirstOrDefaultAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (locationGuest == null)
            {
                return LocationGuestDeleteOutcome.NotFound();
            }

            var restaurantId = owned.Location!.RestaurantId;
            var nowUtc = DateTime.UtcNow;
            var counts = await LocationGuestHardDelete.ApplyAsync(
                _context,
                _suppressions,
                locationGuest,
                GuestContactSuppressionSources.GuestDelete,
                nowUtc,
                cancellationToken
            );

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.GuestDelete,
                    ActorIdentity: $"user:{userId}",
                    TargetType: AdminAuditTargetTypes.LocationGuest,
                    TargetId: locationGuestId.ToString(),
                    RestaurantId: restaurantId,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            locationId,
                            feedbacks = counts.Feedbacks,
                            claims = counts.Claims,
                            frozenRecipients = counts.FrozenRecipients,
                            deliveries = counts.Deliveries,
                        }
                    )
                )
            );

            await _context.SaveChangesAsync(cancellationToken);
            return LocationGuestDeleteOutcome.Deleted();
        }
    }
}
