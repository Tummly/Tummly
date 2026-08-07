using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaigns list — Draft rows on All/Drafts; other views empty in slice 1 (ticket 30).
    /// </summary>
    public class CampaignsListService : ICampaignsListService
    {
        public const int DefaultPageSize = 25;
        public const string DraftStatus = "draft";

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
            var nameQuery = query.Q?.Trim() ?? string.Empty;

            var draftsAtLocation = _context.Campaigns
                .AsNoTracking()
                .Where(campaign =>
                    campaign.RestaurantLocationId == query.LocationId
                    && campaign.Status == DraftStatus
                );

            var draftCount = await draftsAtLocation.CountAsync(cancellationToken);
            var tabCounts = new CampaignsTabCountsDto
            {
                All = draftCount,
                Drafts = draftCount,
                NeedsAttention = 0,
                InFlight = 0,
                Sent = 0,
            };

            // Needs attention / In flight / Sent stay empty — no fake status transitions.
            if (view is "needs-attention" or "in-flight" or "sent")
            {
                return new CampaignsListResponse
                {
                    Items = Array.Empty<CampaignsListItemDto>(),
                    TotalCount = 0,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TabCounts = tabCounts,
                };
            }

            var filtered = draftsAtLocation;
            if (nameQuery.Length > 0)
            {
                filtered = filtered.Where(campaign =>
                    EF.Functions.Like(campaign.Name, $"%{EscapeLike(nameQuery)}%")
                );
            }

            var totalCount = await filtered.CountAsync(cancellationToken);

            var locationName = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == query.LocationId)
                .Select(location => location.LocationName)
                .FirstOrDefaultAsync(cancellationToken)
                ?? string.Empty;

            // Project list columns only — omit MessageBody / MessageSubject (nvarchar(max)).
            var items = await filtered
                .OrderByDescending(campaign => campaign.UpdatedAt)
                .ThenByDescending(campaign => campaign.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(campaign => new CampaignsListItemDto
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    Status = campaign.Status,
                    GoalId = campaign.GoalId,
                    LocationId = campaign.RestaurantLocationId,
                    LocationName = locationName,
                    Channel = campaign.Channel,
                    AudienceKey = campaign.AudienceKey,
                    OfferStance = campaign.OfferStance,
                    UpdatedAt = campaign.UpdatedAt,
                    SendDate = null,
                    Delivery = null,
                    Engagement = null,
                    Redemptions = null,
                })
                .ToListAsync(cancellationToken);

            return new CampaignsListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TabCounts = tabCounts,
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
    }
}
