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
                    qrScans = ReportsQueryGate.WireMetric(dto.Funnel!.QrScans),
                    feedbackReceived = ReportsQueryGate.WireMetric(
                        dto.Funnel.FeedbackReceived
                    ),
                    marketingOptIns = ReportsQueryGate.WireMetric(
                        dto.Funnel.MarketingOptIns
                    ),
                    offerRedemptions = ReportsQueryGate.WireMetric(
                        dto.Funnel.OfferRedemptions
                    ),
                    campaignsSent = ReportsQueryGate.WireMetric(
                        dto.Funnel.CampaignsSent
                    ),
                },
                privateFeedback = new
                {
                    feedbackMessages = ReportsQueryGate.WireMetric(
                        dto.PrivateFeedback!.FeedbackMessages
                    ),
                    marketingOptIns = ReportsQueryGate.WireMetric(
                        dto.PrivateFeedback.MarketingOptIns
                    ),
                    followUpNeeded = ReportsQueryGate.WireMetric(
                        dto.PrivateFeedback.FollowUpNeeded
                    ),
                    followedUp = ReportsQueryGate.WireMetric(
                        dto.PrivateFeedback.FollowedUp
                    ),
                },
                offersAndCampaigns = new
                {
                    activeOffers = ReportsQueryGate.WireMetric(
                        dto.OffersAndCampaigns!.ActiveOffers
                    ),
                    offerClaims = ReportsQueryGate.WireMetric(
                        dto.OffersAndCampaigns.OfferClaims
                    ),
                    offerRedemptions = ReportsQueryGate.WireMetric(
                        dto.OffersAndCampaigns.OfferRedemptions
                    ),
                    campaignsSent = ReportsQueryGate.WireMetric(
                        dto.OffersAndCampaigns.CampaignsSent
                    ),
                    unsubscribes = ReportsQueryGate.WireMetric(
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
    }
}
