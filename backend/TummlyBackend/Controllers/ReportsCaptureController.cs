using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Reports Capture KPIs and placements (ticket 13). Soft lock does not
    /// deny the read.
    /// </summary>
    [ApiController]
    [Route("api/reports/capture")]
    [Authorize]
    public class ReportsCaptureController : ControllerBase
    {
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IReportsCaptureService _capture;

        public ReportsCaptureController(
            IRestaurantPermissionHelper permissions,
            IReportsCaptureService capture
        )
        {
            _permissions = permissions;
            _capture = capture;
        }

        [HttpGet]
        public async Task<IActionResult> GetCapture(
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

            var dto = await _capture.GetCaptureAsync(
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
                funnel = new
                {
                    qrScans = ReportsQueryGate.WireMetric(dto.Funnel!.QrScans),
                    feedbackSubmitted = ReportsQueryGate.WireMetric(
                        dto.Funnel.FeedbackSubmitted
                    ),
                    contactableGuests = ReportsQueryGate.WireMetric(
                        dto.Funnel.ContactableGuests
                    ),
                    offerClaimed = ReportsQueryGate.WireMetric(
                        dto.Funnel.OfferClaimed
                    ),
                },
                placements = (dto.Placements ?? []).Select(
                    row => new
                    {
                        qrCodeId = row.QrCodeId,
                        name = row.Name,
                        status = row.Status,
                        scans = row.Scans,
                        feedback = row.Feedback,
                        contactable = row.Contactable,
                    }
                ),
            });
        }
    }
}
