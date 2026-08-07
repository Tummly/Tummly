using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/campaigns")]
    [Authorize]
    public class CampaignsController : ControllerBase
    {
        private readonly IOwnedLocationService _ownedLocation;
        private readonly ICampaignsListService _campaignsList;
        private readonly ICampaignDraftService _campaignDrafts;

        public CampaignsController(
            IOwnedLocationService ownedLocation,
            ICampaignsListService campaignsList,
            ICampaignDraftService campaignDrafts
        )
        {
            _ownedLocation = ownedLocation;
            _campaignsList = campaignsList;
            _campaignDrafts = campaignDrafts;
        }

        /*
         =========================================
         LOCATION CAMPAIGNS LIST (OWNED)
         =========================================
        */

        [HttpGet]
        public async Task<IActionResult> ListCampaigns(
            [FromQuery] int locationId,
            [FromQuery] string view = "all",
            [FromQuery] string? q = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = CampaignsListService.DefaultPageSize
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var response = await _campaignsList.ListAsync(
                    new CampaignsListQuery
                    {
                        LocationId = locationId,
                        View = view,
                        Q = q,
                        Page = page,
                        PageSize = pageSize,
                    }
                );

                return Ok(new
                {
                    success = true,
                    items = response.Items,
                    totalCount = response.TotalCount,
                    page = response.Page,
                    pageSize = response.PageSize,
                    tabCounts = new
                    {
                        all = response.TabCounts.All,
                        needsAttention = response.TabCounts.NeedsAttention,
                        drafts = response.TabCounts.Drafts,
                        inFlight = response.TabCounts.InFlight,
                        sent = response.TabCounts.Sent,
                    },
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        /*
         =========================================
         CAMPAIGN DRAFT CREATE / GET / PATCH
         =========================================
        */

        [HttpPost]
        public async Task<IActionResult> CreateCampaign(
            [FromBody] CreateCampaignDraftRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, request.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var campaign = await _campaignDrafts.CreateAsync(request);
                return Ok(new
                {
                    success = true,
                    campaign,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpGet("{campaignId:int}")]
        public async Task<IActionResult> GetCampaign(int campaignId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var campaign = await _campaignDrafts.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Campaign not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, campaign.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            return Ok(new
            {
                success = true,
                campaign,
            });
        }

        [HttpPatch("{campaignId:int}")]
        public async Task<IActionResult> PatchCampaign(
            int campaignId,
            [FromBody] PatchCampaignDraftRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var existing = await _campaignDrafts.GetByIdAsync(campaignId);
            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Campaign not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, existing.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _campaignDrafts.PatchAsync(
                    campaignId,
                    request
                );

                return result switch
                {
                    CampaignDraftWriteResult.Ok ok => Ok(new
                    {
                        success = true,
                        campaign = ok.Campaign,
                    }),
                    CampaignDraftWriteResult.NotFound => NotFound(new
                    {
                        success = false,
                        message = "Campaign not found.",
                    }),
                    CampaignDraftWriteResult.Conflict => Conflict(new
                    {
                        success = false,
                        message =
                            "This campaign was updated elsewhere. Reload and try again.",
                    }),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            success = false,
                            message = "Unexpected campaign update result.",
                        }
                    ),
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }
    }
}
