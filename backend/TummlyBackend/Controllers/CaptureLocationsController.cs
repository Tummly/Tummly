using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/capture/locations")]
    [Authorize]
    public class CaptureLocationsController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;
        private const int DefaultPageSize = 20;
        private const string DefaultSort = "highest-qr-scans";

        private static readonly HashSet<string> AllowedSorts = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "highest-qr-scans",
            "highest-submission-rate",
            "highest-marketing-opt-ins",
            "highest-offer-claims",
            "most-active-placements",
            "most-recent-activity",
            "location-name-az",
        };

        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;

        public CaptureLocationsController(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? q,
            [FromQuery] string[]? status,
            [FromQuery] int[]? locationIds,
            [FromQuery] string sort = DefaultSort,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (from == null || to == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from and to are required."
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to."
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days."
                });
            }

            if (page < 1)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "page must be at least 1."
                });
            }

            if (pageSize != DefaultPageSize)
            {
                pageSize = DefaultPageSize;
            }

            var sortKey = string.IsNullOrWhiteSpace(sort)
                ? DefaultSort
                : sort.Trim();
            if (!AllowedSorts.Contains(sortKey))
            {
                sortKey = DefaultSort;
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == userId);

            if (restaurant == null)
            {
                return Ok(EmptyPage(page, pageSize));
            }

            var ownedLocationIds =
                await _ownedLocation.ListOwnedLocationIdsAsync(
                    restaurant.Id,
                    userId
                );

            if (ownedLocationIds.Count == 0)
            {
                return Ok(EmptyPage(page, pageSize));
            }

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => ownedLocationIds.Contains(l.Id))
                .Select(l => new LocationSeed(
                    l.Id,
                    l.LocationName,
                    l.CaptureLocationStatus
                ))
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                locations = locations
                    .Where(l =>
                        l.LocationName.Contains(
                            needle,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();
            }

            if (locationIds is { Length: > 0 })
            {
                var allowed = locationIds
                    .Where(id => ownedLocationIds.Contains(id))
                    .ToHashSet();
                locations = locations
                    .Where(l => allowed.Contains(l.LocationId))
                    .ToList();
            }

            var statusFilters = NormalizeStatuses(status);
            if (statusFilters.Count > 0)
            {
                locations = locations
                    .Where(l =>
                        statusFilters.Contains(l.CaptureLocationStatus.ToString())
                    )
                    .ToList();
            }

            if (locations.Count == 0)
            {
                return Ok(EmptyPage(page, pageSize));
            }

            var filteredIds = locations.Select(l => l.LocationId).ToList();

            var activePlacementCounts = await _context.QrCodes
                .AsNoTracking()
                .Where(qr =>
                    filteredIds.Contains(qr.RestaurantLocationId)
                    && qr.Status == QrCodeStatus.Active
                )
                .GroupBy(qr => qr.RestaurantLocationId)
                .Select(g => new
                {
                    LocationId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.LocationId, x => x.Count);

            var activeOrPausedQrCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(qr =>
                    filteredIds.Contains(qr.RestaurantLocationId)
                    && (qr.Status == QrCodeStatus.Active
                        || qr.Status == QrCodeStatus.Paused)
                )
                .Select(qr => new
                {
                    qr.Id,
                    qr.RestaurantLocationId
                })
                .ToListAsync();

            var activeOrPausedQrCodeIds = activeOrPausedQrCodes
                .Select(qr => qr.Id)
                .ToList();

            var qrScansByLocation = new Dictionary<int, int>();
            var feedbackByLocation = new Dictionary<int, int>();
            var marketingByLocation = new Dictionary<int, int>();
            var lastScanByLocation = new Dictionary<int, DateTime>();
            var lastFeedbackByLocation = new Dictionary<int, DateTime>();

            if (activeOrPausedQrCodeIds.Count > 0)
            {
                var scanRows = await _context.QrScanEvents
                    .AsNoTracking()
                    .Where(e =>
                        filteredIds.Contains(e.RestaurantLocationId)
                        && e.QrCodeId != null
                        && activeOrPausedQrCodeIds.Contains(e.QrCodeId.Value)
                        && e.CreatedAt >= fromUtc
                        && e.CreatedAt < toUtc
                    )
                    .GroupBy(e => e.RestaurantLocationId)
                    .Select(g => new
                    {
                        LocationId = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                foreach (var row in scanRows)
                {
                    qrScansByLocation[row.LocationId] = row.Count;
                }

                var feedbackRows = await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        filteredIds.Contains(f.RestaurantLocationId)
                        && activeOrPausedQrCodeIds.Contains(f.QrCodeId)
                        && f.CreatedAt >= fromUtc
                        && f.CreatedAt < toUtc
                    )
                    .GroupBy(f => f.RestaurantLocationId)
                    .Select(g => new
                    {
                        LocationId = g.Key,
                        Count = g.Count(),
                        OptIns = g.Count(f => !f.OffersOptOut)
                    })
                    .ToListAsync();

                foreach (var row in feedbackRows)
                {
                    feedbackByLocation[row.LocationId] = row.Count;
                    marketingByLocation[row.LocationId] = row.OptIns;
                }

                var lastScans = await _context.QrScanEvents
                    .AsNoTracking()
                    .Where(e =>
                        filteredIds.Contains(e.RestaurantLocationId)
                        && e.QrCodeId != null
                        && activeOrPausedQrCodeIds.Contains(e.QrCodeId.Value)
                    )
                    .GroupBy(e => e.RestaurantLocationId)
                    .Select(g => new
                    {
                        LocationId = g.Key,
                        LastAt = g.Max(e => e.CreatedAt)
                    })
                    .ToListAsync();

                foreach (var row in lastScans)
                {
                    lastScanByLocation[row.LocationId] = row.LastAt;
                }

                var lastFeedbacks = await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        filteredIds.Contains(f.RestaurantLocationId)
                        && activeOrPausedQrCodeIds.Contains(f.QrCodeId)
                    )
                    .GroupBy(f => f.RestaurantLocationId)
                    .Select(g => new
                    {
                        LocationId = g.Key,
                        LastAt = g.Max(f => f.CreatedAt)
                    })
                    .ToListAsync();

                foreach (var row in lastFeedbacks)
                {
                    lastFeedbackByLocation[row.LocationId] = row.LastAt;
                }
            }

            var rows = locations
                .Select(location =>
                {
                    qrScansByLocation.TryGetValue(
                        location.LocationId,
                        out var qrScans
                    );
                    feedbackByLocation.TryGetValue(
                        location.LocationId,
                        out var feedbackSubmitted
                    );
                    marketingByLocation.TryGetValue(
                        location.LocationId,
                        out var marketingOptIns
                    );
                    activePlacementCounts.TryGetValue(
                        location.LocationId,
                        out var activePlacements
                    );

                    DateTime? lastActivityAt = null;
                    lastScanByLocation.TryGetValue(
                        location.LocationId,
                        out var lastScan
                    );
                    lastFeedbackByLocation.TryGetValue(
                        location.LocationId,
                        out var lastFeedback
                    );
                    var hasScan = lastScanByLocation.ContainsKey(
                        location.LocationId
                    );
                    var hasFeedback = lastFeedbackByLocation.ContainsKey(
                        location.LocationId
                    );
                    if (hasScan && hasFeedback)
                    {
                        lastActivityAt = lastScan > lastFeedback
                            ? lastScan
                            : lastFeedback;
                    }
                    else if (hasScan)
                    {
                        lastActivityAt = lastScan;
                    }
                    else if (hasFeedback)
                    {
                        lastActivityAt = lastFeedback;
                    }

                    return new LocationRow(
                        location.LocationId,
                        location.LocationName,
                        Status: location.CaptureLocationStatus.ToString(),
                        ActivePlacementsCount: activePlacements,
                        QrScans: qrScans,
                        FeedbackSubmitted: feedbackSubmitted,
                        MarketingOptIns: marketingOptIns,
                        OfferClaims: 0,
                        LastActivityAt: lastActivityAt
                    );
                })
                .ToList();

            rows = SortRows(rows, sortKey);

            var totalCount = rows.Count;
            var pageItems = rows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new
                {
                    locationId = row.LocationId,
                    locationName = row.LocationName,
                    status = row.Status,
                    activePlacementsCount = row.ActivePlacementsCount,
                    qrScans = row.QrScans,
                    feedbackSubmitted = row.FeedbackSubmitted,
                    marketingOptIns = row.MarketingOptIns,
                    offerClaims = row.OfferClaims,
                    lastActivityAt = row.LastActivityAt
                })
                .ToList();

            return Ok(new
            {
                success = true,
                items = pageItems,
                totalCount,
                page,
                pageSize
            });
        }

        private static object EmptyPage(int page, int pageSize)
        {
            return new
            {
                success = true,
                items = Array.Empty<object>(),
                totalCount = 0,
                page,
                pageSize
            };
        }

        private static HashSet<string> NormalizeStatuses(string[]? status)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (status == null)
            {
                return result;
            }

            foreach (var raw in status)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                foreach (var part in raw.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries
                ))
                {
                    if (part.Equals("Active", StringComparison.OrdinalIgnoreCase)
                        || part.Equals(
                            "Paused",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        result.Add(
                            part.Equals(
                                "Active",
                                StringComparison.OrdinalIgnoreCase
                            )
                                ? "Active"
                                : "Paused"
                        );
                    }
                }
            }

            return result;
        }

        private static List<LocationRow> SortRows(
            List<LocationRow> rows,
            string sortKey
        )
        {
            IOrderedEnumerable<LocationRow> ordered = sortKey.ToLowerInvariant()
                switch
                {
                    "highest-submission-rate" => rows
                        // Defined rates first; 0-scan / undefined rates last.
                        .OrderByDescending(r => r.QrScans > 0)
                        .ThenByDescending(r =>
                            r.QrScans > 0
                                ? (double)r.FeedbackSubmitted / r.QrScans
                                : 0d
                        )
                        .ThenBy(r => r.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.LocationId),
                    "highest-marketing-opt-ins" => rows
                        .OrderByDescending(r => r.MarketingOptIns)
                        .ThenBy(r => r.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.LocationId),
                    "highest-offer-claims" => rows
                        .OrderByDescending(r => r.OfferClaims)
                        .ThenBy(r => r.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.LocationId),
                    "most-active-placements" => rows
                        .OrderByDescending(r => r.ActivePlacementsCount)
                        .ThenBy(r => r.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.LocationId),
                    "most-recent-activity" => rows
                        .OrderByDescending(r => r.LastActivityAt.HasValue)
                        .ThenByDescending(r => r.LastActivityAt)
                        .ThenBy(r => r.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.LocationId),
                    "location-name-az" => rows
                        .OrderBy(r => r.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.LocationId),
                    _ => rows
                        .OrderByDescending(r => r.QrScans)
                        .ThenBy(r => r.LocationName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.LocationId),
                };

            return ordered.ToList();
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private sealed record LocationSeed(
            int LocationId,
            string LocationName,
            CaptureLocationStatus CaptureLocationStatus
        );

        private sealed record LocationRow(
            int LocationId,
            string LocationName,
            string Status,
            int ActivePlacementsCount,
            int QrScans,
            int FeedbackSubmitted,
            int MarketingOptIns,
            int OfferClaims,
            DateTime? LastActivityAt
        );
    }
}
