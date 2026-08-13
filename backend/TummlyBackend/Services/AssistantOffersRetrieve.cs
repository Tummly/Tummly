using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantOffersRetrieve : IAssistantOffersRetrieve
    {
        public const int MaxSampleRows = 100;

        private readonly ApplicationDbContext _context;
        private readonly IOffersMetricsService _metrics;

        public AssistantOffersRetrieve(
            ApplicationDbContext context,
            IOffersMetricsService metrics
        )
        {
            _context = context;
            _metrics = metrics;
        }

        public async Task<AssistantOffersRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var catalogQuery = _context.CatalogOffers
                    .AsNoTracking()
                    .Where(offer => offer.RestaurantLocationId == ownedLocationId);

                var catalogTotal = await catalogQuery.CountAsync(cancellationToken);
                var catalog = await catalogQuery
                    .OrderByDescending(offer => offer.CreatedAt)
                    .ThenByDescending(offer => offer.Id)
                    .Take(MaxSampleRows)
                    .Select(offer => new AssistantOfferCatalogRow(
                        offer.Id,
                        offer.Title,
                        offer.Status,
                        offer.CreatedAt
                    ))
                    .ToListAsync(cancellationToken);

                var performance = await _metrics.GetPerformanceAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );

                var offerIds = catalog.Select(row => row.Id).ToList();
                var perOfferMetrics = await BuildPerOfferMetricsAsync(
                    catalog,
                    offerIds,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                var linkedCampaigns = await LoadLinkedCampaignsAsync(
                    ownedLocationId,
                    offerIds,
                    cancellationToken
                );
                var claimLogs = await LoadLogsAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    claimed: true,
                    cancellationToken
                );
                var redemptionLogs = await LoadLogsAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    claimed: false,
                    cancellationToken
                );

                return new AssistantOffersRetrieveResult.Ok(
                    new AssistantOffersEvidence(
                        catalogTotal,
                        catalog.Count,
                        performance.ActiveOffers,
                        performance.OffersIssued,
                        performance.Claims,
                        performance.Redemptions,
                        performance.ClaimToRedemptionRate,
                        catalog,
                        perOfferMetrics,
                        linkedCampaigns,
                        claimLogs,
                        redemptionLogs
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new AssistantOffersRetrieveResult.Failed();
            }
        }

        private async Task<IReadOnlyList<AssistantOfferMetricsRow>> BuildPerOfferMetricsAsync(
            IReadOnlyList<AssistantOfferCatalogRow> catalog,
            IReadOnlyList<int> offerIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            if (offerIds.Count == 0)
            {
                return [];
            }

            var issues = await _context.OfferIssues
                .AsNoTracking()
                .Where(issue => offerIds.Contains(issue.CatalogOfferId))
                .Select(issue => new
                {
                    issue.CatalogOfferId,
                    issue.ClaimedAtUtc,
                    issue.RedeemedAtUtc,
                    issue.RedemptionVoidedAtUtc,
                    issue.ExpiryAtUtc,
                    issue.CancelledAtUtc,
                })
                .ToListAsync(cancellationToken);

            var failed = await _context.OfferRedeemFailedAttempts
                .AsNoTracking()
                .Where(attempt =>
                    offerIds.Contains(attempt.CatalogOfferId)
                    && attempt.AttemptedAtUtc >= fromUtc
                    && attempt.AttemptedAtUtc < toUtc
                )
                .GroupBy(attempt => attempt.CatalogOfferId)
                .Select(group => new { OfferId = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            var failedLookup = failed.ToDictionary(row => row.OfferId, row => row.Count);

            return catalog
                .Select(offer =>
                {
                    var rows = issues.Where(issue => issue.CatalogOfferId == offer.Id);
                    var claims = rows.Count(issue =>
                        issue.ClaimedAtUtc != null
                        && issue.ClaimedAtUtc >= fromUtc
                        && issue.ClaimedAtUtc < toUtc
                    );
                    var redemptions = rows.Count(issue =>
                        issue.RedeemedAtUtc != null
                        && issue.RedemptionVoidedAtUtc == null
                        && issue.RedeemedAtUtc >= fromUtc
                        && issue.RedeemedAtUtc < toUtc
                    );
                    var expiredUnused = rows.Count(issue =>
                        issue.ExpiryAtUtc >= fromUtc
                        && issue.ExpiryAtUtc < toUtc
                        && issue.RedeemedAtUtc == null
                        && issue.CancelledAtUtc == null
                    );
                    double? rate = claims == 0 ? null : (double)redemptions / claims;
                    return new AssistantOfferMetricsRow(
                        offer.Id,
                        offer.Title,
                        claims,
                        redemptions,
                        rate,
                        expiredUnused,
                        failedLookup.GetValueOrDefault(offer.Id)
                    );
                })
                .ToList();
        }

        private async Task<IReadOnlyList<AssistantOfferLinkedCampaignRow>> LoadLinkedCampaignsAsync(
            int ownedLocationId,
            IReadOnlyList<int> offerIds,
            CancellationToken cancellationToken
        )
        {
            if (offerIds.Count == 0)
            {
                return [];
            }

            return await _context.Campaigns
                .AsNoTracking()
                .Where(campaign =>
                    campaign.RestaurantLocationId == ownedLocationId
                    && campaign.OfferId != null
                    && offerIds.Contains(campaign.OfferId.Value)
                )
                .OrderByDescending(campaign => campaign.UpdatedAt)
                .ThenByDescending(campaign => campaign.Id)
                .Select(campaign => new AssistantOfferLinkedCampaignRow(
                    campaign.OfferId!.Value,
                    campaign.Id,
                    campaign.Name,
                    campaign.Status
                ))
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<AssistantOfferLogRow>> LoadLogsAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            bool claimed,
            CancellationToken cancellationToken
        )
        {
            var joined =
                from issue in _context.OfferIssues.AsNoTracking()
                join offer in _context.CatalogOffers.AsNoTracking()
                    on issue.CatalogOfferId equals offer.Id
                where offer.RestaurantLocationId == ownedLocationId
                select new { issue, offer };

            if (claimed)
            {
                return await joined
                    .Where(row =>
                        row.issue.ClaimedAtUtc != null
                        && row.issue.ClaimedAtUtc >= fromUtc
                        && row.issue.ClaimedAtUtc < toUtc
                    )
                    .OrderByDescending(row => row.issue.ClaimedAtUtc)
                    .ThenByDescending(row => row.issue.Id)
                    .Take(MaxSampleRows)
                    .Select(row => new AssistantOfferLogRow(
                        row.offer.Id,
                        row.offer.Title,
                        row.issue.ClaimedAtUtc!.Value,
                        row.issue.ClaimCode
                    ))
                    .ToListAsync(cancellationToken);
            }

            return await joined
                .Where(row =>
                    row.issue.RedeemedAtUtc != null
                    && row.issue.RedemptionVoidedAtUtc == null
                    && row.issue.RedeemedAtUtc >= fromUtc
                    && row.issue.RedeemedAtUtc < toUtc
                )
                .OrderByDescending(row => row.issue.RedeemedAtUtc)
                .ThenByDescending(row => row.issue.Id)
                .Take(MaxSampleRows)
                .Select(row => new AssistantOfferLogRow(
                    row.offer.Id,
                    row.offer.Title,
                    row.issue.RedeemedAtUtc!.Value,
                    row.issue.ClaimCode
                ))
                .ToListAsync(cancellationToken);
        }
    }
}
