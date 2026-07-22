using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestsListService : IGuestsListService
    {
        public const int ExportSoftMaxRows = 10_000;

        private const int NewGuestDays = 13;
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
            var dormantCutoff = utcNow.AddDays(-DormantDays);

            var locationIds = query.LocationIds.ToList();

            var guests = await _context.LocationGuests
                .AsNoTracking()
                .Include(lg => lg.MasterGuest)
                .Where(lg => locationIds.Contains(lg.RestaurantLocationId))
                .ToListAsync();

            var feedbackStats = await LoadFeedbackStatsAsync(locationIds);
            var succeededSentiments = query.Sentiment.Count > 0
                ? await LoadSucceededSentimentsAsync(locationIds)
                : new Dictionary<int, HashSet<string>>();
            var tagMemberships = await LoadTagMembershipsAsync(
                locationIds,
                query.RestaurantId,
                query.TagIds
            );

            var derivedRows = guests
                .Select(lg =>
                    DeriveRow(
                        lg,
                        query.LocationNamesById,
                        feedbackStats,
                        succeededSentiments,
                        tagMemberships,
                        newGuestCutoff,
                        dormantCutoff
                    )
                )
                .ToList();

            var overviewWindow = ResolveOverviewWindow(query, utcNow);
            var overviewCohort = overviewWindow == null
                ? derivedRows
                : derivedRows
                    .Where(row =>
                        row.CapturedAt >= overviewWindow.Value.FromUtc
                        && row.CapturedAt < overviewWindow.Value.ToUtc
                    )
                    .ToList();

            var overview = new
            {
                totalGuests = overviewCohort.Count,
                marketingEligible = overviewCohort.Count(row =>
                    row.MarketingStatus is "Eligible — Email" or "Eligible — SMS"
                ),
                needsRecovery = 0,
            };

            var smartGroupCounts = new Dictionary<string, int>
            {
                ["all-guests"] = derivedRows.Count,
                ["new-guests"] = derivedRows.Count(row => row.IsNewGuest),
                ["needs-recovery"] = 0,
                ["positive-feedback"] = derivedRows.Count(row => row.IsPositiveFeedback),
                ["offer-not-redeemed"] = 0,
                ["recent-redeemers"] = 0,
                ["dormant-guests"] = derivedRows.Count(row => row.IsDormant),
            };

            IEnumerable<DerivedGuestRow> filteredRows = derivedRows;

            if (DeferredSmartGroups.Contains(normalizedSmartGroup))
            {
                filteredRows = Array.Empty<DerivedGuestRow>();
            }
            else
            {
                filteredRows = normalizedSmartGroup switch
                {
                    "all-guests" => filteredRows,
                    "new-guests" => filteredRows.Where(row => row.IsNewGuest),
                    "positive-feedback" => filteredRows.Where(row => row.IsPositiveFeedback),
                    "dormant-guests" => filteredRows.Where(row => row.IsDormant),
                    _ => filteredRows,
                };
            }

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                filteredRows = filteredRows.Where(row =>
                    row.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                    || (
                        row.Email != null
                        && row.Email.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        row.NormalizedEmail != null
                        && row.NormalizedEmail.Contains(
                            normalizedQuery,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    || (
                        row.Mobile != null
                        && row.Mobile.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        row.NormalizedPhone != null
                        && row.NormalizedPhone.Contains(
                            normalizedQuery,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                );
            }

            filteredRows = ApplyTableFilters(filteredRows, query, utcNow);

            var filteredList = SortRows(filteredRows.ToList(), normalizedSort).ToList();

            var totalFilteredCount = filteredList.Count;
            var pagedRows = filteredList
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
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
            var newGuestCutoff = utcNow.AddDays(-NewGuestDays);
            var dormantCutoff = utcNow.AddDays(-DormantDays);

            List<DerivedGuestRow> exportRows;
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

                var feedbackStats = await LoadFeedbackStatsAsync(locationIds);
                var byId = guests.ToDictionary(g => g.Id);

                exportRows = orderedIds
                    .Select(id =>
                        DeriveRow(
                            byId[id],
                            locationNames,
                            feedbackStats,
                            new Dictionary<int, HashSet<string>>(),
                            new Dictionary<int, HashSet<int>>(),
                            newGuestCutoff,
                            dormantCutoff
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
                var locationIds = query.LocationIds.ToList();

                var guests = await _context.LocationGuests
                    .AsNoTracking()
                    .Include(lg => lg.MasterGuest)
                    .Where(lg => locationIds.Contains(lg.RestaurantLocationId))
                    .ToListAsync();

                var feedbackStats = await LoadFeedbackStatsAsync(locationIds);
                var succeededSentiments = query.Sentiment.Count > 0
                    ? await LoadSucceededSentimentsAsync(locationIds)
                    : new Dictionary<int, HashSet<string>>();
                var tagMemberships = query.TagIds.Count > 0
                    ? await LoadTagMembershipsAsync(
                        locationIds,
                        query.RestaurantId,
                        query.TagIds
                    )
                    : new Dictionary<int, HashSet<int>>();

                var derivedRows = guests
                    .Select(lg =>
                        DeriveRow(
                            lg,
                            query.LocationNamesById,
                            feedbackStats,
                            succeededSentiments,
                            tagMemberships,
                            newGuestCutoff,
                            dormantCutoff
                        )
                    )
                    .ToList();

                IEnumerable<DerivedGuestRow> filteredRows = derivedRows;

                if (DeferredSmartGroups.Contains(normalizedSmartGroup))
                {
                    filteredRows = Array.Empty<DerivedGuestRow>();
                }
                else
                {
                    filteredRows = normalizedSmartGroup switch
                    {
                        "all-guests" => filteredRows,
                        "new-guests" => filteredRows.Where(row => row.IsNewGuest),
                        "positive-feedback" => filteredRows.Where(row =>
                            row.IsPositiveFeedback
                        ),
                        "dormant-guests" => filteredRows.Where(row => row.IsDormant),
                        _ => filteredRows,
                    };
                }

                if (!string.IsNullOrWhiteSpace(normalizedQuery))
                {
                    filteredRows = filteredRows.Where(row =>
                        row.Name.Contains(
                            normalizedQuery,
                            StringComparison.OrdinalIgnoreCase
                        )
                        || (
                            row.Email != null
                            && row.Email.Contains(
                                normalizedQuery,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        || (
                            row.NormalizedEmail != null
                            && row.NormalizedEmail.Contains(
                                normalizedQuery,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        || (
                            row.Mobile != null
                            && row.Mobile.Contains(
                                normalizedQuery,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        || (
                            row.NormalizedPhone != null
                            && row.NormalizedPhone.Contains(
                                normalizedQuery,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    );
                }

                var listQuery = new GuestsListQuery
                {
                    LocationIds = query.LocationIds,
                    LocationNamesById = query.LocationNamesById,
                    ShellLocationId = query.ShellLocationId,
                    RestaurantId = query.RestaurantId,
                    SmartGroup = query.SmartGroup,
                    Q = query.Q,
                    Sort = query.Sort,
                    Page = 1,
                    PageSize = 25,
                    Marketing = query.Marketing,
                    Contact = query.Contact,
                    Sentiment = query.Sentiment,
                    TagIds = query.TagIds,
                    DateAxis = query.DateAxis,
                    DatePreset = query.DatePreset,
                    DateFrom = query.DateFrom,
                    DateTo = query.DateTo,
                    UtcOffsetMinutes = query.UtcOffsetMinutes,
                };

                filteredRows = ApplyTableFilters(filteredRows, listQuery, utcNow);
                exportRows = SortRows(filteredRows.ToList(), normalizedSort).ToList();
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

        private static IEnumerable<DerivedGuestRow> ApplyTableFilters(
            IEnumerable<DerivedGuestRow> rows,
            GuestsListQuery query,
            DateTime utcNow
        )
        {
            var filtered = rows;

            if (query.Marketing.Count > 0)
            {
                var marketing = GuestsFilterOptions.Normalize(
                    query.Marketing,
                    GuestsFilterOptions.Marketing
                );
                filtered = filtered.Where(row =>
                {
                    var eligible =
                        row.MarketingStatus is "Eligible — Email" or "Eligible — SMS";
                    return marketing.Any(option =>
                        option == "eligible"
                            ? eligible
                            : !eligible
                    );
                });
            }

            if (query.Contact.Count > 0)
            {
                var contact = GuestsFilterOptions.Normalize(
                    query.Contact,
                    GuestsFilterOptions.Contact
                );
                filtered = filtered.Where(row =>
                    contact.Any(option =>
                        option == "email"
                            ? !string.IsNullOrWhiteSpace(row.Email)
                            : !string.IsNullOrWhiteSpace(row.Mobile)
                    )
                );
            }

            if (query.Sentiment.Count > 0)
            {
                var sentiments = GuestsFilterOptions.Normalize(
                    query.Sentiment,
                    GuestsFilterOptions.Sentiment
                );
                filtered = filtered.Where(row =>
                    sentiments.Any(sentiment =>
                        row.SucceededSentiments.Contains(sentiment)
                    )
                );
            }

            if (query.TagIds.Count > 0)
            {
                var tagIds = query.TagIds.Distinct().ToHashSet();
                filtered = filtered.Where(row =>
                    row.TagIds.Any(tagId => tagIds.Contains(tagId))
                );
            }

            var tableWindow = ResolveTableDateWindow(query, utcNow);
            if (tableWindow != null)
            {
                var axis = ValidDateAxes.Single(option =>
                    option.Equals(query.DateAxis, StringComparison.OrdinalIgnoreCase)
                );
                filtered = filtered.Where(row =>
                {
                    var value = axis == "first-captured"
                        ? row.CapturedAt
                        : row.LastInteractionAt;

                    if (value == null)
                    {
                        return false;
                    }

                    return value >= tableWindow.Value.FromUtc
                        && value < tableWindow.Value.ToUtc;
                });
            }

            return filtered;
        }

        private static (DateTime FromUtc, DateTime ToUtc)? ResolveTableDateWindow(
            GuestsListQuery query,
            DateTime utcNow
        )
        {
            var hasPreset = !string.IsNullOrWhiteSpace(query.DatePreset);
            var hasCustom = query.DateFrom != null || query.DateTo != null;

            if (!hasPreset && !hasCustom)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(query.DateAxis))
            {
                throw new ArgumentException("dateAxis is required when filtering by date.");
            }

            if (!ValidDateAxes.Contains(query.DateAxis))
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
                if (!GuestsDateWindows.IsValidTablePreset(query.DatePreset!))
                {
                    throw new ArgumentException("Invalid datePreset.");
                }

                return GuestsDateWindows.ResolvePreset(
                    query.DatePreset!,
                    utcNow,
                    query.UtcOffsetMinutes
                );
            }

            if (query.DateFrom == null || query.DateTo == null)
            {
                throw new ArgumentException("dateFrom and dateTo are both required.");
            }

            return GuestsDateWindows.ResolveCustom(
                query.DateFrom.Value,
                query.DateTo.Value,
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

        private async Task<Dictionary<int, LocationGuestFeedbackStats>> LoadFeedbackStatsAsync(
            IReadOnlyList<int> locationIds
        )
        {
            var facts = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    locationIds.Contains(f.RestaurantLocationId)
                    && f.LocationGuestId != null
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

        private async Task<Dictionary<int, HashSet<string>>> LoadSucceededSentimentsAsync(
            IReadOnlyList<int> locationIds
        )
        {
            var rows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    locationIds.Contains(f.RestaurantLocationId)
                    && f.LocationGuestId != null
                    && f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment != null
                )
                .Select(f => new
                {
                    LocationGuestId = f.LocationGuestId!.Value,
                    f.Sentiment,
                })
                .ToListAsync();

            return rows
                .GroupBy(row => row.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(row =>
                            FeedbackClassificationMapping.ToWireSentiment(row.Sentiment)!
                        )
                        .ToHashSet(StringComparer.OrdinalIgnoreCase)
                );
        }

        private async Task<Dictionary<int, HashSet<int>>> LoadTagMembershipsAsync(
            IReadOnlyList<int> locationIds,
            int restaurantId,
            IReadOnlyList<int> requestedTagIds
        )
        {
            if (requestedTagIds.Count > 0)
            {
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

            var memberships = await (
                from membership in _context.LocationGuestTags.AsNoTracking()
                join guest in _context.LocationGuests.AsNoTracking()
                    on membership.LocationGuestId equals guest.Id
                join tag in _context.GuestTags.AsNoTracking()
                    on membership.GuestTagId equals tag.Id
                where locationIds.Contains(guest.RestaurantLocationId)
                    && tag.RestaurantId == restaurantId
                select new { membership.LocationGuestId, membership.GuestTagId }
            ).ToListAsync();

            return memberships
                .GroupBy(m => m.LocationGuestId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(m => m.GuestTagId).ToHashSet()
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

        private static IEnumerable<DerivedGuestRow> SortRows(
            List<DerivedGuestRow> rows,
            string sort
        )
        {
            return sort switch
            {
                "newest-guests" => rows
                    .OrderByDescending(row => row.CapturedAt)
                    .ThenByDescending(row => row.LocationGuestId),
                "oldest-guests" => rows
                    .OrderBy(row => row.CapturedAt)
                    .ThenBy(row => row.LocationGuestId),
                "guest-name-az" => rows
                    .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => row.LocationGuestId),
                "guest-name-za" => rows
                    .OrderByDescending(row => row.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(row => row.LocationGuestId),
                "most-feedback-submissions" => rows
                    .OrderByDescending(row => row.FeedbackSubmissionCount)
                    .ThenByDescending(row => row.LastInteractionAt ?? DateTime.MinValue)
                    .ThenByDescending(row => row.LocationGuestId),
                "most-recent-redemption" or "recent-activity" => rows
                    .OrderByDescending(row => row.LastInteractionAt ?? DateTime.MinValue)
                    .ThenByDescending(row => row.CapturedAt)
                    .ThenByDescending(row => row.LocationGuestId),
                _ => rows
                    .OrderByDescending(row => row.LastInteractionAt ?? DateTime.MinValue)
                    .ThenByDescending(row => row.CapturedAt)
                    .ThenByDescending(row => row.LocationGuestId),
            };
        }

        private static DerivedGuestRow DeriveRow(
            LocationGuest locationGuest,
            IReadOnlyDictionary<int, string> locationNamesById,
            IReadOnlyDictionary<int, LocationGuestFeedbackStats> feedbackStats,
            IReadOnlyDictionary<int, HashSet<string>> succeededSentiments,
            IReadOnlyDictionary<int, HashSet<int>> tagMemberships,
            DateTime newGuestCutoff,
            DateTime dormantCutoff
        )
        {
            var masterGuest = locationGuest.MasterGuest
                ?? throw new InvalidOperationException(
                    "Location guest is missing master guest."
                );

            feedbackStats.TryGetValue(locationGuest.Id, out var stats);
            succeededSentiments.TryGetValue(
                locationGuest.Id,
                out var sentiments
            );
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

            return new DerivedGuestRow
            {
                LocationGuestId = locationGuest.Id,
                Name = locationGuest.Name,
                Email = masterGuest.Email,
                NormalizedEmail = masterGuest.NormalizedEmail,
                Mobile = masterGuest.Mobile,
                NormalizedPhone = masterGuest.NormalizedPhone,
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
                IsNewGuest = locationGuest.CreatedAt >= newGuestCutoff,
                IsPositiveFeedback = latestFeedbackSentiment == "positive",
                IsDormant = lastInteractionAt != null
                    && lastInteractionAt.Value < dormantCutoff,
                SucceededSentiments = sentiments
                    ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                TagIds = tags ?? new HashSet<int>(),
            };
        }

        private sealed class DerivedGuestRow
        {
            public int LocationGuestId { get; init; }

            public string Name { get; init; } = string.Empty;

            public string? Email { get; init; }

            public string? NormalizedEmail { get; init; }

            public string? Mobile { get; init; }

            public string? NormalizedPhone { get; init; }

            public string LocationName { get; init; } = string.Empty;

            public DateTime CapturedAt { get; init; }

            public int FeedbackSubmissionCount { get; init; }

            public string LatestFeedbackSentiment { get; init; } = "none";

            public DateTime? LastInteractionAt { get; init; }

            public string MarketingStatus { get; init; } = "Not eligible";

            public bool IsNewGuest { get; init; }

            public bool IsPositiveFeedback { get; init; }

            public bool IsDormant { get; init; }

            public HashSet<string> SucceededSentiments { get; init; } =
                new(StringComparer.OrdinalIgnoreCase);

            public HashSet<int> TagIds { get; init; } = new();
        }
    }
}
