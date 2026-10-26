using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/home/performance")]
    [Authorize]
    public class HomePerformanceController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;

        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;

        public HomePerformanceController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions
        )
        {
            _context = context;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetPerformance(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (from == null || to == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from and to are required."
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to."
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days."
                });
            }

            var decision = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Reports,
                PermissionLevel.View,
                locationId
            );

            var denied = decision.ToHttpResult();

            if (denied != null)
            {
                return denied;
            }

            // Previous period is the equal-length window immediately before [from, to).
            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var feedbackSubmitted = await _context.Feedbacks
                .CountAsync(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                );

            var feedbackSubmittedPrevious = await _context.Feedbacks
                .CountAsync(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= previousFromUtc
                    && f.CreatedAt < previousToUtc
                );

            var guestsJoined = await _context.LocationGuests
                .CountAsync(lg =>
                    lg.RestaurantLocationId == locationId
                    && lg.CreatedAt >= fromUtc
                    && lg.CreatedAt < toUtc
                );

            var guestsJoinedPrevious = await _context.LocationGuests
                .CountAsync(lg =>
                    lg.RestaurantLocationId == locationId
                    && lg.CreatedAt >= previousFromUtc
                    && lg.CreatedAt < previousToUtc
                );

            var qrScans = await _context.QrScanEvents
                .CountAsync(e =>
                    e.RestaurantLocationId == locationId
                    && e.CreatedAt >= fromUtc
                    && e.CreatedAt < toUtc
                );

            var qrScansPrevious = await _context.QrScanEvents
                .CountAsync(e =>
                    e.RestaurantLocationId == locationId
                    && e.CreatedAt >= previousFromUtc
                    && e.CreatedAt < previousToUtc
                );

            return Ok(new
            {
                success = true,
                feedbackSubmitted,
                feedbackSubmittedPrevious,
                guestsJoined,
                guestsJoinedPrevious,
                qrScans,
                qrScansPrevious
            });
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
