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
                    feedbackReceived = ReportsQueryGate.WireMetric(
                        dto.Kpis!.FeedbackReceived
                    ),
                    marketingOptIns = ReportsQueryGate.WireMetric(
                        dto.Kpis.MarketingOptIns
                    ),
                    followUpNeeded = ReportsQueryGate.WireMetric(
                        dto.Kpis.FollowUpNeeded
                    ),
                    resolved = ReportsQueryGate.WireMetric(dto.Kpis.Resolved),
                },
                status = new
                {
                    @new = ReportsQueryGate.WireMetric(dto.Status!.New),
                    inProgress = ReportsQueryGate.WireMetric(
                        dto.Status.InProgress
                    ),
                    followUpNeeded = ReportsQueryGate.WireMetric(
                        dto.Status.FollowUpNeeded
                    ),
                    resolved = ReportsQueryGate.WireMetric(dto.Status.Resolved),
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
    }
}
