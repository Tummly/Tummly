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
            var persisted = await _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(e => e.LocationGuestId == locationGuestId)
                .ToListAsync(cancellationToken);

            var pending = _context.ChangeTracker
                .Entries<LocationGuestPermissionLedgerEntry>()
                .Where(e =>
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified
                )
                .Select(e => e.Entity)
                .Where(e => e.LocationGuestId == locationGuestId);

            var latestEvents = persisted
                .Concat(pending)
                .GroupBy(e => e.PermissionKind)
                .Select(g => g.OrderByDescending(e => e.OccurredAt)
                    .ThenByDescending(e => e.Id)
                    .First());

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

            return await SyncMarketingPreferenceRollupAsync(
                locationGuest,
                cancellationToken
            );
        }

        public async Task<LocationGuestMarketingPreference> SyncMarketingPreferenceRollupAsync(
            LocationGuest locationGuest,
            CancellationToken cancellationToken = default
        )
        {
            var states = await GetCurrentStatesAsync(
                locationGuest.Id,
                cancellationToken
            );
            var rollup = LocationGuestMarketingPreferenceRollup.Derive(states);
            locationGuest.MarketingPreference = rollup;
            return rollup;
        }

        public async Task<
            IReadOnlyDictionary<
                int,
                IReadOnlyDictionary<
                    LocationGuestPermissionKind,
                    LocationGuestPermissionState
                >
            >
        > GetCurrentStatesBatchAsync(
            IReadOnlyList<int> locationGuestIds,
            CancellationToken cancellationToken = default
        )
        {
            if (locationGuestIds.Count == 0)
            {
                return new Dictionary<
                    int,
                    IReadOnlyDictionary<
                        LocationGuestPermissionKind,
                        LocationGuestPermissionState
                    >
                >();
            }

            var idSet = locationGuestIds.Distinct().ToHashSet();

            var persisted = await _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(e => idSet.Contains(e.LocationGuestId))
                .ToListAsync(cancellationToken);

            var pending = _context.ChangeTracker
                .Entries<LocationGuestPermissionLedgerEntry>()
                .Where(e =>
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified
                )
                .Select(e => e.Entity)
                .Where(e => idSet.Contains(e.LocationGuestId));

            var latestByGuestAndKind = persisted
                .Concat(pending)
                .GroupBy(e => (e.LocationGuestId, e.PermissionKind))
                .Select(g => g.OrderByDescending(e => e.OccurredAt)
                    .ThenByDescending(e => e.Id)
                    .First());

            var result =
                new Dictionary<
                    int,
                    IReadOnlyDictionary<
                        LocationGuestPermissionKind,
                        LocationGuestPermissionState
                    >
                >();

            foreach (var guestId in idSet)
            {
                var states = LocationGuestPermissionKindExtensions.All.ToDictionary(
                    kind => kind,
                    _ => LocationGuestPermissionState.NotRecorded
                );

                foreach (var entry in latestByGuestAndKind.Where(e =>
                             e.LocationGuestId == guestId
                         ))
                {
                    states[entry.PermissionKind] =
                        entry.EventKind
                            == LocationGuestPermissionLedgerEventKinds.Grant
                            ? LocationGuestPermissionState.Granted
                            : LocationGuestPermissionState.Withdrawn;
                }

                result[guestId] = states;
            }

            return result;
        }
    }
}
