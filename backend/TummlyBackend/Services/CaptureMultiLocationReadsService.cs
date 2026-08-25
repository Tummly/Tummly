using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class CaptureMultiLocationReadsService
        : ICaptureMultiLocationReadsService
    {
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
        private readonly CaptureWindowedEngagementAggregate _engagement;

        public CaptureMultiLocationReadsService(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate engagement
        )
        {
            _context = context;
            _engagement = engagement;
        }

        public async Task<object> GetOverviewAsync(CaptureOverviewQuery query)
        {
            var (fromUtc, toUtc) = CaptureDateWindows.Resolve(query.From, query.To);

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == query.RestaurantId);

            if (restaurant == null)
            {
                return EmptyOverview();
            }

            var ownedLocationIds = query.ScopedLocationIds.ToList();

            // Capture location status: count only Active Owned locations.
            var totalLocations = ownedLocationIds.Count;
            var activeLocations = ownedLocationIds.Count == 0
                ? 0
                : await _context.RestaurantLocations
                    .CountAsync(l =>
                        ownedLocationIds.Contains(l.Id)
                        && l.CaptureLocationStatus
                            == CaptureLocationStatus.Active
                    );

            var activeQrPlacements = ownedLocationIds.Count == 0
                ? 0
                : await _context.QrCodes
                    .CountAsync(q =>
                        ownedLocationIds.Contains(q.RestaurantLocationId)
                        && q.Status == QrCodeStatus.Active
                    );

            if (ownedLocationIds.Count == 0)
            {
                return OverviewPayload(
                    activeLocations,
                    totalLocations,
                    activeQrPlacements,
                    qrScans: 0,
                    qrScansPrevious: 0,
                    feedbackSubmitted: 0,
                    feedbackSubmittedPrevious: 0,
                    marketingOptIns: 0,
                    marketingOptInsPrevious: 0
                );
            }

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var activeOrPausedQrCodeIds =
                await _engagement.ListActiveOrPausedQrCodeIdsAsync(
                    ownedLocationIds
                );

            var qrScans = await _engagement.CountScansAsync(
                ownedLocationIds,
                activeOrPausedQrCodeIds,
                fromUtc,
                toUtc
            );
            var qrScansPrevious = await _engagement.CountScansAsync(
                ownedLocationIds,
                activeOrPausedQrCodeIds,
                previousFromUtc,
                previousToUtc
            );

            var feedbackSubmitted = await _engagement.CountFeedbackAsync(
                ownedLocationIds,
                activeOrPausedQrCodeIds,
                fromUtc,
                toUtc,
                marketingOptInOnly: false
            );
            var feedbackSubmittedPrevious =
                await _engagement.CountFeedbackAsync(
                    ownedLocationIds,
                    activeOrPausedQrCodeIds,
                    previousFromUtc,
                    previousToUtc,
                    marketingOptInOnly: false
                );

            var marketingOptIns = await _engagement.CountFeedbackAsync(
                ownedLocationIds,
                activeOrPausedQrCodeIds,
                fromUtc,
                toUtc,
                marketingOptInOnly: true
            );
            var marketingOptInsPrevious = await _engagement.CountFeedbackAsync(
                ownedLocationIds,
                activeOrPausedQrCodeIds,
                previousFromUtc,
                previousToUtc,
                marketingOptInOnly: true
            );

            return OverviewPayload(
                activeLocations,
                totalLocations,
                activeQrPlacements,
                qrScans,
                qrScansPrevious,
                feedbackSubmitted,
                feedbackSubmittedPrevious,
                marketingOptIns,
                marketingOptInsPrevious
            );
        }

        public async Task<object> GetLocationsAsync(CaptureLocationsQuery query)
        {
            var (fromUtc, toUtc) = CaptureDateWindows.Resolve(query.From, query.To);

            if (query.Page < 1)
            {
                throw new ArgumentException("page must be at least 1.");
            }

            // Wire accepts pageSize but Location performance is frozen at 20.
            var pageSize = DefaultPageSize;
            _ = query.PageSize;

            var sortKey = string.IsNullOrWhiteSpace(query.Sort)
                ? DefaultSort
                : query.Sort.Trim();
            if (!AllowedSorts.Contains(sortKey))
            {
                sortKey = DefaultSort;
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == query.RestaurantId);

            if (restaurant == null)
            {
                return EmptyPage(query.Page, pageSize);
            }

            var ownedLocationIds = query.ScopedLocationIds.ToList();

            if (ownedLocationIds.Count == 0)
            {
                return EmptyPage(query.Page, pageSize);
            }

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => ownedLocationIds.Contains(l.Id))
                .Select(l => new LocationSeed(
                    l.Id,
                    l.LocationName,
                    l.CaptureLocationStatus,
                    l.CaptureLocationPauseRestoreQrCodeIdsJson
                ))
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var needle = query.Q.Trim();
                locations = locations
                    .Where(l =>
                        l.LocationName.Contains(
                            needle,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();
            }

            if (query.LocationIds is { Length: > 0 })
            {
                if (query.LocationIds.Any(id => !ownedLocationIds.Contains(id)))
                {
                    throw new InvalidOperationException(
                        "location-scope-denied"
                    );
                }

                var allowed = query.LocationIds.ToHashSet();
                locations = locations
                    .Where(l => allowed.Contains(l.LocationId))
                    .ToList();
            }

            var statusFilters = NormalizeStatuses(query.Status);
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
                return EmptyPage(query.Page, pageSize);
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

            var activeOrPausedQrCodeIds =
                await _engagement.ListActiveOrPausedQrCodeIdsAsync(filteredIds);

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
                        PauseRestoreQrCodeCount: CaptureLocationPauseRestore.Count(
                            location.CaptureLocationPauseRestoreQrCodeIdsJson
                        ),
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
                .Skip((query.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new
                {
                    locationId = row.LocationId,
                    locationName = row.LocationName,
                    status = row.Status,
                    activePlacementsCount = row.ActivePlacementsCount,
                    pauseRestoreQrCodeCount = row.PauseRestoreQrCodeCount,
                    qrScans = row.QrScans,
                    feedbackSubmitted = row.FeedbackSubmitted,
                    marketingOptIns = row.MarketingOptIns,
                    offerClaims = row.OfferClaims,
                    lastActivityAt = row.LastActivityAt
                })
                .ToList();

            return new
            {
                success = true,
                items = pageItems,
                totalCount,
                page = query.Page,
                pageSize
            };
        }

        private static object EmptyOverview()
        {
            return OverviewPayload(
                activeLocations: 0,
                totalLocations: 0,
                activeQrPlacements: 0,
                qrScans: 0,
                qrScansPrevious: 0,
                feedbackSubmitted: 0,
                feedbackSubmittedPrevious: 0,
                marketingOptIns: 0,
                marketingOptInsPrevious: 0
            );
        }

        private static object OverviewPayload(
            int activeLocations,
            int totalLocations,
            int activeQrPlacements,
            int qrScans,
            int qrScansPrevious,
            int feedbackSubmitted,
            int feedbackSubmittedPrevious,
            int marketingOptIns,
            int marketingOptInsPrevious
        )
        {
            return new
            {
                success = true,
                activeLocations,
                totalLocations,
                activeQrPlacements,
                qrScans,
                qrScansPrevious,
                feedbackSubmitted,
                feedbackSubmittedPrevious,
                marketingOptIns,
                marketingOptInsPrevious,
                offerClaims = 0,
                offerClaimsHasRealData = false
            };
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

        private sealed record LocationSeed(
            int LocationId,
            string LocationName,
            CaptureLocationStatus CaptureLocationStatus,
            string? CaptureLocationPauseRestoreQrCodeIdsJson
        );

        private sealed record LocationRow(
            int LocationId,
            string LocationName,
            string Status,
            int ActivePlacementsCount,
            int PauseRestoreQrCodeCount,
            int QrScans,
            int FeedbackSubmitted,
            int MarketingOptIns,
            int OfferClaims,
            DateTime? LastActivityAt
        );
    }
}
