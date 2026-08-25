using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Capture Archive list module — SQL-composed filters/sort/page (ADR-0024).
    /// </summary>
    public class CaptureArchiveListService : ICaptureArchiveListService
    {
        public const int DefaultPageSize = 25;

        private static readonly HashSet<string> ValidSorts =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "recently-archived",
                "oldest-archived",
                "placement-name-az",
                "highest-qr-scans",
                "highest-feedback",
                "most-recent-activity",
            };

        private static readonly Dictionary<QrType, string> QrTypeLabels =
            new()
            {
                [QrType.CounterCard] = "Counter card",
                [QrType.PackagingSticker] = "Packaging sticker",
                [QrType.DeliveryInsert] = "Delivery insert",
                [QrType.WindowSticker] = "Window sticker",
                [QrType.SmartGuest] = "Smart Guest",
                [QrType.DigitalGuestLink] = "Digital guest link",
            };

        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;

        public CaptureArchiveListService(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
        }

        public async Task<object> ListAsync(CaptureArchiveListQuery query)
        {
            ValidateQuery(query);

            var normalizedSort = NormalizeSort(query.Sort);
            var normalizedQuery = query.Q?.Trim() ?? string.Empty;
            var page = query.Page;
            var pageSize = query.PageSize;

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == query.RestaurantId);

            if (restaurant == null)
            {
                return EmptyPage(page, pageSize);
            }

            var ownedLocationIds = query.ScopedLocationIds.ToList();

            if (ownedLocationIds.Count == 0)
            {
                return EmptyPage(page, pageSize);
            }

            var archiverOptions = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    ownedLocationIds.Contains(q.RestaurantLocationId)
                    && q.Status == QrCodeStatus.Archived
                    && q.ArchivedByDisplayName != null
                    && q.ArchivedByDisplayName != ""
                )
                .Select(q => q.ArchivedByDisplayName!)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            var locationNames = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l => ownedLocationIds.Contains(l.Id))
                .Select(l => new { l.Id, l.LocationName })
                .ToDictionaryAsync(l => l.Id, l => l.LocationName);

            var scopedLocationIds = ResolveScopedLocationIds(
                ownedLocationIds,
                query.LocationIds
            );

            if (scopedLocationIds.Count == 0)
            {
                return PagePayload(
                    Array.Empty<object>(),
                    totalCount: 0,
                    page,
                    pageSize,
                    archiverOptions
                );
            }

            var filtered = BuildFilteredQuery(
                scopedLocationIds,
                normalizedQuery,
                query.QrTypes,
                query.DatePreset,
                query.DateFrom,
                query.DateTo,
                query.UtcOffsetMinutes,
                query.ArchivedBy,
                locationNames
            );

            var totalCount = await filtered.CountAsync();

            var orderedIds = await ApplySort(filtered, normalizedSort)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (orderedIds.Count == 0)
            {
                return PagePayload(
                    Array.Empty<object>(),
                    totalCount,
                    page,
                    pageSize,
                    archiverOptions
                );
            }

            var pageCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q => orderedIds.Contains(q.Id))
                .ToListAsync();
            var byId = pageCodes.ToDictionary(q => q.Id);

            var scanCounts = await _context.QrScanEvents
                .AsNoTracking()
                .Where(e =>
                    e.QrCodeId != null && orderedIds.Contains(e.QrCodeId.Value)
                )
                .GroupBy(e => e.QrCodeId!.Value)
                .Select(g => new { QrCodeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.QrCodeId, x => x.Count);

            var feedbackCounts = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => orderedIds.Contains(f.QrCodeId))
                .GroupBy(f => f.QrCodeId)
                .Select(g => new { QrCodeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.QrCodeId, x => x.Count);

            var lastScans = await _context.QrScanEvents
                .AsNoTracking()
                .Where(e =>
                    e.QrCodeId != null && orderedIds.Contains(e.QrCodeId.Value)
                )
                .GroupBy(e => e.QrCodeId!.Value)
                .Select(g => new
                {
                    QrCodeId = g.Key,
                    LastScanAt = g.Max(e => e.CreatedAt),
                })
                .ToDictionaryAsync(x => x.QrCodeId, x => x.LastScanAt);

            var liveCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    ownedLocationIds.Contains(q.RestaurantLocationId)
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .Select(q => new
                {
                    q.RestaurantLocationId,
                    q.QrType,
                    q.NormalizedLinkName,
                })
                .ToListAsync();

            var occupiedCatalogSlots = liveCodes
                .Where(q => q.QrType != QrType.DigitalGuestLink)
                .Select(q => (q.RestaurantLocationId, q.QrType))
                .ToHashSet();

            var occupiedLinkNames = liveCodes
                .Where(q =>
                    q.QrType == QrType.DigitalGuestLink
                    && q.NormalizedLinkName != null
                )
                .Select(q => (q.RestaurantLocationId, q.NormalizedLinkName!))
                .ToHashSet();

            var placements = orderedIds
                .Select(id =>
                {
                    var qr = byId[id];
                    var canRestore = qr.QrType == QrType.DigitalGuestLink
                        ? qr.NormalizedLinkName == null
                            || !occupiedLinkNames.Contains(
                                (qr.RestaurantLocationId, qr.NormalizedLinkName)
                            )
                        : !occupiedCatalogSlots.Contains(
                            (qr.RestaurantLocationId, qr.QrType)
                        );

                    DateTime? lastScanAt = lastScans.TryGetValue(
                        qr.Id,
                        out var scannedAt
                    )
                        ? scannedAt
                        : null;

                    return (object)new
                    {
                        qrCodeId = qr.Id,
                        locationId = qr.RestaurantLocationId,
                        locationName = locationNames.GetValueOrDefault(
                            qr.RestaurantLocationId,
                            string.Empty
                        ),
                        qrType = qr.QrType.ToString(),
                        status = qr.Status.ToString(),
                        linkName = qr.LinkName,
                        channel = qr.Channel?.ToString(),
                        internalDescription = qr.InternalDescription,
                        qrLinkUrl = _smartGuestLink.BuildGuestUrl(qr.Token),
                        archivedAt = qr.ArchivedAt,
                        archivedByDisplayName = qr.ArchivedByDisplayName,
                        qrScans = scanCounts.GetValueOrDefault(qr.Id),
                        feedbackSubmitted = feedbackCounts.GetValueOrDefault(
                            qr.Id
                        ),
                        lastScanAt,
                        canRestore,
                    };
                })
                .ToList();

            return PagePayload(
                placements,
                totalCount,
                page,
                pageSize,
                archiverOptions
            );
        }

        private IQueryable<QrCode> BuildFilteredQuery(
            IReadOnlyList<int> locationIds,
            string normalizedQuery,
            string[]? qrTypes,
            string? datePreset,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? utcOffsetMinutes,
            string[]? archivedBy,
            IReadOnlyDictionary<int, string> locationNames
        )
        {
            var filtered = _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    locationIds.Contains(q.RestaurantLocationId)
                    && q.Status == QrCodeStatus.Archived
                );

            var typeFilters = ParseQrTypes(qrTypes);
            if (typeFilters.Count > 0)
            {
                filtered = filtered.Where(q => typeFilters.Contains(q.QrType));
            }

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                filtered = ApplySearch(
                    filtered,
                    normalizedQuery,
                    locationIds,
                    locationNames
                );
            }

            var dateWindow = ResolveArchivedDateWindow(
                datePreset,
                dateFrom,
                dateTo,
                utcOffsetMinutes
            );
            if (dateWindow != null)
            {
                var fromUtc = dateWindow.Value.FromUtc;
                var toUtc = dateWindow.Value.ToUtc;
                filtered = filtered.Where(q =>
                    q.ArchivedAt != null
                    && q.ArchivedAt >= fromUtc
                    && q.ArchivedAt < toUtc
                );
            }

            var archivers = (archivedBy ?? Array.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (archivers.Count > 0)
            {
                filtered = filtered.Where(q =>
                    q.ArchivedByDisplayName != null
                    && archivers.Contains(q.ArchivedByDisplayName)
                );
            }

            return filtered;
        }

        private IQueryable<QrCode> ApplySearch(
            IQueryable<QrCode> query,
            string normalizedQuery,
            IReadOnlyList<int> locationIds,
            IReadOnlyDictionary<int, string> locationNames
        )
        {
            var term = normalizedQuery.ToLowerInvariant();

            var matchingLocationIds = locationNames
                .Where(kv =>
                    locationIds.Contains(kv.Key)
                    && kv.Value.ToLowerInvariant().Contains(term)
                )
                .Select(kv => kv.Key)
                .ToList();

            var matchingTypes = QrTypeLabels
                .Where(kv =>
                    kv.Value.ToLowerInvariant().Contains(term)
                    || kv.Key.ToString().ToLowerInvariant().Contains(term)
                )
                .Select(kv => kv.Key)
                .ToList();

            return query.Where(q =>
                matchingLocationIds.Contains(q.RestaurantLocationId)
                || matchingTypes.Contains(q.QrType)
                || (q.LinkName != null && q.LinkName.ToLower().Contains(term))
                || (
                    q.ArchivedByDisplayName != null
                    && q.ArchivedByDisplayName.ToLower().Contains(term)
                )
            );
        }

        private IQueryable<int> ApplySort(
            IQueryable<QrCode> query,
            string sort
        )
        {
            var labeled = query.Select(q => new
            {
                q.Id,
                q.ArchivedAt,
                Label = q.QrType == QrType.DigitalGuestLink
                    ? (q.LinkName ?? string.Empty).ToLower()
                    : q.QrType == QrType.CounterCard
                        ? "counter card"
                        : q.QrType == QrType.PackagingSticker
                            ? "packaging sticker"
                            : q.QrType == QrType.DeliveryInsert
                                ? "delivery insert"
                                : q.QrType == QrType.WindowSticker
                                    ? "window sticker"
                                    : q.QrType == QrType.SmartGuest
                                        ? "smart guest"
                                        : "digital guest link",
                ScanCount = _context.QrScanEvents.Count(e =>
                    e.QrCodeId == q.Id
                ),
                FeedbackCount = _context.Feedbacks.Count(f =>
                    f.QrCodeId == q.Id
                ),
                LastScanAt = _context.QrScanEvents
                    .Where(e => e.QrCodeId == q.Id)
                    .Select(e => (DateTime?)e.CreatedAt)
                    .Max(),
            });

            return sort switch
            {
                "oldest-archived" => labeled
                    .OrderBy(row => row.ArchivedAt)
                    .ThenBy(row => row.Id)
                    .Select(row => row.Id),
                "placement-name-az" => labeled
                    .OrderBy(row => row.Label)
                    .ThenBy(row => row.Id)
                    .Select(row => row.Id),
                "highest-qr-scans" => labeled
                    .OrderByDescending(row => row.ScanCount)
                    .ThenBy(row => row.Label)
                    .ThenBy(row => row.Id)
                    .Select(row => row.Id),
                "highest-feedback" => labeled
                    .OrderByDescending(row => row.FeedbackCount)
                    .ThenBy(row => row.Label)
                    .ThenBy(row => row.Id)
                    .Select(row => row.Id),
                "most-recent-activity" => labeled
                    .OrderByDescending(row =>
                        row.LastScanAt ?? DateTime.MinValue
                    )
                    .ThenBy(row => row.Label)
                    .ThenBy(row => row.Id)
                    .Select(row => row.Id),
                _ => labeled
                    .OrderByDescending(row => row.ArchivedAt)
                    .ThenByDescending(row => row.Id)
                    .Select(row => row.Id),
            };
        }

        private static List<int> ResolveScopedLocationIds(
            IReadOnlyList<int> ownedLocationIds,
            int[]? requested
        )
        {
            if (requested is not { Length: > 0 })
            {
                return ownedLocationIds.ToList();
            }

            var owned = ownedLocationIds.ToHashSet();
            if (requested.Any(id => !owned.Contains(id)))
            {
                throw new InvalidOperationException("location-scope-denied");
            }

            return requested.Distinct().ToList();
        }

        private static List<QrType> ParseQrTypes(string[]? qrTypes)
        {
            if (qrTypes is not { Length: > 0 })
            {
                return [];
            }

            var result = new List<QrType>();
            foreach (var raw in qrTypes)
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
                    if (!Enum.TryParse<QrType>(part, ignoreCase: false, out var parsed)
                        && !Enum.TryParse<QrType>(
                            part,
                            ignoreCase: true,
                            out parsed
                        ))
                    {
                        throw new ArgumentException("Invalid qrTypes value.");
                    }

                    if (!result.Contains(parsed))
                    {
                        result.Add(parsed);
                    }
                }
            }

            return result;
        }

        private static (DateTime FromUtc, DateTime ToUtc)? ResolveArchivedDateWindow(
            string? datePreset,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? utcOffsetMinutes
        )
        {
            var preset = datePreset?.Trim();
            if (
                string.IsNullOrWhiteSpace(preset)
                || preset.Equals("any-time", StringComparison.OrdinalIgnoreCase)
            )
            {
                if (dateFrom != null || dateTo != null)
                {
                    throw new ArgumentException(
                        "dateFrom/dateTo require datePreset=custom."
                    );
                }

                return null;
            }

            if (preset.Equals("custom", StringComparison.OrdinalIgnoreCase))
            {
                if (dateFrom == null || dateTo == null)
                {
                    throw new ArgumentException(
                        "dateFrom and dateTo are both required for a custom range."
                    );
                }

                return GuestsDateWindows.ResolveCustom(
                    dateFrom.Value,
                    dateTo.Value
                );
            }

            if (!GuestsDateWindows.IsValidTablePreset(preset))
            {
                throw new ArgumentException("Invalid datePreset.");
            }

            if (utcOffsetMinutes == null)
            {
                throw new ArgumentException(
                    "utcOffsetMinutes is required when using a datePreset."
                );
            }

            return GuestsDateWindows.ResolvePreset(
                preset,
                DateTime.UtcNow,
                utcOffsetMinutes.Value
            );
        }

        private static void ValidateQuery(CaptureArchiveListQuery query)
        {
            if (query.Page < 1)
            {
                throw new ArgumentException("page must be at least 1.");
            }

            if (query.PageSize != DefaultPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be {DefaultPageSize}."
                );
            }

            if (!ValidSorts.Contains(query.Sort))
            {
                throw new ArgumentException("Invalid sort.");
            }

            // Fail closed on qrTypes early when provided.
            _ = ParseQrTypes(query.QrTypes);
        }

        private static string NormalizeSort(string sort)
        {
            return ValidSorts.Single(option =>
                option.Equals(sort, StringComparison.OrdinalIgnoreCase)
            );
        }

        private static object EmptyPage(int page, int pageSize)
        {
            return PagePayload(
                Array.Empty<object>(),
                totalCount: 0,
                page,
                pageSize,
                Array.Empty<string>()
            );
        }

        private static object PagePayload(
            IReadOnlyList<object> placements,
            int totalCount,
            int page,
            int pageSize,
            IReadOnlyList<string> archiverOptions
        )
        {
            return new
            {
                success = true,
                placements,
                totalCount,
                page,
                pageSize,
                archiverOptions,
            };
        }
    }
}
