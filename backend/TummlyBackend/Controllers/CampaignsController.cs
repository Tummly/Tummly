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

        public CampaignsController(
            IOwnedLocationService ownedLocation,
            ICampaignsListService campaignsList
        )
        {
            _ownedLocation = ownedLocation;
            _campaignsList = campaignsList;
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
    }
}
