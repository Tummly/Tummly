using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Upserts / finds <see cref="GuestContactSuppression"/> rows by hashed
    /// contact key. Does not call SaveChanges.
    /// </summary>
    public sealed class GuestContactSuppressionService
        : IGuestContactSuppressionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGuestContactHasher _hasher;

        public GuestContactSuppressionService(
            ApplicationDbContext context,
            IGuestContactHasher hasher
        )
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task StageAsync(
            int restaurantId,
            string contactKey,
            LocationGuestPermissionKind kind,
            DateTime withdrawnAtUtc,
            string source,
            CancellationToken ct = default
        )
        {
            ArgumentException.ThrowIfNullOrEmpty(contactKey);
            ArgumentException.ThrowIfNullOrEmpty(source);

            var hash = _hasher.Hash(contactKey);
            // Unspecified (SQL/datetime2) keeps wall time as UTC; Local converts.
            var withdrawn = GuestsDateWindows.EnsureUtc(withdrawnAtUtc);

            var existing = await FindExistingAsync(
                restaurantId,
                kind,
                hash,
                ct
            );

            if (existing is null)
            {
                _context.GuestContactSuppressions.Add(
                    new GuestContactSuppression
                    {
                        RestaurantId = restaurantId,
                        PermissionKind = kind,
                        ContactHash = hash,
                        WithdrawnAtUtc = withdrawn,
                        Source = source,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                return;
            }

            var existingWithdrawn = GuestsDateWindows.EnsureUtc(
                existing.WithdrawnAtUtc
            );
            if (withdrawn > existingWithdrawn)
            {
                existing.WithdrawnAtUtc = withdrawn;
                existing.Source = source;
            }
        }

        public async Task<IReadOnlyList<GuestContactSuppression>> FindAsync(
            int restaurantId,
            string contactKey,
            CancellationToken ct = default
        )
        {
            ArgumentException.ThrowIfNullOrEmpty(contactKey);

            var hash = _hasher.Hash(contactKey);

            // Include Local (Added / Modified) so callers that Stage then Find
            // in one unit of work see the row before SaveChanges (SQL Server
            // queries otherwise miss unsaved Added entities).
            var local = _context.GuestContactSuppressions.Local
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.ContactHash == hash
                )
                .ToList();

            var fromDb = await _context.GuestContactSuppressions
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.ContactHash == hash
                )
                .ToListAsync(ct);

            if (local.Count == 0)
            {
                return fromDb;
            }

            var seen = new HashSet<GuestContactSuppression>(fromDb);
            foreach (var row in local)
            {
                seen.Add(row);
            }

            return seen.ToList();
        }

        public async Task<IReadOnlySet<string>> FindContactKeysWithKindAsync(
            int restaurantId,
            LocationGuestPermissionKind kind,
            IEnumerable<string> contactKeys,
            CancellationToken ct = default
        )
        {
            var keys = contactKeys.Distinct(StringComparer.Ordinal).ToList();
            if (keys.Count == 0)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var storedHashes = (
                await _context.GuestContactSuppressions
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.PermissionKind == kind
                    )
                    .Select(row => row.ContactHash)
                    .ToListAsync(ct)
            ).ToHashSet(StringComparer.Ordinal);

            return keys
                .Where(key => storedHashes.Contains(_hasher.Hash(key)))
                .ToHashSet(StringComparer.Ordinal);
        }

        private async Task<GuestContactSuppression?> FindExistingAsync(
            int restaurantId,
            LocationGuestPermissionKind kind,
            string hash,
            CancellationToken ct
        )
        {
            var local = _context.GuestContactSuppressions.Local.FirstOrDefault(
                row =>
                    row.RestaurantId == restaurantId
                    && row.PermissionKind == kind
                    && row.ContactHash == hash
            );
            if (local is not null)
            {
                return local;
            }

            return await _context.GuestContactSuppressions
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.PermissionKind == kind
                        && row.ContactHash == hash,
                    ct
                );
        }
    }
}
