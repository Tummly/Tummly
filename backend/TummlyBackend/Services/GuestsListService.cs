using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestsListService : IGuestsListService
    {
        private const int NewGuestDays = 13;
        private const int NewThisMonthDays = 30;
        private const int DormantDays = 90;

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

        private readonly ApplicationDbContext _context;

        public GuestsListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetListAsync(
            int locationId,
            string locationName,
            string smartGroup,
            string? q,
            string sort,
            int page,
            int pageSize
        )
        {
            ValidateQuery(smartGroup, sort, page, pageSize);

            var normalizedSmartGroup = NormalizeSmartGroup(smartGroup);
            var normalizedSort = NormalizeSort(sort);
            var normalizedQuery = q?.Trim() ?? string.Empty;
            var utcNow = DateTime.UtcNow;
            var newGuestCutoff = utcNow.AddDays(-NewGuestDays);
            var newThisMonthCutoff = utcNow.AddDays(-NewThisMonthDays);
            var dormantCutoff = utcNow.AddDays(-DormantDays);

            var guests = await _context.LocationGuests
                .AsNoTracking()
                .Include(lg => lg.MasterGuest)
                .Where(lg => lg.RestaurantLocationId == locationId)
                .ToListAsync();

            var feedbackStats = await LoadFeedbackStatsAsync(locationId);

            var derivedRows = guests
                .Select(lg => DeriveRow(lg, feedbackStats, utcNow, newGuestCutoff, dormantCutoff))
                .ToList();

            var overview = new
            {
                totalGuests = derivedRows.Count,
                newThisMonth = derivedRows.Count(row =>
                    row.CapturedAt >= newThisMonthCutoff
                ),
                marketingEligible = derivedRows.Count(row =>
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

            var filteredList = SortRows(
                    filteredRows.ToList(),
                    normalizedSort
                )
                .ToList();

            var totalFilteredCount = filteredList.Count;
            var pagedRows = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new
                {
                    id = row.LocationGuestId.ToString(),
                    name = row.Name,
                    email = row.Email,
                    mobile = row.Mobile,
                    marketingStatus = row.MarketingStatus,
                    locationName,
                    latestFeedbackSentiment = row.LatestFeedbackSentiment,
                    feedbackSubmissionCount = row.FeedbackSubmissionCount,
                    lastInteractionLabel = "Feedback submitted",
                    lastInteractionAt = row.LastInteractionAt,
                    capturedAt = row.CapturedAt,
                })
                .ToList();

            return new
            {
                success = true,
                locationId,
                smartGroup = normalizedSmartGroup,
                q = normalizedQuery,
                sort = normalizedSort,
                page,
                pageSize,
                totalFilteredCount,
                overview,
                smartGroupCounts,
                rows = pagedRows,
            };
        }

        private async Task<Dictionary<int, LocationGuestFeedbackStats>> LoadFeedbackStatsAsync(
            int locationId
        )
        {
            var facts = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
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

        private static void ValidateQuery(
            string smartGroup,
            string sort,
            int page,
            int pageSize
        )
        {
            if (!ValidSmartGroups.Contains(smartGroup))
            {
                throw new ArgumentException("Invalid smartGroup.");
            }

            if (!ValidSorts.Contains(sort))
            {
                throw new ArgumentException("Invalid sort.");
            }

            if (page < 1)
            {
                throw new ArgumentException("page must be at least 1.");
            }

            if (pageSize != 25)
            {
                throw new ArgumentException("pageSize must be 25.");
            }
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
            IReadOnlyDictionary<int, LocationGuestFeedbackStats> feedbackStats,
            DateTime utcNow,
            DateTime newGuestCutoff,
            DateTime dormantCutoff
        )
        {
            var masterGuest = locationGuest.MasterGuest
                ?? throw new InvalidOperationException(
                    "Location guest is missing master guest."
                );

            feedbackStats.TryGetValue(
                locationGuest.Id,
                out var stats
            );

            var latestFeedbackSentiment =
                stats?.LatestFeedbackSentiment ?? "none";
            var lastInteractionAt = stats?.LastInteractionAt;

            return new DerivedGuestRow
            {
                LocationGuestId = locationGuest.Id,
                Name = locationGuest.Name,
                Email = masterGuest.Email,
                NormalizedEmail = masterGuest.NormalizedEmail,
                Mobile = masterGuest.Mobile,
                NormalizedPhone = masterGuest.NormalizedPhone,
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

            public DateTime CapturedAt { get; init; }

            public int FeedbackSubmissionCount { get; init; }

            public string LatestFeedbackSentiment { get; init; } = "none";

            public DateTime? LastInteractionAt { get; init; }

            public string MarketingStatus { get; init; } = "Not eligible";

            public bool IsNewGuest { get; init; }

            public bool IsPositiveFeedback { get; init; }

            public bool IsDormant { get; init; }
        }
    }
}

