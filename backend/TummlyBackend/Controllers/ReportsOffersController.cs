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
                    activeOffers = ReportsQueryGate.WireMetric(
                        dto.Kpis!.ActiveOffers
                    ),
                    offerClaims = ReportsQueryGate.WireMetric(
                        dto.Kpis.OfferClaims
                    ),
                    redemptions = ReportsQueryGate.WireMetric(
                        dto.Kpis.Redemptions
                    ),
                    redemptionRate = WireRateMetric(dto.Kpis.RedemptionRate),
                    expiredClaims = ReportsQueryGate.WireMetric(
                        dto.Kpis.ExpiredClaims
                    ),
                    invalidAttempts = ReportsQueryGate.WireMetric(
                        dto.Kpis.InvalidAttempts
                    ),
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
                controlSignals = (dto.ControlSignals ?? []).Select(
                    WireControlSignal
                ),
            });
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
    }
}
