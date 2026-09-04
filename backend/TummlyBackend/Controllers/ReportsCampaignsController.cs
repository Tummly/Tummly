using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Campaigns report KPIs (ticket 16). Soft lock does not deny the read.
    /// </summary>
    [ApiController]
    [Route("api/reports/campaigns")]
    [Authorize]
    public class ReportsCampaignsController : ControllerBase
    {
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IReportsCampaignsService _campaigns;

        public ReportsCampaignsController(
            IRestaurantPermissionHelper permissions,
            IReportsCampaignsService campaigns
        )
        {
            _permissions = permissions;
            _campaigns = campaigns;
        }

        [HttpGet]
        public async Task<IActionResult> GetCampaigns(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var windowError = ReportsQueryGate.TryValidateLocationAndWindow(
                this,
                locationId,
                from,
                to,
                out var fromUtc,
                out var toUtc
            );
            if (windowError != null)
            {
                return windowError;
            }

            var reports = await ReportsQueryGate.AuthorizeReportsViewAsync(
                _permissions,
                User,
                locationId
            );
            var denied = reports.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var dto = await _campaigns.GetCampaignsAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );

            if (dto.LifetimeEmpty)
            {
                return Ok(new
                {
                    success = true,
                    lifetimeEmpty = true,
                });
            }

            return Ok(new
            {
                success = true,
                lifetimeEmpty = false,
                campaignsSent = ReportsQueryGate.WireMetric(dto.CampaignsSent!),
                guestsMessaged = ReportsQueryGate.WireMetric(
                    dto.GuestsMessaged!
                ),
                failedSends = ReportsQueryGate.WireMetric(dto.FailedSends!),
                performance = (dto.Performance ?? []).Select(row => new
                {
                    campaignId = row.CampaignId,
                    name = row.Name,
                    goal = row.Goal,
                    channel = row.Channel,
                    sent = row.Sent,
                    status = row.Status,
                }),
                needsAttention = (dto.NeedsAttention ?? []).Select(row => new
                {
                    campaignId = row.CampaignId,
                    name = row.Name,
                    status = row.Status,
                }),
            });
        }
    }
}
