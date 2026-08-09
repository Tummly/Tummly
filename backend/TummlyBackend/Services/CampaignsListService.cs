using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaigns list — tab membership + filters/sort/pagination (ticket 28).
    /// </summary>
    public class CampaignsListService : ICampaignsListService
    {
        public const int DefaultPageSize = 25;
        public const string DraftStatus = "draft";
        public const string ScheduledStatus = "scheduled";
        public const string SendingStatus = "sending";
        public const string SentStatus = "sent";
        public const string PartiallySentStatus = "partially-sent";
        public const string PausedStatus = "paused";
        public const string FailedStatus = "failed";
        public const string CancelledStatus = "cancelled";

        private static readonly HashSet<string> AllowedViews = new(
            StringComparer.Ordinal
        )
        {
            "all",
            "needs-attention",
            "drafts",
            "in-flight",
            "sent",
        };

        private static readonly HashSet<string> AllowedSorts = new(
            StringComparer.Ordinal
        )
        {
            "recent-activity",
            "send-date",
            "name-az",
        };

        private static readonly HashSet<string> AllowedStatuses = new(
            StringComparer.Ordinal
        )
        {
            DraftStatus,
            ScheduledStatus,
            SendingStatus,
            SentStatus,
            PartiallySentStatus,
            PausedStatus,
            FailedStatus,
            CancelledStatus,
        };

        private static readonly HashSet<string> AllowedDeliveryIssues = new(
            StringComparer.Ordinal
        )
        {
            FailedStatus,
            PartiallySentStatus,
        };

        private static readonly HashSet<string> AllowedDateAxes = new(
            StringComparer.Ordinal
        )
        {
            "updated",
            "send-date",
        };

        private static readonly HashSet<string> NeedsAttentionStatuses = new(
            StringComparer.Ordinal
        )
        {
            FailedStatus,
            PartiallySentStatus,
        };

        private static readonly HashSet<string> InFlightStatuses = new(
            StringComparer.Ordinal
        )
        {
            ScheduledStatus,
            SendingStatus,
            PausedStatus,
        };

        private static readonly HashSet<string> SentTabStatuses = new(
            StringComparer.Ordinal
        )
        {
            SentStatus,
            PartiallySentStatus,
        };

        private readonly ApplicationDbContext _context;

        public CampaignsListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CampaignsListResponse> ListAsync(
            CampaignsListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidatePaging(query.Page, query.PageSize);
            var view = NormalizeView(query.View);
            var sort = NormalizeSort(query.Sort);
            var nameQuery = query.Q?.Trim() ?? string.Empty;
            var statuses = NormalizeStringFilters(query.Status, AllowedStatuses, "status");
            var channels = NormalizeStringFilters(
                query.Channel,
                CampaignProductAllowLists.Channels,
                "channel"
            );
            var goalIds = NormalizeStringFilters(
                query.GoalId,
                CampaignProductAllowLists.GoalIds,
                "goalId"
            );
            var offerStances = NormalizeStringFilters(
                query.OfferStance,
                CampaignProductAllowLists.OfferStances,
                "offerStance"
            );
            var deliveryIssues = NormalizeStringFilters(
                query.DeliveryIssue,
                AllowedDeliveryIssues,
                "deliveryIssue"
            );
            var createdBy = query.CreatedBy
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var locationIds = query.LocationIds.Count > 0
                ? query.LocationIds.ToList()
                : new List<int> { query.LocationId };

            var (dateFromUtc, dateToUtc) =
                GuestScopedListValidation.ResolveOptionalDateWindow(
                    query.DatePreset,
                    query.DateFrom,
                    query.DateTo,
                    query.UtcOffsetMinutes
                );
            var dateAxis = NormalizeOptionalDateAxis(
                query.DateAxis,
                dateFromUtc.HasValue
            );

            var atShell = _context.Campaigns
                .AsNoTracking()
                .Where(campaign =>
                    campaign.RestaurantLocationId == query.LocationId
                );

            var draftCount = await atShell.CountAsync(
                campaign => campaign.Status == DraftStatus,
                cancellationToken
            );
            var needsAttentionCount = await atShell.CountAsync(
                campaign => NeedsAttentionStatuses.Contains(campaign.Status),
                cancellationToken
            );
            var inFlightCount = await atShell.CountAsync(
                campaign => InFlightStatuses.Contains(campaign.Status),
                cancellationToken
            );
            var sentCount = await atShell.CountAsync(
                campaign => SentTabStatuses.Contains(campaign.Status),
                cancellationToken
            );
            var allCount = await atShell.CountAsync(cancellationToken);

            var tabCounts = new CampaignsTabCountsDto
            {
                All = allCount,
                Drafts = draftCount,
                NeedsAttention = needsAttentionCount,
                InFlight = inFlightCount,
                Sent = sentCount,
            };

            var filtered = _context.Campaigns
                .AsNoTracking()
                .Where(campaign =>
                    locationIds.Contains(campaign.RestaurantLocationId)
                );

            filtered = view switch
            {
                "drafts" => filtered.Where(c => c.Status == DraftStatus),
                "needs-attention" => filtered.Where(c =>
                    NeedsAttentionStatuses.Contains(c.Status)
                ),
                "in-flight" => filtered.Where(c =>
                    InFlightStatuses.Contains(c.Status)
                ),
                "sent" => filtered.Where(c =>
                    SentTabStatuses.Contains(c.Status)
                ),
                _ => filtered,
            };

            if (statuses.Count > 0)
            {
                filtered = filtered.Where(c => statuses.Contains(c.Status));
            }

            if (channels.Count > 0)
            {
                filtered = filtered.Where(c =>
                    c.Channel != null && channels.Contains(c.Channel)
                );
            }

            if (goalIds.Count > 0)
            {
                filtered = filtered.Where(c =>
                    c.GoalId != null && goalIds.Contains(c.GoalId)
                );
            }

            if (offerStances.Count > 0)
            {
                filtered = filtered.Where(c =>
                    c.OfferStance != null
                    && offerStances.Contains(c.OfferStance)
                );
            }

            if (createdBy.Count > 0)
            {
                filtered = filtered.Where(c =>
                    c.CreatedByUserId != null
                    && createdBy.Contains(c.CreatedByUserId.Value)
                );
            }

            if (deliveryIssues.Count > 0)
            {
                filtered = filtered.Where(c =>
                    deliveryIssues.Contains(c.Status)
                );
            }

            if (dateFromUtc.HasValue && dateToUtc.HasValue)
            {
                var from = dateFromUtc.Value;
                var to = dateToUtc.Value;
                filtered = dateAxis switch
                {
                    "send-date" => filtered.Where(c =>
                        c.ScheduledAtUtc != null
                        && c.ScheduledAtUtc >= from
                        && c.ScheduledAtUtc < to
                    ),
                    _ => filtered.Where(c =>
                        c.UpdatedAt >= from && c.UpdatedAt < to
                    ),
                };
            }

            if (nameQuery.Length > 0)
            {
                filtered = filtered.Where(campaign =>
                    EF.Functions.Like(campaign.Name, $"%{EscapeLike(nameQuery)}%")
                );
            }

            var totalCount = await filtered.CountAsync(cancellationToken);

            var ordered = sort switch
            {
                "name-az" => filtered
                    .OrderBy(c => c.Name)
                    .ThenByDescending(c => c.Id),
                "send-date" => filtered
                    .OrderBy(c => c.ScheduledAtUtc == null)
                    .ThenBy(c => c.ScheduledAtUtc)
                    .ThenByDescending(c => c.Id),
                _ => filtered
                    .OrderByDescending(c => c.UpdatedAt)
                    .ThenByDescending(c => c.Id),
            };

            var locationNames = query.LocationNamesById;
            var pageRows = await ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(campaign => new
                {
                    campaign.Id,
                    campaign.Name,
                    campaign.Status,
                    campaign.GoalId,
                    campaign.RestaurantLocationId,
                    campaign.Channel,
                    campaign.AudienceKey,
                    campaign.OfferStance,
                    campaign.CreatedByUserId,
                    CreatedByDisplayName = campaign.CreatedByUser != null
                        ? campaign.CreatedByUser.FullName
                        : null,
                    campaign.UpdatedAt,
                    campaign.ScheduledAtUtc,
                })
                .ToListAsync(cancellationToken);

            var items = pageRows
                .Select(campaign => new CampaignsListItemDto
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    Status = campaign.Status,
                    GoalId = campaign.GoalId,
                    LocationId = campaign.RestaurantLocationId,
                    LocationName = locationNames.TryGetValue(
                        campaign.RestaurantLocationId,
                        out var name
                    )
                        ? name
                        : string.Empty,
                    Channel = campaign.Channel,
                    AudienceKey = campaign.AudienceKey,
                    OfferStance = campaign.OfferStance,
                    CreatedByUserId = campaign.CreatedByUserId,
                    CreatedByDisplayName = campaign.CreatedByDisplayName,
                    UpdatedAt = campaign.UpdatedAt,
                    SendDate = campaign.ScheduledAtUtc == null
                        ? null
                        : campaign.ScheduledAtUtc.Value.ToString("O"),
                    Delivery = null,
                    Engagement = null,
                    Redemptions = null,
                })
                .ToList();

            var createdByRows = await _context.Campaigns
                .AsNoTracking()
                .Where(c =>
                    c.RestaurantLocationId == query.LocationId
                    && c.CreatedByUserId != null
                )
                .Select(c => new
                {
                    Id = c.CreatedByUserId!.Value,
                    Label = c.CreatedByUser != null
                        ? c.CreatedByUser.FullName
                        : $"User {c.CreatedByUserId}",
                })
                .ToListAsync(cancellationToken);

            var createdByOptions = createdByRows
                .GroupBy(row => row.Id)
                .Select(group => new CampaignsCreatedByOptionDto
                {
                    Id = group.Key,
                    Label = group.First().Label,
                })
                .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CampaignsListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TabCounts = tabCounts,
                FilterCatalog = new CampaignsListFilterCatalogDto
                {
                    CreatedBy = createdByOptions,
                },
            };
        }

        private static string EscapeLike(string value)
        {
            return value
                .Replace("[", "[[]", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);
        }

        private static void ValidatePaging(int page, int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException("page must be >= 1.");
            }

            if (pageSize != DefaultPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be {DefaultPageSize}."
                );
            }
        }

        private static string NormalizeView(string? view)
        {
            var key = (view ?? "all").Trim().ToLowerInvariant();
            if (!AllowedViews.Contains(key))
            {
                throw new ArgumentException(
                    "view must be all, needs-attention, drafts, in-flight, or sent."
                );
            }

            return key;
        }

        private static string NormalizeSort(string? sort)
        {
            var key = (sort ?? "recent-activity").Trim().ToLowerInvariant();
            if (!AllowedSorts.Contains(key))
            {
                throw new ArgumentException(
                    "sort must be recent-activity, send-date, or name-az."
                );
            }

            return key;
        }

        private static string? NormalizeOptionalDateAxis(
            string? dateAxis,
            bool hasDateWindow
        )
        {
            if (!hasDateWindow)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(dateAxis))
            {
                throw new ArgumentException(
                    "dateAxis is required when filtering by date."
                );
            }

            var key = dateAxis.Trim().ToLowerInvariant();
            if (!AllowedDateAxes.Contains(key))
            {
                throw new ArgumentException(
                    "dateAxis must be updated or send-date."
                );
            }

            return key;
        }

        private static List<string> NormalizeStringFilters(
            IReadOnlyList<string> values,
            IReadOnlySet<string> allowed,
            string fieldName
        )
        {
            if (values.Count == 0)
            {
                return new List<string>();
            }

            var normalized = new List<string>();
            foreach (var raw in values)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var key = raw.Trim().ToLowerInvariant();
                if (!allowed.Contains(key))
                {
                    throw new ArgumentException(
                        $"{fieldName} '{raw}' is not supported."
                    );
                }

                if (!normalized.Contains(key, StringComparer.Ordinal))
                {
                    normalized.Add(key);
                }
            }

            return normalized;
        }
    }
}
