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
        private const int MaxInclusiveCalendarDays = 180;

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
                    qrScans = WireMetric(dto.Funnel!.QrScans),
                    feedbackSubmitted = WireMetric(dto.Funnel.FeedbackSubmitted),
                    contactableGuests = WireMetric(dto.Funnel.ContactableGuests),
                    offerClaimed = WireMetric(dto.Funnel.OfferClaimed),
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
