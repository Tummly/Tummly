using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestsListService : IGuestsListService, IGuestsExportService
    {
        public const int ExportSoftMaxRows = 10_000;

        private const int NewGuestDays = 13;
        private const int NewThisMonthDays = 30;
        private const int DormantDays = 90;

        private static readonly string[] ExportHeaders =
        [
            "Name",
            "Email",
            "Mobile",
            "Marketing status",
            "Location",
            "Latest feedback",
            "Feedback submissions",
            "Last interaction",
            "Last interaction at",
            "First captured",
            "Guest tags",
        ];

        private static readonly HashSet<string> ValidSmartGroups =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all-guests",
                "new-guests",
                "needs-recovery",
                "positive-feedback",
                "offer-not-redeemed",
                "recent-redeemers",
                "dormant-guests",
            };

        private static readonly HashSet<string> DeferredSmartGroups =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "needs-recovery",
                "offer-not-redeemed",
                "recent-redeemers",
            };

        private static readonly HashSet<string> ValidSorts =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "recent-activity",
                "newest-guests",
                "oldest-guests",
                "guest-name-az",
                "guest-name-za",
                "most-feedback-submissions",
                "most-recent-redemption",
            };

        private static readonly HashSet<string> ValidDateAxes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "first-captured",
                "last-interaction",
            };

        private readonly ApplicationDbContext _context;

        public GuestsListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetListAsync(GuestsListQuery query)
        {
            ValidateQuery(query);

            var normalizedSmartGroup = NormalizeSmartGroup(query.SmartGroup);
            var normalizedSort = NormalizeSort(query.Sort);
            var normalizedQuery = query.Q?.Trim() ?? string.Empty;
            var utcNow = DateTime.UtcNow;
            var newGuestCutoff = utcNow.AddDays(-NewGuestDays);
            var newThisMonthCutoff = utcNow.AddDays(-NewThisMonthDays);
            var dormantCutoff = utcNow.AddDays(-DormantDays);
            var locationIds = query.LocationIds.ToList();

            await EnsureTagsOwnedAsync(query.RestaurantId, query.TagIds);

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking(),
                locationIds
            );

            var overviewWindow = ResolveOverviewWindow(query, utcNow);
            var overviewQuery = scoped;
            if (overviewWindow != null)
            {
                overviewQuery = GuestsListQueryComposer.ApplyCapturedAtWindow(
                    overviewQuery,
                    overviewWindow.Value.FromUtc,
                    overviewWindow.Value.ToUtc
                );
            }

            var overview = new
            {
                totalGuests = await overviewQuery.CountAsync(),
                newThisMonth = await GuestsListQueryComposer
                    .WhereNewGuest(overviewQuery, newThisMonthCutoff)
                    .CountAsync(),
                marketingEligible = await GuestsListQueryComposer
                    .WhereMarketingEligible(overviewQuery)
                    .CountAsync(),
                needsRecovery = 0,
            };

            var smartGroupCounts = new Dictionary<string, int>
            {
                ["all-guests"] = await scoped.CountAsync(),
                ["new-guests"] = await GuestsListQueryComposer
                    .WhereNewGuest(scoped, newGuestCutoff)
                    .CountAsync(),
                ["needs-recovery"] = 0,
                ["positive-feedback"] = await GuestsListQueryComposer
                    .WherePositiveFeedback(scoped)
                    .CountAsync(),
                ["offer-not-redeemed"] = 0,
                ["recent-redeemers"] = 0,
                ["dormant-guests"] = await GuestsListQueryComposer
                    .WhereDormant(scoped, dormantCutoff)
                    .CountAsync(),
            };

            var filtered = BuildFilteredQuery(
                scoped,
                normalizedSmartGroup,
                normalizedQuery,
                query.Marketing,
                query.Contact,
                query.Sentiment,
                query.TagIds,
                query.DateAxis,
                query.DatePreset,
                query.DateFrom,
                query.DateTo,
                query.UtcOffsetMinutes,
                newGuestCutoff,
                dormantCutoff,
                utcNow
            );

            var totalFilteredCount = await filtered.CountAsync();
            var pageIds = await GuestsListQueryComposer
                .ApplySort(filtered, normalizedSort)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(lg => lg.Id)
                .ToListAsync();

            var shapedRows = await ShapeRowsAsync(
                pageIds,
                query.LocationNamesById
            );

            var pagedRows = shapedRows
                .Select(row => new
                {
                    id = row.LocationGuestId.ToString(),
                    name = row.Name,
                    email = row.Email,
                    mobile = row.Mobile,
                    marketingStatus = row.MarketingStatus,
                    locationName = row.LocationName,
                    latestFeedbackSentiment = row.LatestFeedbackSentiment,
                    feedbackSubmissionCount = row.FeedbackSubmissionCount,
                    lastInteractionLabel = "Feedback submitted",
                    lastInteractionAt = row.LastInteractionAt,
                    capturedAt = row.CapturedAt,
                    tagIds = row.TagIds.OrderBy(id => id).ToArray(),
                })
                .ToList();

            return new
            {
                success = true,
                locationId = query.ShellLocationId,
                smartGroup = normalizedSmartGroup,
                q = normalizedQuery,
                sort = normalizedSort,
                page = query.Page,
                pageSize = query.PageSize,
                totalFilteredCount,
                overview,
                smartGroupCounts,
                rows = pagedRows,
            };
        }

        public async Task<GuestsExportResult> ExportAsync(GuestsExportQuery query)
        {
            var isSelected = query.GuestIds != null;
            if (isSelected && query.GuestIds!.Count == 0)
            {
                throw new ArgumentException(
                    "guestIds is required for selected export."
                );
            }

            if (isSelected && query.GuestIds!.Count > ExportSoftMaxRows)
            {
                throw new ArgumentException(
                    "Export exceeds 10,000 rows. Narrow filters and try again."
                );
            }

            var utcNow = DateTime.UtcNow;
            List<ShapedGuestRow> exportRows;
            string scopeToken;

            if (isSelected)
            {
                var orderedIds = new List<int>();
                var seen = new HashSet<int>();
                foreach (var id in query.GuestIds!)
                {
                    if (seen.Add(id))
                    {
                        orderedIds.Add(id);
                    }
                }

                var guests = await _context.LocationGuests
                    .AsNoTracking()
                    .Include(lg => lg.MasterGuest)
                    .Include(lg => lg.RestaurantLocation)!
                        .ThenInclude(l => l!.Restaurant)
                    .Where(lg =>
                        orderedIds.Contains(lg.Id)
                        && lg.RestaurantLocation!.Restaurant!.OwnerUserId
                            == query.OwnerUserId
                    )
                    .ToListAsync();

                if (guests.Count != orderedIds.Count)
                {
                    throw new ArgumentException(
                        "One or more guest ids are invalid."
                    );
                }

                var locationIds = guests
                    .Select(g => g.RestaurantLocationId)
                    .Distinct()
                    .ToList();
                var locationNames = guests
                    .GroupBy(g => g.RestaurantLocationId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().RestaurantLocation!.LocationName
                    );

                var feedbackStats = await LoadFeedbackStatsForGuestsAsync(
                    orderedIds
                );
                var byId = guests.ToDictionary(g => g.Id);

                exportRows = orderedIds
                    .Select(id =>
                        ShapeRow(
                            byId[id],
                            locationNames,
                            feedbackStats,
                            new Dictionary<int, HashSet<int>>()
                        )
                    )
                    .ToList();

                scopeToken = locationIds.Count > 1
                    ? "multi"
                    : locationIds[0].ToString();
            }
            else
            {
                ValidateExportListQuery(query);

                var normalizedSmartGroup = NormalizeSmartGroup(query.SmartGroup);
                var normalizedSort = NormalizeSort(query.Sort);
                var normalizedQuery = query.Q?.Trim() ?? string.Empty;
                var newGuestCutoff = utcNow.AddDays(-NewGuestDays);
                var dormantCutoff = utcNow.AddDays(-DormantDays);
                var locationIds = query.LocationIds.ToList();

                await EnsureTagsOwnedAsync(query.RestaurantId, query.TagIds);

                var scoped = GuestsListQueryComposer.ScopeToLocations(
                    _context.LocationGuests.AsNoTracking(),
                    locationIds
                );

                var filtered = BuildFilteredQuery(
                    scoped,
                    normalizedSmartGroup,
                    normalizedQuery,
                    query.Marketing,
                    query.Contact,
                    query.Sentiment,
                    query.TagIds,
                    query.DateAxis,
                    query.DatePreset,
                    query.DateFrom,
                    query.DateTo,
                    query.UtcOffsetMinutes,
                    newGuestCutoff,
                    dormantCutoff,
                    utcNow
                );

                var filteredCount = await filtered.CountAsync();
                if (filteredCount > ExportSoftMaxRows)
                {
                    throw new ArgumentException(
                        "Export exceeds 10,000 rows. Narrow filters and try again."
                    );
                }

                var exportIds = await GuestsListQueryComposer
                    .ApplySort(filtered, normalizedSort)
                    .Select(lg => lg.Id)
                    .ToListAsync();

                exportRows = await ShapeRowsAsync(
                    exportIds,
                    query.LocationNamesById
                );
                scopeToken = query.LocationScopeToken;
            }

            if (exportRows.Count > ExportSoftMaxRows)
            {
                throw new ArgumentException(
                    "Export exceeds 10,000 rows. Narrow filters and try again."
                );
            }

            var tagLabels = await LoadTagDisplayNamesAsync(
                exportRows.Select(row => row.LocationGuestId).ToList()
            );

            var csvRows = exportRows
                .Select(row =>
                {
                    tagLabels.TryGetValue(row.LocationGuestId, out var tags);
                    return (IReadOnlyList<string>)
                    [
                        row.Name,
                        row.Email ?? string.Empty,
                        row.Mobile ?? string.Empty,
                        row.MarketingStatus,
                        row.LocationName,
                        FormatLatestFeedbackLabel(row.LatestFeedbackSentiment),
                        row.FeedbackSubmissionCount.ToString(),
                        "Feedback submitted",
                        FormatIsoUtc(row.LastInteractionAt),
                        FormatIsoUtc(row.CapturedAt),
                        tags == null
                            ? string.Empty
                            : string.Join(";", tags),
                    ];
                })
                .ToList();

            var content = Rfc4180Csv.WriteUtf8(ExportHeaders, csvRows);
            var selectedSegment = isSelected ? "-selected" : string.Empty;
            var stamp = utcNow.ToString("yyyyMMdd-HHmmss");

            return new GuestsExportResult
            {
                FileName =
                    $"tummly-guests{selectedSegment}-{scopeToken}-{stamp}Z.csv",
                ContentType = "text/csv",
                Content = content,
            };
        }

        private IQueryable<LocationGuest> BuildFilteredQuery(
            IQueryable<LocationGuest> scoped,
            string smartGroup,
            string normalizedQuery,
            IReadOnlyList<string> marketing,
            IReadOnlyList<string> contact,
            IReadOnlyList<string> sentiment,
            IReadOnlyList<int> tagIds,
            string? dateAxis,
            string? datePreset,
            DateTime? dateFrom,
            DateTime? dateTo,
            int utcOffsetMinutes,
            DateTime newGuestCutoff,
            DateTime dormantCutoff,
            DateTime utcNow
        )
        {
            var filtered = GuestsListQueryComposer.ApplySmartGroup(
                scoped,
                smartGroup,
                newGuestCutoff,
                dormantCutoff,
                DeferredSmartGroups
            );

            filtered = GuestsListQueryComposer.ApplySearch(
                filtered,
                normalizedQuery
            );

            filtered = GuestsListQueryComposer.ApplyMarketingFilter(
                filtered,
                marketing
            );
            filtered = GuestsListQueryComposer.ApplyContactFilter(
                filtered,
                contact
            );

            var sentimentEnums = ToSentimentEnums(sentiment);
            filtered = GuestsListQueryComposer.ApplySentimentFilter(
                filtered,
                sentimentEnums
            );
            filtered = GuestsListQueryComposer.ApplyTagFilter(filtered, tagIds);

            var tableWindow = ResolveTableDateWindow(
                dateAxis,
                datePreset,
                dateFrom,
                dateTo,
                utcOffsetMinutes,
                utcNow
            );
            if (tableWindow != null)
            {
                var axis = ValidDateAxes.Single(option =>
                    option.Equals(dateAxis, StringComparison.OrdinalIgnoreCase)
                );
                filtered = GuestsListQueryComposer.ApplyDateAxisFilter(
                    filtered,
                    axis,
                    tableWindow.Value.FromUtc,
                    tableWindow.Value.ToUtc
                );
            }

            return filtered;
        }

        private static List<FeedbackSentiment> ToSentimentEnums(
            IReadOnlyList<string> sentiment
        )
        {
            if (sentiment.Count == 0)
            {
                return [];
            }

            var normalized = GuestsFilterOptions.Normalize(
                sentiment,
                GuestsFilterOptions.Sentiment
            );
            var result = new List<FeedbackSentiment>(normalized.Count);
            foreach (var wire in normalized)
            {
                if (
                    !FeedbackClassificationMapping.TryParseWireSentiment(
                        wire,
                        out var parsed
                    )
                )
                {
                    throw new ArgumentException("Invalid sentiment value.");
                }

                result.Add(parsed);
            }

            return result;
        }

        private async Task EnsureTagsOwnedAsync(
            int restaurantId,
            IReadOnlyList<int> requestedTagIds
        )
        {
            if (requestedTagIds.Count == 0)
            {
                return;
            }

            var distinctTagIds = requestedTagIds.Distinct().ToList();
            var ownedCount = await _context.GuestTags
                .AsNoTracking()
                .CountAsync(t =>
                    t.RestaurantId == restaurantId
                    && distinctTagIds.Contains(t.Id)
                );

            if (ownedCount != distinctTagIds.Count)
            {
                throw new ArgumentException("One or more tag ids are invalid.");
            }
        }

        private async Task<List<ShapedGuestRow>> ShapeRowsAsync(
            IReadOnlyList<int> orderedIds,
            IReadOnlyDictionary<int, string> locationNamesById
        )
        {
            if (orderedIds.Count == 0)
            {
                return [];
            }

            var guests = await _context.LocationGuests
                .AsNoTracking()
                .Include(lg => lg.MasterGuest)
                .Where(lg => orderedIds.Contains(lg.Id))
                .ToListAsync();

            var byId = guests.ToDictionary(g => g.Id);
            var feedbackStats = await LoadFeedbackStatsForGuestsAsync(orderedIds);
            var tagMemberships = await LoadTagIdsForGuestsAsync(orderedIds);

            return orderedIds
                .Select(id =>
                    ShapeRow(
                        byId[id],
                        locationNamesById,
                        feedbackStats,
                        tagMemberships
                    )
                )
                .ToList();
        }

        private static ShapedGuestRow ShapeRow(
            LocationGuest locationGuest,
            IReadOnlyDictionary<int, string> locationNamesById,
            IReadOnlyDictionary<int, LocationGuestFeedbackStats> feedbackStats,
            IReadOnlyDictionary<int, HashSet<int>> tagMemberships
        )
        {
            var masterGuest = locationGuest.MasterGuest
                ?? throw new InvalidOperationException(
                    "Location guest is missing master guest."
                );

            feedbackStats.TryGetValue(locationGuest.Id, out var stats);
            tagMemberships.TryGetValue(locationGuest.Id, out var tags);

            var latestFeedbackSentiment = stats?.LatestFeedbackSentiment ?? "none";
            var lastInteractionAt = stats?.LastInteractionAt;

            if (
                !locationNamesById.TryGetValue(
                    locationGuest.RestaurantLocationId,
                    out var locationName
                )
            )
            {
                locationName = string.Empty;
            }

            return new ShapedGuestRow
            {
                LocationGuestId = locationGuest.Id,
                Name = locationGuest.Name,
                Email = masterGuest.Email,
                Mobile = masterGuest.Mobile,
                LocationName = locationName,
                CapturedAt = locationGuest.CreatedAt,
                FeedbackSubmissionCount = stats?.FeedbackSubmissionCount ?? 0,
                LatestFeedbackSentiment = latestFeedbackSentiment,
                LastInteractionAt = lastInteractionAt,
                MarketingStatus = LocationGuestProjections.DeriveMarketingStatus(
                    locationGuest.OffersOptOut,
                    masterGuest.Email,
                    masterGuest.Mobile
                ),
                TagIds = tags ?? new HashSet<int>(),
            };
        }

        private async Task<Dictionary<int, LocationGuestFeedbackStats>>
            LoadFeedbackStatsForGuestsAsync(IReadOnlyList<int> locationGuestIds)
        {
            if (locationGuestIds.Count == 0)
            {
                return new Dictionary<int, LocationGuestFeedbackStats>();
            }

            var ids = locationGuestIds as List<int> ?? locationGuestIds.ToList();
            var facts = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.LocationGuestId != null
                    && ids.Contains(f.LocationGuestId.Value)
                )
                .Select(f => new LocationGuestScopedFeedbackFact(
                    f.LocationGuestId!.Value,
                    f.CreatedAt,
                    f.ClassificationStatus,
                    f.Sentiment
                ))
                .ToListAsync();

            return facts
                .GroupBy(fact => fact.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group => LocationGuestProjections.BuildFeedbackStats(
                        group.Select(fact => fact.ToFeedbackFact())
                    )
                );
        }

        private async Task<Dictionary<int, HashSet<int>>> LoadTagIdsForGuestsAsync(
            IReadOnlyList<int> locationGuestIds
        )
        {
            if (locationGuestIds.Count == 0)
            {
                return new Dictionary<int, HashSet<int>>();
            }

            var ids = locationGuestIds as List<int> ?? locationGuestIds.ToList();
            var memberships = await _context.LocationGuestTags
                .AsNoTracking()
                .Where(m => ids.Contains(m.LocationGuestId))
                .Select(m => new { m.LocationGuestId, m.GuestTagId })
                .ToListAsync();

            return memberships
                .GroupBy(m => m.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(m => m.GuestTagId).ToHashSet()
                );
        }

        private static void ValidateExportListQuery(GuestsExportQuery query)
        {
            if (query.LocationIds.Count == 0)
            {
                throw new ArgumentException("At least one location is required.");
            }

            if (!ValidSmartGroups.Contains(query.SmartGroup))
            {
                throw new ArgumentException("Invalid smartGroup.");
            }

            if (!ValidSorts.Contains(query.Sort))
            {
                throw new ArgumentException("Invalid sort.");
            }

            GuestsFilterOptions.Validate(
                query.Marketing,
                query.Contact,
                query.Sentiment
            );
        }

        private async Task<Dictionary<int, List<string>>> LoadTagDisplayNamesAsync(
            IReadOnlyList<int> locationGuestIds
        )
        {
            if (locationGuestIds.Count == 0)
            {
                return new Dictionary<int, List<string>>();
            }

            var rows = await (
                from membership in _context.LocationGuestTags.AsNoTracking()
                join tag in _context.GuestTags.AsNoTracking()
                    on membership.GuestTagId equals tag.Id
                where locationGuestIds.Contains(membership.LocationGuestId)
                select new
                {
                    membership.LocationGuestId,
                    tag.DisplayName,
                }
            ).ToListAsync();

            return rows
                .GroupBy(row => row.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(row => row.DisplayName)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                );
        }

        private static string FormatLatestFeedbackLabel(string wireSentiment)
        {
            return wireSentiment switch
            {
                "positive" => "Positive",
                "neutral" => "Neutral",
                "negative" => "Negative",
                _ => string.Empty,
            };
        }

        private static string FormatIsoUtc(DateTime? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var utc = value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };

            return utc.ToString("O");
        }

        private static string FormatIsoUtc(DateTime value)
        {
            return FormatIsoUtc((DateTime?)value);
        }

        private static (DateTime FromUtc, DateTime ToUtc)? ResolveTableDateWindow(
            string? dateAxis,
            string? datePreset,
            DateTime? dateFrom,
            DateTime? dateTo,
            int utcOffsetMinutes,
            DateTime utcNow
        )
        {
            var hasPreset = !string.IsNullOrWhiteSpace(datePreset);
            var hasCustom = dateFrom != null || dateTo != null;

            if (!hasPreset && !hasCustom)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(dateAxis))
            {
                throw new ArgumentException("dateAxis is required when filtering by date.");
            }

            if (!ValidDateAxes.Contains(dateAxis))
            {
                throw new ArgumentException("Invalid dateAxis.");
            }

            if (hasPreset && hasCustom)
            {
                throw new ArgumentException(
                    "datePreset and dateFrom/dateTo are mutually exclusive."
                );
            }

            if (hasPreset)
            {
                if (!GuestsDateWindows.IsValidTablePreset(datePreset!))
                {
                    throw new ArgumentException("Invalid datePreset.");
                }

                return GuestsDateWindows.ResolvePreset(
                    datePreset!,
                    utcNow,
                    utcOffsetMinutes
                );
            }

            if (dateFrom == null || dateTo == null)
            {
                throw new ArgumentException("dateFrom and dateTo are both required.");
            }

            return GuestsDateWindows.ResolveCustom(
                dateFrom.Value,
                dateTo.Value,
                "dateFrom",
                "dateTo"
            );
        }

        private static (DateTime FromUtc, DateTime ToUtc)? ResolveOverviewWindow(
            GuestsListQuery query,
            DateTime utcNow
        )
        {
            var hasPreset = !string.IsNullOrWhiteSpace(query.OverviewDatePreset);
            var hasCustom =
                query.OverviewDateFrom != null || query.OverviewDateTo != null;

            if (!hasPreset && !hasCustom)
            {
                return null;
            }

            if (hasPreset && hasCustom)
            {
                throw new ArgumentException(
                    "overviewDatePreset and overviewDateFrom/overviewDateTo are mutually exclusive."
                );
            }

            if (hasPreset)
            {
                if (!GuestsDateWindows.IsValidOverviewPreset(query.OverviewDatePreset!))
                {
                    throw new ArgumentException("Invalid overviewDatePreset.");
                }

                return GuestsDateWindows.ResolvePreset(
                    query.OverviewDatePreset!,
                    utcNow,
                    query.UtcOffsetMinutes
                );
            }

            if (query.OverviewDateFrom == null || query.OverviewDateTo == null)
            {
                throw new ArgumentException(
                    "overviewDateFrom and overviewDateTo are both required."
                );
            }

            return GuestsDateWindows.ResolveCustom(
                query.OverviewDateFrom.Value,
                query.OverviewDateTo.Value,
                "overviewDateFrom",
                "overviewDateTo"
            );
        }

        private static void ValidateQuery(GuestsListQuery query)
        {
            if (query.LocationIds.Count == 0)
            {
                throw new ArgumentException("At least one location is required.");
            }

            if (!ValidSmartGroups.Contains(query.SmartGroup))
            {
                throw new ArgumentException("Invalid smartGroup.");
            }

            if (!ValidSorts.Contains(query.Sort))
            {
                throw new ArgumentException("Invalid sort.");
            }

            if (query.Page < 1)
            {
                throw new ArgumentException("page must be at least 1.");
            }

            if (query.PageSize != 25)
            {
                throw new ArgumentException("pageSize must be 25.");
            }

            GuestsFilterOptions.Validate(
                query.Marketing,
                query.Contact,
                query.Sentiment
            );
        }

        private static string NormalizeSmartGroup(string smartGroup)
        {
            return ValidSmartGroups.Single(value =>
                value.Equals(smartGroup, StringComparison.OrdinalIgnoreCase)
            );
        }

        private static string NormalizeSort(string sort)
        {
            return ValidSorts.Single(value =>
                value.Equals(sort, StringComparison.OrdinalIgnoreCase)
            );
        }

        private sealed class ShapedGuestRow
        {
            public int LocationGuestId { get; init; }

            public string Name { get; init; } = string.Empty;

            public string? Email { get; init; }

            public string? Mobile { get; init; }

            public string LocationName { get; init; } = string.Empty;

            public DateTime CapturedAt { get; init; }

            public int FeedbackSubmissionCount { get; init; }

            public string LatestFeedbackSentiment { get; init; } = "none";

            public DateTime? LastInteractionAt { get; init; }

            public string MarketingStatus { get; init; } = "Not eligible";

            public HashSet<int> TagIds { get; init; } = new();
        }
    }
}
