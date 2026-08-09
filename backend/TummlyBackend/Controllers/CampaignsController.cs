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
        private readonly ICampaignRecommendationService _campaignRecommendation;
        private readonly ICampaignMessageDraftService _campaignMessageDraft;
        private readonly ICampaignSendTestService _campaignSendTest;

        public CampaignsController(
            IOwnedLocationService ownedLocation,
            ICampaignsListService campaignsList,
            ICampaignDraftService campaignDrafts,
            ICampaignRecommendationService campaignRecommendation,
            ICampaignMessageDraftService campaignMessageDraft,
            ICampaignSendTestService campaignSendTest
        )
        {
            _ownedLocation = ownedLocation;
            _campaignsList = campaignsList;
            _campaignDrafts = campaignDrafts;
            _campaignRecommendation = campaignRecommendation;
            _campaignMessageDraft = campaignMessageDraft;
            _campaignSendTest = campaignSendTest;
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
         CAMPAIGN RECOMMENDATION (AI)
         =========================================
        */

        [HttpPost("recommendation")]
        public async Task<IActionResult> RecommendCampaign(
            [FromBody] CampaignRecommendationRequest request
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
                var result = await _campaignRecommendation.RecommendAsync(
                    userId,
                    request
                );

                return result switch
                {
                    CampaignRecommendationServiceResult.Ok ok => Ok(new
                    {
                        success = true,
                        recommendation = ok.Recommendation,
                    }),
                    CampaignRecommendationServiceResult.Failed failed => StatusCode(
                        StatusCodes.Status502BadGateway,
                        new
                        {
                            success = false,
                            message = failed.Message,
                            retryable = failed.Retryable,
                        }
                    ),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            success = false,
                            message = "Unexpected campaign recommendation result.",
                            retryable = true,
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

        /*
         =========================================
         CAMPAIGN MESSAGE DRAFT (AI)
         =========================================
        */

        [HttpPost("message-draft")]
        public async Task<IActionResult> PrepareMessageDraft(
            [FromBody] PrepareCampaignMessageDraftRequest request
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
                var result = await _campaignMessageDraft.PrepareAsync(
                    ownedLocation.Location!.LocationName,
                    request
                );

                return result switch
                {
                    CampaignMessageDraftServiceResult.Ok ok => Ok(new
                    {
                        success = true,
                        body = ok.Body,
                        subject = ok.Subject,
                        channel = ok.Channel,
                    }),
                    CampaignMessageDraftServiceResult.Failed failed => StatusCode(
                        StatusCodes.Status502BadGateway,
                        new
                        {
                            success = false,
                            message = failed.Message,
                            retryable = failed.Retryable,
                        }
                    ),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            success = false,
                            message = "Unexpected campaign message draft result.",
                            retryable = true,
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
                    retryable = false,
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

            var locationId = await _campaignDrafts.GetLocationIdAsync(campaignId);
            if (locationId == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Campaign not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, locationId.Value);

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
                    CampaignDraftWriteResult.NotDraft => Conflict(new
                    {
                        success = false,
                        message = "Only draft campaigns can be updated.",
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

        /*
         =========================================
         CAMPAIGN SEND TEST (OWNED) — nominated Email
         =========================================
        */

        [HttpPost("send-test")]
        public async Task<IActionResult> SendCampaignTest(
            [FromBody] CampaignSendTestRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await _ownedLocation.ResolveAsync(
                userId,
                dto.LocationId
            );

            var denied = OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _campaignSendTest.SendAsync(
                    dto.LocationId,
                    dto.ToEmail,
                    dto.Subject,
                    dto.Body,
                    dto.Offer
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Location not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                });
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains("Failed to send", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("Resend failed", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("via Resend", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("Email is not configured", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = "We could not send the test email. Try again.",
                        retryable = true,
                    }
                );
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
