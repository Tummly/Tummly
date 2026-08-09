using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaigns list — full MVP tab membership + honest Delivery / Engagement /
    /// Redemptions (ticket 26).
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
            var nameQuery = query.Q?.Trim() ?? string.Empty;

            var atLocation = _context.Campaigns
                .AsNoTracking()
                .Where(campaign =>
                    campaign.RestaurantLocationId == query.LocationId
                );

            var draftCount = await atLocation.CountAsync(
                campaign => campaign.Status == DraftStatus,
                cancellationToken
            );
            var needsAttentionCount = await atLocation.CountAsync(
                campaign => NeedsAttentionStatuses.Contains(campaign.Status),
                cancellationToken
            );
            var inFlightCount = await atLocation.CountAsync(
                campaign => InFlightStatuses.Contains(campaign.Status),
                cancellationToken
            );
            var sentCount = await atLocation.CountAsync(
                campaign => SentTabStatuses.Contains(campaign.Status),
                cancellationToken
            );
            var allCount = await atLocation.CountAsync(cancellationToken);

            var tabCounts = new CampaignsTabCountsDto
            {
                All = allCount,
                Drafts = draftCount,
                NeedsAttention = needsAttentionCount,
                InFlight = inFlightCount,
                Sent = sentCount,
            };

            var filtered = view switch
            {
                "drafts" => atLocation.Where(c => c.Status == DraftStatus),
                "needs-attention" => atLocation.Where(c =>
                    NeedsAttentionStatuses.Contains(c.Status)
                ),
                "in-flight" => atLocation.Where(c =>
                    InFlightStatuses.Contains(c.Status)
                ),
                "sent" => atLocation.Where(c =>
                    SentTabStatuses.Contains(c.Status)
                ),
                _ => atLocation,
            };

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
                    // Send date: scheduled fire time when present; else null (Draft / send-now pending).
                    SendDate = campaign.ScheduledAtUtc == null
                        ? null
                        : campaign.ScheduledAtUtc.Value.ToString("O"),
                    // Delivery = Submitted/accepted when known — none yet → null (UI dash).
                    Delivery = null,
                    // Engagement always dash until report ingestion.
                    Engagement = null,
                    // Redemptions honest — no mark-complete attach yet → null (UI dash / 0).
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
