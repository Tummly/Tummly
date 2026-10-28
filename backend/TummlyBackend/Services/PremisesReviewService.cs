using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class PremisesReviewService : IPremisesReviewService
    {
        private static readonly JsonSerializerOptions DetailJsonOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly ApplicationDbContext _context;

        public PremisesReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public const int FsaMaxLength = 50;

        public static string? NormalizeFsa(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed)
                ? null
                : trimmed.ToUpperInvariant();
        }

        public async Task<PremisesCheckResult> CheckAsync(
            PremisesCheckInput input,
            CancellationToken cancellationToken = default
        )
        {
            var key = PremisesKey.ForStorage(
                PremisesKey.Build(input.Address, input.City, input.Postcode)
            );
            var fsa = NormalizeFsa(input.FsaEstablishmentId);
            if (key == null && fsa == null)
            {
                return new PremisesCheckResult(null, null, []);
            }

            var candidates = new Dictionary<int, Candidate>();
            var hasKey = key != null;
            var hasFsa = fsa != null;
            var excludeId = input.ExcludeLocationId ?? 0;

            var persisted = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row =>
                    row.Id != excludeId
                    && (
                        (hasKey && row.PremisesKey == key)
                        || (hasFsa && row.FsaEstablishmentId == fsa)
                    )
                )
                .Select(row => new
                {
                    row.Id,
                    row.RestaurantId,
                    KeyMatch = hasKey && row.PremisesKey == key,
                    FsaMatch = hasFsa && row.FsaEstablishmentId == fsa,
                })
                .ToListAsync(cancellationToken);
            foreach (var row in persisted)
            {
                candidates[row.Id] = new Candidate(
                    row.Id,
                    row.RestaurantId,
                    row.KeyMatch,
                    row.FsaMatch
                );
            }

            if (key != null)
            {
                await AddLegacyMatchesAsync(
                    input,
                    key,
                    excludeId,
                    candidates,
                    cancellationToken
                );
                await AddStarterKitSnapshotMatchesAsync(
                    input,
                    key,
                    excludeId,
                    candidates,
                    cancellationToken
                );
            }

            if (candidates.Count == 0)
            {
                return new PremisesCheckResult(key, fsa, []);
            }

            var workspaceIds = candidates.Values
                .Select(c => c.RestaurantId)
                .Distinct()
                .ToList();
            var locationIds = candidates.Keys.ToList();

            var pilotWorkspaces = (
                await _context.BillingAccounts
                    .AsNoTracking()
                    .Where(row =>
                        workspaceIds.Contains(row.RestaurantId)
                        && row.PilotStartedAtUtc != null
                    )
                    .Select(row => row.RestaurantId)
                    .ToListAsync(cancellationToken)
            ).ToHashSet();
            pilotWorkspaces.UnionWith(
                await _context.CreditLedgerEntries
                    .AsNoTracking()
                    .Where(row =>
                        workspaceIds.Contains(row.RestaurantId)
                        && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                    )
                    .Select(row => row.RestaurantId)
                    .Distinct()
                    .ToListAsync(cancellationToken)
            );
            var kitLocations = (
                await _context.ShopOrders
                    .AsNoTracking()
                    .Where(row =>
                        row.IsComplimentary && locationIds.Contains(row.LocationId)
                    )
                    .Select(row => row.LocationId)
                    .Distinct()
                    .ToListAsync(cancellationToken)
            ).ToHashSet();

            var matches = candidates.Values
                .OrderBy(c => c.LocationId)
                .Select(c => new PremisesMatch(
                    c.LocationId,
                    c.RestaurantId,
                    c.RestaurantId == input.RestaurantId,
                    c.KeyMatch,
                    c.FsaMatch,
                    pilotWorkspaces.Contains(c.RestaurantId),
                    kitLocations.Contains(c.LocationId)
                ))
                .ToList();
            return new PremisesCheckResult(key, fsa, matches);
        }

        public bool Apply(
            RestaurantLocation location,
            PremisesCheckResult result,
            bool alsoFlagged = false
        )
        {
            location.PremisesKey = result.PremisesKey;
            location.FsaEstablishmentId = result.FsaEstablishmentId;
            if (!result.Flagged && !alsoFlagged)
            {
                return false;
            }

            location.PremisesReviewStatus = PremisesReviewStatus.PendingReview;
            location.PremisesReviewedAtUtc = null;
            location.PremisesReviewedByAdminUserId = null;
            location.PremisesReviewReason = null;
            return true;
        }

        public void RecordFlag(
            RestaurantLocation location,
            PremisesCheckResult result,
            int? matchedImportRowIndex = null
        )
        {
            var detail = new
            {
                matchedImportRowIndex,
                matches = result.Matches.Select(match => new
                {
                    matchedLocationId = match.LocationId,
                    matchedRestaurantId = match.RestaurantId,
                    sameWorkspace = match.SameWorkspace,
                    premisesKeyMatch = match.PremisesKeyMatch,
                    fsaMatch = match.FsaMatch,
                    priorPilot = match.PriorPilot,
                    priorStarterKit = match.PriorStarterKit,
                }),
                priorPilot = result.Matches.Any(match => match.PriorPilot),
                priorStarterKit = result.Matches.Any(match => match.PriorStarterKit),
            };
            _context.AdminAuditEvents.Add(
                new AdminAuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = DateTime.UtcNow,
                    Action = AdminAuditActions.LocationPremisesFlagged,
                    ActorIdentity = "system",
                    TargetType = AdminAuditTargetTypes.RestaurantLocation,
                    TargetId = location.Id.ToString(),
                    RestaurantId = location.RestaurantId,
                    DetailJson = JsonSerializer.Serialize(detail, DetailJsonOptions),
                    Succeeded = true,
                }
            );
        }

        /// <summary>
        /// Rows that predate the persisted key have none. Build it here, match
        /// on it, and stage it on the tracked row so the caller's save fills
        /// the column. Rows are narrowed by postcode first.
        /// </summary>
        private async Task AddLegacyMatchesAsync(
            PremisesCheckInput input,
            string key,
            int excludeId,
            Dictionary<int, Candidate> candidates,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(input.Postcode))
            {
                return;
            }

            var postcodeKey = UkPostcode.NormalizeForLookup(input.Postcode);
            var legacy = await _context.RestaurantLocations
                .Where(row =>
                    row.PremisesKey == null
                    && row.Id != excludeId
                    && row.Postcode != null
                    && row.Postcode.ToUpper().Replace(" ", "") == postcodeKey
                )
                .ToListAsync(cancellationToken);
            foreach (var row in legacy)
            {
                var rowKey = PremisesKey.ForStorage(
                    PremisesKey.Build(row.Address, row.City, row.Postcode)
                );
                if (rowKey == null)
                {
                    continue;
                }

                row.PremisesKey = rowKey;
                if (rowKey == key)
                {
                    candidates.TryAdd(
                        row.Id,
                        new Candidate(row.Id, row.RestaurantId, true, false)
                    );
                }
            }
        }

        /// <summary>
        /// A complimentary Starter Kit order keeps the address it shipped to.
        /// That still counts after the Location was edited or deleted.
        /// </summary>
        private async Task AddStarterKitSnapshotMatchesAsync(
            PremisesCheckInput input,
            string key,
            int excludeId,
            Dictionary<int, Candidate> candidates,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(input.Postcode))
            {
                return;
            }

            var postcodeKey = UkPostcode.NormalizeForLookup(input.Postcode);
            var orders = await _context.ShopOrders
                .AsNoTracking()
                .Where(row =>
                    row.IsComplimentary
                    && row.LocationId != excludeId
                    && row.ShipToPostcode.ToUpper().Replace(" ", "") == postcodeKey
                )
                .Select(row => new
                {
                    row.LocationId,
                    row.RestaurantId,
                    row.ShipToAddressLine1,
                    row.ShipToAddressLine2,
                    row.ShipToPostcode,
                })
                .ToListAsync(cancellationToken);
            foreach (var order in orders)
            {
                var orderKey = PremisesKey.ForStorage(
                    PremisesKey.Build(
                        order.ShipToAddressLine1,
                        order.ShipToAddressLine2,
                        order.ShipToPostcode
                    )
                );
                if (orderKey == key)
                {
                    candidates.TryAdd(
                        order.LocationId,
                        new Candidate(
                            order.LocationId,
                            order.RestaurantId,
                            true,
                            false
                        )
                    );
                }
            }
        }

        private sealed record Candidate(
            int LocationId,
            int RestaurantId,
            bool KeyMatch,
            bool FsaMatch
        );
    }
}
