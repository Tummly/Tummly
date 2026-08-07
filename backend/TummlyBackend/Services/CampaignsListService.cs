using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Thin Campaigns list — empty-first until Draft rows persist (ticket 30).
    /// Needs attention / In flight / Sent always return empty items in slice 1.
    /// </summary>
    public class CampaignsListService : ICampaignsListService
    {
        public const int DefaultPageSize = 25;

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

        public Task<CampaignsListResponse> ListAsync(
            CampaignsListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ValidatePaging(query.Page, query.PageSize);
            _ = NormalizeView(query.View);

            // Draft count is 0 until the Draft stub API persists rows.
            var draftCount = 0;
            var tabCounts = new CampaignsTabCountsDto
            {
                All = draftCount,
                Drafts = draftCount,
                NeedsAttention = 0,
                InFlight = 0,
                Sent = 0,
            };

            return Task.FromResult(
                new CampaignsListResponse
                {
                    Items = Array.Empty<CampaignsListItemDto>(),
                    TotalCount = 0,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TabCounts = tabCounts,
                }
            );
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
