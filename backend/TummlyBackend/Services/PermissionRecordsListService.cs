using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.PrivacyConsent;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class PermissionRecordsListService : IPermissionRecordsListService
    {
        public const int DefaultPageSize = 25;

        private readonly ApplicationDbContext _context;

        public PermissionRecordsListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> ListAsync(
            PermissionRecordsListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var sortKey = GuestScopedListValidation.ValidatePagingAndSort(
                query.Page,
                query.PageSize,
                query.Sort,
                DefaultPageSize
            );
            var (rangeFrom, rangeTo) =
                GuestScopedListValidation.ResolveOptionalDateWindow(
                    query.DatePreset,
                    query.DateFrom,
                    query.DateTo,
                    query.UtcOffsetMinutes
                );

            var scopedLocationIds = ResolveScopedLocationIds(
                query.LocationIds,
                query.Locations
            );
            if (scopedLocationIds.Count == 0)
            {
                return EmptyResponse(query);
            }

            var permissionKinds = ParsePermissionKinds(query.Permissions);
            var currentStateEventKinds = ParseCurrentStateEventKinds(
                query.CurrentStates
            );

            var baseQuery = _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(entry =>
                    scopedLocationIds.Contains(entry.RestaurantLocationId)
                    && entry.RestaurantLocation.RestaurantId == query.RestaurantId
                );

            if (permissionKinds != null)
            {
                baseQuery = baseQuery.Where(entry =>
                    permissionKinds.Contains(entry.PermissionKind)
                );
            }

            var latestEntryIds = await baseQuery
                .GroupBy(entry => new
                {
                    entry.LocationGuestId,
                    entry.PermissionKind,
                    entry.RestaurantLocationId,
                })
                .Select(group =>
                    group
                        .OrderByDescending(entry => entry.OccurredAt)
                        .ThenByDescending(entry => entry.Id)
                        .Select(entry => entry.Id)
                        .First()
                )
                .ToListAsync(cancellationToken);

            if (latestEntryIds.Count == 0)
            {
                return EmptyResponse(query);
            }

            var rowsQuery = _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(entry => latestEntryIds.Contains(entry.Id))
                .Select(entry => new PermissionRecordRowProjection
                {
                    Id = entry.Id,
                    LocationGuestId = entry.LocationGuestId,
                    LocationId = entry.RestaurantLocationId,
                    GuestName = entry.LocationGuest.Name,
                    PermissionKind = entry.PermissionKind,
                    EventKind = entry.EventKind,
                    LocationName = entry.RestaurantLocation.LocationName,
                    Source = entry.Source,
                    OccurredAt = entry.OccurredAt,
                });

            var search = query.Q?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                var lowered = search.ToLowerInvariant();
                rowsQuery = rowsQuery.Where(row =>
                    row.GuestName.ToLower().Contains(lowered)
                );
            }

            if (currentStateEventKinds != null)
            {
                rowsQuery = rowsQuery.Where(row =>
                    currentStateEventKinds.Contains(row.EventKind)
                );
            }

            if (rangeFrom.HasValue && rangeTo.HasValue)
            {
                var from = rangeFrom.Value;
                var to = rangeTo.Value;
                rowsQuery = rowsQuery.Where(row =>
                    row.OccurredAt >= from && row.OccurredAt < to
                );
            }

            var totalCount = await rowsQuery.CountAsync(cancellationToken);

            rowsQuery = sortKey == "oldest-first"
                ? rowsQuery.OrderBy(row => row.OccurredAt).ThenBy(row => row.Id)
                : rowsQuery
                    .OrderByDescending(row => row.OccurredAt)
                    .ThenByDescending(row => row.Id);

            var pageRows = await rowsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return new
            {
                success = true,
                page = query.Page,
                pageSize = query.PageSize,
                totalCount,
                rows = pageRows
                    .Select(row => new
                    {
                        id = row.Id.ToString(),
                        locationGuestId = row.LocationGuestId,
                        locationId = row.LocationId,
                        guestName = row.GuestName,
                        permissionId = row.PermissionKind.ToWireString(),
                        permissionLabel =
                            LocationGuestPermissionPresentation.PermissionLabel(
                                row.PermissionKind
                            ),
                        currentState =
                            row.EventKind
                            == LocationGuestPermissionLedgerEventKinds.Grant
                                ? LocationGuestPermissionStateExtensions.GrantedWire
                                : LocationGuestPermissionStateExtensions.WithdrawnWire,
                        locationLabel = row.LocationName,
                        source = row.Source,
                        sourceLabel = LocationGuestPermissionPresentation.SourceLabel(
                            row.Source
                        ),
                        recordedAt = row.OccurredAt.ToUniversalTime().ToString("O"),
                    })
                    .ToList(),
            };
        }

        private static HashSet<int> ResolveScopedLocationIds(
            IReadOnlyList<int> allowedLocationIds,
            string[] locationFilter
        )
        {
            var scoped = allowedLocationIds.ToHashSet();
            if (locationFilter.Length == 0)
            {
                return scoped;
            }

            var parsed = new HashSet<int>();
            foreach (var raw in locationFilter)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                if (!int.TryParse(raw.Trim(), out var locationId))
                {
                    throw new ArgumentException(
                        $"Unknown location id '{raw}'."
                    );
                }

                parsed.Add(locationId);
            }

            if (parsed.Count == 0)
            {
                return scoped;
            }

            scoped.IntersectWith(parsed);
            return scoped;
        }

        private static HashSet<LocationGuestPermissionKind>? ParsePermissionKinds(
            string[] permissions
        )
        {
            if (permissions.Length == 0)
            {
                return null;
            }

            var kinds = new HashSet<LocationGuestPermissionKind>();
            foreach (var permission in permissions)
            {
                if (string.IsNullOrWhiteSpace(permission))
                {
                    continue;
                }

                if (
                    !LocationGuestPermissionKindExtensions.TryFromWireString(
                        permission,
                        out var kind
                    )
                )
                {
                    throw new ArgumentException(
                        $"Unknown permission id '{permission}'."
                    );
                }

                kinds.Add(kind);
            }

            return kinds.Count == 0 ? null : kinds;
        }

        private static HashSet<string>? ParseCurrentStateEventKinds(
            string[] currentStates
        )
        {
            if (currentStates.Length == 0)
            {
                return null;
            }

            var eventKinds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var currentState in currentStates)
            {
                if (string.IsNullOrWhiteSpace(currentState))
                {
                    continue;
                }

                switch (currentState.Trim().ToLowerInvariant())
                {
                    case LocationGuestPermissionStateExtensions.GrantedWire:
                        eventKinds.Add(
                            LocationGuestPermissionLedgerEventKinds.Grant
                        );
                        break;
                    case LocationGuestPermissionStateExtensions.WithdrawnWire:
                        eventKinds.Add(
                            LocationGuestPermissionLedgerEventKinds.Withdraw
                        );
                        break;
                    default:
                        throw new ArgumentException(
                            $"Unknown current state '{currentState}'."
                        );
                }
            }

            return eventKinds.Count == 0 ? null : eventKinds;
        }

        private static object EmptyResponse(PermissionRecordsListQuery query) =>
            new
            {
                success = true,
                page = query.Page,
                pageSize = query.PageSize,
                totalCount = 0,
                rows = Array.Empty<object>(),
            };

        private sealed class PermissionRecordRowProjection
        {
            public int Id { get; init; }

            public int LocationGuestId { get; init; }

            public int LocationId { get; init; }

            public string GuestName { get; init; } = string.Empty;

            public LocationGuestPermissionKind PermissionKind { get; init; }

            public string EventKind { get; init; } = string.Empty;

            public string LocationName { get; init; } = string.Empty;

            public string Source { get; init; } = string.Empty;

            public DateTime OccurredAt { get; init; }
        }
    }
}
