using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Offers report KPIs (ticket 15). Soft lock does not deny the read.
    /// </summary>
    [ApiController]
    [Route("api/reports/offers")]
    [Authorize]
    public class ReportsOffersController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;

        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IReportsOffersService _offers;

        public ReportsOffersController(
            IRestaurantPermissionHelper permissions,
            IReportsOffersService offers
        )
        {
            _permissions = permissions;
            _offers = offers;
        }

        [HttpGet]
        public async Task<IActionResult> GetOffersReport(
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

            var dto = await _offers.GetOffersReportAsync(
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
                    activeOffers = WireMetric(dto.Kpis!.ActiveOffers),
                    offerClaims = WireMetric(dto.Kpis.OfferClaims),
                    redemptions = WireMetric(dto.Kpis.Redemptions),
                    redemptionRate = WireRateMetric(dto.Kpis.RedemptionRate),
                    expiredClaims = WireMetric(dto.Kpis.ExpiredClaims),
                    invalidAttempts = WireMetric(dto.Kpis.InvalidAttempts),
                },
                performance = (dto.Performance ?? []).Select(
                    row => new
                    {
                        offerId = row.OfferId,
                        offer = row.Offer,
                        status = row.Status,
                        claims = row.Claims,
                        redemptions = row.Redemptions,
                        rate = row.Rate,
                        expired = row.Expired,
                        invalid = row.Invalid,
                    }
                ),
                recentRedemptions = (dto.RecentRedemptions ?? []).Select(
                    row => new
                    {
                        id = row.Id,
                        dateTimeUtc = row.DateTimeUtc,
                        offerTitle = row.OfferTitle,
                        guestName = row.GuestName,
                        locationName = row.LocationName,
                        outcome = row.Outcome,
                    }
                ),
                controlSignals = (dto.ControlSignals ?? []).Select(WireControlSignal),
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

        private static object WireMetric(ReportsMetricDto metric)
        {
            return new
            {
                value = metric.Value,
                valuePrevious = metric.ValuePrevious,
            };
        }

        private static object WireRateMetric(ReportsRateMetricDto metric)
        {
            return new
            {
                value = metric.Value,
                valuePrevious = metric.ValuePrevious,
            };
        }

        private static object WireControlSignal(object signal)
        {
            if (signal is ReportsOffersRepeatedInvalidSignalDto repeated)
            {
                return new
                {
                    kind = repeated.Kind,
                    count = repeated.Count,
                    target = repeated.Target,
                };
            }

            if (signal is ReportsOffersLowRedemptionSignalDto low)
            {
                return new
                {
                    kind = low.Kind,
                    offerId = low.OfferId,
                    offerTitle = low.OfferTitle,
                    claims = low.Claims,
                    redemptions = low.Redemptions,
                    rate = low.Rate,
                    target = low.Target,
                };
            }

            return signal;
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
