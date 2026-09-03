using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Feedback report KPIs (ticket 14). Soft lock does not deny the read.
    /// </summary>
    [ApiController]
    [Route("api/reports/feedback")]
    [Authorize]
    public class ReportsFeedbackController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;

        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IReportsFeedbackService _feedback;

        public ReportsFeedbackController(
            IRestaurantPermissionHelper permissions,
            IReportsFeedbackService feedback
        )
        {
            _permissions = permissions;
            _feedback = feedback;
        }

        [HttpGet]
        public async Task<IActionResult> GetFeedbackReport(
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

            var dto = await _feedback.GetFeedbackReportAsync(
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
                kpis = new
                {
                    feedbackReceived = WireMetric(dto.Kpis!.FeedbackReceived),
                    marketingOptIns = WireMetric(dto.Kpis.MarketingOptIns),
                    followUpNeeded = WireMetric(dto.Kpis.FollowUpNeeded),
                    resolved = WireMetric(dto.Kpis.Resolved),
                },
                status = new
                {
                    @new = WireMetric(dto.Status!.New),
                    inProgress = WireMetric(dto.Status.InProgress),
                    followUpNeeded = WireMetric(dto.Status.FollowUpNeeded),
                    resolved = WireMetric(dto.Status.Resolved),
                },
                needsAttention = (dto.NeedsAttention ?? []).Select(
                    row => new
                    {
                        feedbackId = row.FeedbackId,
                        submittedAt = row.SubmittedAt,
                        guestName = row.GuestName,
                        source = row.Source,
                        commentPreview = row.CommentPreview,
                        workflowStatus = row.WorkflowStatus,
                    }
                ),
                bySource = (dto.BySource ?? []).Select(
                    row => new
                    {
                        qrCodeId = row.QrCodeId,
                        source = row.Source,
                        feedback = row.Feedback,
                        marketingOptIns = row.MarketingOptIns,
                        followUpNeeded = row.FollowUpNeeded,
                    }
                ),
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
