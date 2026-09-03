using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Reports hub overview KPIs (ticket 11). Soft lock does not deny the read.
    /// </summary>
    [ApiController]
    [Route("api/reports/overview")]
    [Authorize]
    public class ReportsOverviewController : ControllerBase
    {
        private const int MaxInclusiveCalendarDays = 180;

        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IReportsOverviewService _overview;

        public ReportsOverviewController(
            IRestaurantPermissionHelper permissions,
            IReportsOverviewService overview
        )
        {
            _permissions = permissions;
            _overview = overview;
        }

        [HttpGet]
        public async Task<IActionResult> GetOverview(
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

            var dto = await _overview.GetOverviewAsync(
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
                    feedbackReceived = WireMetric(dto.Funnel.FeedbackReceived),
                    marketingOptIns = WireMetric(dto.Funnel.MarketingOptIns),
                    offerRedemptions = WireMetric(dto.Funnel.OfferRedemptions),
                    campaignsSent = WireMetric(dto.Funnel.CampaignsSent),
                },
                privateFeedback = new
                {
                    feedbackMessages = WireMetric(
                        dto.PrivateFeedback!.FeedbackMessages
                    ),
                    marketingOptIns = WireMetric(
                        dto.PrivateFeedback.MarketingOptIns
                    ),
                    followUpNeeded = WireMetric(
                        dto.PrivateFeedback.FollowUpNeeded
                    ),
                    followedUp = WireMetric(dto.PrivateFeedback.FollowedUp),
                },
                offersAndCampaigns = new
                {
                    activeOffers = WireMetric(
                        dto.OffersAndCampaigns!.ActiveOffers
                    ),
                    offerClaims = WireMetric(
                        dto.OffersAndCampaigns.OfferClaims
                    ),
                    offerRedemptions = WireMetric(
                        dto.OffersAndCampaigns.OfferRedemptions
                    ),
                    campaignsSent = WireMetric(
                        dto.OffersAndCampaigns.CampaignsSent
                    ),
                    unsubscribes = WireMetric(
                        dto.OffersAndCampaigns.Unsubscribes
                    ),
                },
                topCaptureSources = (dto.TopCaptureSources ?? []).Select(
                    row => new
                    {
                        qrCodeId = row.QrCodeId,
                        source = row.Source,
                        scans = row.Scans,
                        feedback = row.Feedback,
                        marketingOptIns = row.MarketingOptIns,
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
