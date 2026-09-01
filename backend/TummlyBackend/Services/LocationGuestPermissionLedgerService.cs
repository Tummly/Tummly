using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class LocationGuestPermissionLedgerService
        : ILocationGuestPermissionLedgerService
    {
        private readonly ApplicationDbContext _context;

        public LocationGuestPermissionLedgerService(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        public void RecordEvent(
            int locationGuestId,
            int restaurantLocationId,
            LocationGuestPermissionKind permissionKind,
            string eventKind,
            string source,
            DateTime occurredAt,
            int? actorUserId = null
        )
        {
            if (
                eventKind
                    != LocationGuestPermissionLedgerEventKinds.Grant
                && eventKind
                    != LocationGuestPermissionLedgerEventKinds.Withdraw
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventKind),
                    eventKind,
                    "Permission ledger event kind must be grant or withdraw."
                );
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException(
                    "Permission ledger source is required.",
                    nameof(source)
                );
            }

            _context.LocationGuestPermissionLedgerEntries.Add(
                new LocationGuestPermissionLedgerEntry
                {
                    LocationGuestId = locationGuestId,
                    RestaurantLocationId = restaurantLocationId,
                    PermissionKind = permissionKind,
                    EventKind = eventKind,
                    Source = source.Trim(),
                    ActorUserId = actorUserId,
                    OccurredAt = GuestsDateWindows.EnsureUtc(occurredAt),
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }

        public async Task<
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            >
        > GetCurrentStatesAsync(
            int locationGuestId,
            CancellationToken cancellationToken = default
        )
        {
            var latestEvents = await _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(e => e.LocationGuestId == locationGuestId)
                .GroupBy(e => e.PermissionKind)
                .Select(g => g.OrderByDescending(e => e.OccurredAt)
                    .ThenByDescending(e => e.Id)
                    .First())
                .ToListAsync(cancellationToken);

            var states = LocationGuestPermissionKindExtensions.All.ToDictionary(
                kind => kind,
                _ => LocationGuestPermissionState.NotRecorded
            );

            foreach (var entry in latestEvents)
            {
                states[entry.PermissionKind] =
                    entry.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Grant
                        ? LocationGuestPermissionState.Granted
                        : LocationGuestPermissionState.Withdrawn;
            }

            return states;
        }

        public async Task<LocationGuestMarketingPreference> SyncMarketingPreferenceRollupAsync(
            int locationGuestId,
            CancellationToken cancellationToken = default
        )
        {
            var locationGuest = await _context.LocationGuests
                .FirstOrDefaultAsync(
                    lg => lg.Id == locationGuestId,
                    cancellationToken
                );

            if (locationGuest == null)
            {
                throw new InvalidOperationException(
                    "Location Guest not found."
                );
            }

            var states = await GetCurrentStatesAsync(
                locationGuestId,
                cancellationToken
            );
            var rollup = LocationGuestMarketingPreferenceRollup.Derive(states);
            locationGuest.MarketingPreference = rollup;
            return rollup;
        }
    }
}
