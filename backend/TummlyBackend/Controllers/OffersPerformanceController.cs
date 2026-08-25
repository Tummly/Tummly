using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Offers Performance strip KPIs (ticket 29). Dedicated route avoids
    /// colliding with GET api/offers/{offerId}.
    /// </summary>
    [ApiController]
    [Route("api/offers/performance")]
    [Authorize]
    public class OffersPerformanceController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;

        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IOffersMetricsService _metrics;

        public OffersPerformanceController(
            IRestaurantPermissionHelper permissions,
            IOffersMetricsService metrics
        )
        {
            _permissions = permissions;
            _metrics = metrics;
        }

        [HttpGet]
        public async Task<IActionResult> GetPerformance(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
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

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var dto = await _metrics.GetPerformanceAsync(
                locationId,
                fromUtc,
                toUtc
            );

            return Ok(new
            {
                success = true,
                activeOffers = dto.ActiveOffers,
                offersIssued = dto.OffersIssued,
                claims = dto.Claims,
                redemptions = dto.Redemptions,
                claimToRedemptionRate = dto.ClaimToRedemptionRate,
            });
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

        private async Task<RestaurantPermissionDecision> GateLocationAsync(
            int locationId
        )
        {
            var minimum = HttpMethods.IsGet(Request.Method)
                ? PermissionLevel.View
                : PermissionLevel.Manage;
            return await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Offers,
                minimum,
                locationId
            );
        }
    }
}
