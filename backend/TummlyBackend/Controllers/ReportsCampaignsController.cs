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
        private const int MaxInclusiveCalendarDays = 180;

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

            if (locationId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            if (from == null || to == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from and to are required.",
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to.",
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days.",
                });
            }

            var reports = await GateReportsViewAsync(locationId);
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
                campaignsSent = WireMetric(dto.CampaignsSent!),
                guestsMessaged = WireMetric(dto.GuestsMessaged!),
                failedSends = WireMetric(dto.FailedSends!),
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

        private Task<RestaurantPermissionDecision> GateReportsViewAsync(
            int locationId
        )
        {
            return _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Reports,
                PermissionLevel.View,
                locationId
            );
        }

        private static object WireMetric(DTOs.Reports.ReportsMetricDto metric)
        {
            return new
            {
                value = metric.Value,
                valuePrevious = metric.ValuePrevious,
            };
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }
    }
}
