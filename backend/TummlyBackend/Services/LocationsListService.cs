using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class LocationsListService : ILocationsListService
    {
        private static readonly HashSet<string> AllowedLifecycle =
        [
            "active",
            "draft",
            "paused",
            "archived",
        ];

        private static readonly HashSet<string> AllowedSetup =
        [
            "ready",
            "needs-attention",
            "not-started",
            "blocked",
        ];

        private static readonly HashSet<string> AllowedSort =
        [
            "name-asc",
            "name-desc",
        ];

        private readonly ApplicationDbContext _context;

        public LocationsListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetListAsync(LocationsListQuery query)
        {
            ValidateQuery(query);

            var privacyReadyAt = await _context.Restaurants
                .AsNoTracking()
                .Where(r => r.Id == query.RestaurantId)
                .Select(r => r.PrivacyConsentReadyAt)
                .FirstOrDefaultAsync();

            var privacyReady = privacyReadyAt != null;

            var scopedIds = query.LocationIds.ToHashSet();

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l =>
                    l.RestaurantId == query.RestaurantId
                    && scopedIds.Contains(l.Id)
                )
                .Select(l => new LocationRowSource(
                    l.Id,
                    l.LocationName,
                    l.LifecycleStatus,
                    l.City,
                    l.Postcode,
                    l.ManagerUserId,
                    l.ManagerUser != null ? l.ManagerUser.FullName : null
                ))
                .ToListAsync();

            var locationIds = locations.Select(l => l.Id).ToList();

            var activeQrLocationIds = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    locationIds.Contains(q.RestaurantLocationId)
                    && q.Status == QrCodeStatus.Active
                )
                .Select(q => q.RestaurantLocationId)
                .Distinct()
                .ToListAsync();
            var activeQrSet = activeQrLocationIds.ToHashSet();

            var lastActivityByLocation = await (
                from e in _context.LocationGuestActivityEvents.AsNoTracking()
                join lg in _context.LocationGuests.AsNoTracking()
                    on e.LocationGuestId equals lg.Id
                where locationIds.Contains(lg.RestaurantLocationId)
                group e by lg.RestaurantLocationId
                into g
                select new
                {
                    LocationId = g.Key,
                    OccurredAt = g.Max(x => x.OccurredAt),
                }
            ).ToDictionaryAsync(x => x.LocationId, x => x.OccurredAt);

            var projected = locations
                .Select(l =>
                {
                    var setup = DeriveSetupStatus(
                        l.LifecycleStatus,
                        activeQrSet.Contains(l.Id),
                        privacyReady
                    );
                    var city = NormalizeCity(l.City);
                    var cityId = CityIdFrom(city);
                    var postcode = string.IsNullOrWhiteSpace(l.Postcode)
                        ? null
                        : l.Postcode.Trim();
                    lastActivityByLocation.TryGetValue(
                        l.Id,
                        out var lastAt
                    );
                    return new ProjectedRow(
                        l.Id,
                        l.LocationName,
                        ToLifecycleWire(l.LifecycleStatus),
                        setup,
                        string.IsNullOrWhiteSpace(l.ManagerName)
                            ? null
                            : l.ManagerName.Trim(),
                        l.ManagerUserId,
                        city,
                        postcode,
                        cityId,
                        ComposeCityPostcode(city, postcode),
                        lastAt == default
                            ? null
                            : lastAt.ToUniversalTime().ToString("O"),
                        BuildSearchText(
                            l.LocationName,
                            city,
                            postcode
                        )
                    );
                })
                .ToList();

            var kpis = new
            {
                active = projected.Count(r => r.LifecycleStatus == "active"),
                draft = projected.Count(r => r.LifecycleStatus == "draft"),
                paused = projected.Count(r => r.LifecycleStatus == "paused"),
                setupNeedsAttention = projected.Count(r =>
                    r.SetupStatus == "needs-attention"
                ),
            };

            var cityFacets = projected
                .Where(r => !string.IsNullOrEmpty(r.CityId))
                .GroupBy(r => r.CityId)
                .Select(g => new
                {
                    id = g.Key,
                    label = g.First().City!,
                })
                .OrderBy(f => f.label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var filtered = projected.AsEnumerable();

            var q = (query.Q ?? string.Empty).Trim();
            if (q.Length > 0)
            {
                var needle = q.ToLowerInvariant();
                filtered = filtered.Where(r =>
                    r.SearchText.Contains(
                        needle,
                        StringComparison.Ordinal
                    )
                );
            }

            if (query.Lifecycle.Length > 0)
            {
                var set = query.Lifecycle
                    .Select(s => s.Trim().ToLowerInvariant())
                    .ToHashSet();
                filtered = filtered.Where(r =>
                    set.Contains(r.LifecycleStatus)
                );
            }

            if (query.Setup.Length > 0)
            {
                var set = query.Setup
                    .Select(s => s.Trim().ToLowerInvariant())
                    .ToHashSet();
                filtered = filtered.Where(r => set.Contains(r.SetupStatus));
            }

            if (query.City.Length > 0)
            {
                var set = query.City
                    .Select(s => s.Trim().ToLowerInvariant())
                    .ToHashSet();
                filtered = filtered.Where(r =>
                    !string.IsNullOrEmpty(r.CityId) && set.Contains(r.CityId)
                );
            }

            var sorted = query.Sort == "name-desc"
                ? filtered
                    .OrderByDescending(
                        r => r.Name,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ThenByDescending(r => r.Id)
                    .ToList()
                : filtered
                    .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => r.Id)
                    .ToList();

            var totalCount = sorted.Count;
            var pageSize = query.PageSize;
            var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            var page = Math.Min(Math.Max(1, query.Page), maxPage);
            var rows = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    lifecycleStatus = r.LifecycleStatus,
                    setupStatus = r.SetupStatus,
                    managerName = r.ManagerName,
                    managerUserId = r.ManagerUserId,
                    city = r.City,
                    postcode = r.Postcode,
                    cityId = r.CityId,
                    cityPostcode = r.CityPostcode,
                    lastActivityAt = r.LastActivityAt,
                    searchText = r.SearchText,
                })
                .ToList();

            return new
            {
                success = true,
                rows,
                totalCount,
                page,
                pageSize,
                kpis,
                cityFacets,
            };
        }

        private static void ValidateQuery(LocationsListQuery query)
        {
            if (query.Page < 1)
            {
                throw new ArgumentException("page must be >= 1.");
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                throw new ArgumentException(
                    "pageSize must be between 1 and 100."
                );
            }

            if (!AllowedSort.Contains(query.Sort))
            {
                throw new ArgumentException(
                    "sort must be name-asc or name-desc."
                );
            }

            foreach (var value in query.Lifecycle)
            {
                var wire = value.Trim().ToLowerInvariant();
                if (!AllowedLifecycle.Contains(wire))
                {
                    throw new ArgumentException(
                        $"Invalid lifecycle filter '{value}'."
                    );
                }
            }

            foreach (var value in query.Setup)
            {
                var wire = value.Trim().ToLowerInvariant();
                if (!AllowedSetup.Contains(wire))
                {
                    throw new ArgumentException(
                        $"Invalid setup filter '{value}'."
                    );
                }
            }
        }

        internal static string DeriveSetupStatus(
            LocationLifecycleStatus lifecycle,
            bool hasActiveQr,
            bool privacyReady
        )
        {
            if (lifecycle == LocationLifecycleStatus.Draft)
            {
                return "not-started";
            }

            if (lifecycle == LocationLifecycleStatus.Archived)
            {
                return "ready";
            }

            // Active or Paused
            if (!hasActiveQr || !privacyReady)
            {
                return "needs-attention";
            }

            return "ready";
        }

        private static string ToLifecycleWire(LocationLifecycleStatus status) =>
            status switch
            {
                LocationLifecycleStatus.Draft => "draft",
                LocationLifecycleStatus.Active => "active",
                LocationLifecycleStatus.Paused => "paused",
                LocationLifecycleStatus.Archived => "archived",
                _ => "active",
            };

        private static string? NormalizeCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            return city.Trim();
        }

        private static string CityIdFrom(string? city)
        {
            if (city == null)
            {
                return string.Empty;
            }

            return city.ToLowerInvariant();
        }

        private static string ComposeCityPostcode(
            string? city,
            string? postcode
        )
        {
            if (city != null && postcode != null)
            {
                return $"{city}, {postcode}";
            }

            if (city != null)
            {
                return city;
            }

            if (postcode != null)
            {
                return postcode;
            }

            return "—";
        }

        private static string BuildSearchText(
            string name,
            string? city,
            string? postcode
        )
        {
            return string.Join(
                    " ",
                    new[] { name, city, postcode }.Where(s =>
                        !string.IsNullOrWhiteSpace(s)
                    )
                )
                .ToLowerInvariant();
        }

        private sealed record LocationRowSource(
            int Id,
            string LocationName,
            LocationLifecycleStatus LifecycleStatus,
            string? City,
            string? Postcode,
            int? ManagerUserId,
            string? ManagerName
        );

        private sealed record ProjectedRow(
            int Id,
            string Name,
            string LifecycleStatus,
            string SetupStatus,
            string? ManagerName,
            int? ManagerUserId,
            string? City,
            string? Postcode,
            string CityId,
            string CityPostcode,
            string? LastActivityAt,
            string SearchText
        );
    }
}
