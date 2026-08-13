using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantCampaignsRetrieve : IAssistantCampaignsRetrieve
    {
        public const int MaxSampleRows = 100;

        private readonly ApplicationDbContext _context;
        private readonly ICampaignsSummaryService _summary;
        private readonly ICampaignEligibilityService _eligibility;

        public AssistantCampaignsRetrieve(
            ApplicationDbContext context,
            ICampaignsSummaryService summary,
            ICampaignEligibilityService eligibility
        )
        {
            _context = context;
            _summary = summary;
            _eligibility = eligibility;
        }

        public async Task<AssistantCampaignsRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var listQuery = _context.Campaigns
                    .AsNoTracking()
                    .Where(campaign => campaign.RestaurantLocationId == ownedLocationId);

                var listTotal = await listQuery.CountAsync(cancellationToken);
                var rows = await listQuery
                    .OrderByDescending(campaign => campaign.CreatedAt)
                    .ThenByDescending(campaign => campaign.Id)
                    .Take(MaxSampleRows)
                    .Select(campaign => new AssistantCampaignListRow(
                        campaign.Id,
                        campaign.Name,
                        campaign.Status,
                        campaign.CreatedAt,
                        campaign.UpdatedAt,
                        campaign.OfferId
                    ))
                    .ToListAsync(cancellationToken);

                var summary = await _summary.GetSummaryAsync(
                    new CampaignsSummaryQuery
                    {
                        LocationId = ownedLocationId,
                        OverviewDateFrom = fromUtc,
                        OverviewDateTo = toUtc,
                    },
                    cancellationToken
                );

                var sampleIds = rows.Select(row => row.Id).ToList();
                var details = sampleIds.Count == 0
                    ? []
                    : await _context.Campaigns
                        .AsNoTracking()
                        .Where(campaign => sampleIds.Contains(campaign.Id))
                        .Select(campaign => new AssistantCampaignDetailRow(
                            campaign.Id,
                            campaign.Name,
                            campaign.Status,
                            campaign.MessageSubject,
                            campaign.MessageBody,
                            campaign.AudienceKey,
                            campaign.Channel
                        ))
                        .ToListAsync(cancellationToken);

                var eligibility = await LoadEligibilityAsync(
                    ownedLocationId,
                    details,
                    cancellationToken
                );

                return new AssistantCampaignsRetrieveResult.Ok(
                    new AssistantCampaignsEvidence(
                        listTotal,
                        rows.Count,
                        summary.CampaignsInFlightScheduled,
                        summary.CampaignsInFlightSending,
                        summary.MessagesSentAccepted,
                        rows,
                        eligibility,
                        details
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new AssistantCampaignsRetrieveResult.Failed();
            }
        }

        private async Task<IReadOnlyList<AssistantCampaignEligibilityRow>> LoadEligibilityAsync(
            int ownedLocationId,
            IReadOnlyList<AssistantCampaignDetailRow> details,
            CancellationToken cancellationToken
        )
        {
            var rows = new List<AssistantCampaignEligibilityRow>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var detail in details)
            {
                var key = detail.AudienceKey?.Trim();
                if (string.IsNullOrEmpty(key) || !seenKeys.Add(key))
                {
                    continue;
                }

                try
                {
                    var result = await _eligibility.EvaluateAsync(
                        ownedLocationId,
                        key,
                        cancellationToken
                    );
                    rows.Add(
                        new AssistantCampaignEligibilityRow(
                            detail.Id,
                            result.AudienceKey,
                            result.Evaluable,
                            result.Matched,
                            result.CurrentlyEligible,
                            result.Excluded
                        )
                    );
                }
                catch (ArgumentException)
                {
                    // Unknown or disallowed audience keys are skipped.
                }
            }

            return rows;
        }
    }
}
