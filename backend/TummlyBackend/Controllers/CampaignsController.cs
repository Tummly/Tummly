using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.DTOs.Guests;
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
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IGuestsEffectiveLocationService _effectiveLocations;
        private readonly ICampaignsListService _campaignsList;
        private readonly ICampaignsSummaryService _campaignsSummary;
        private readonly ICampaignDraftService _campaignDrafts;
        private readonly ICampaignRecommendationService _campaignRecommendation;
        private readonly ICampaignMessageDraftService _campaignMessageDraft;
        private readonly ICampaignSendTestService _campaignSendTest;
        private readonly ICampaignEligibilityService _campaignEligibility;
        private readonly ICampaignScheduleCommitService _campaignScheduleCommit;
        private readonly ICampaignLifecycleService _campaignLifecycle;
        private readonly ICampaignFireService _campaignFire;

        public CampaignsController(
            IRestaurantPermissionHelper permissions,
            IGuestsEffectiveLocationService effectiveLocations,
            ICampaignsListService campaignsList,
            ICampaignsSummaryService campaignsSummary,
            ICampaignDraftService campaignDrafts,
            ICampaignRecommendationService campaignRecommendation,
            ICampaignMessageDraftService campaignMessageDraft,
            ICampaignSendTestService campaignSendTest,
            ICampaignEligibilityService campaignEligibility,
            ICampaignScheduleCommitService campaignScheduleCommit,
            ICampaignLifecycleService campaignLifecycle,
            ICampaignFireService campaignFire
        )
        {
            _permissions = permissions;
            _effectiveLocations = effectiveLocations;
            _campaignsList = campaignsList;
            _campaignsSummary = campaignsSummary;
            _campaignDrafts = campaignDrafts;
            _campaignRecommendation = campaignRecommendation;
            _campaignMessageDraft = campaignMessageDraft;
            _campaignSendTest = campaignSendTest;
            _campaignEligibility = campaignEligibility;
            _campaignScheduleCommit = campaignScheduleCommit;
            _campaignLifecycle = campaignLifecycle;
            _campaignFire = campaignFire;
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
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = CampaignsListService.DefaultPageSize,
            [FromQuery] string[]? status = null,
            [FromQuery] string[]? channel = null,
            [FromQuery] string[]? goalId = null,
            [FromQuery] string[]? offerStance = null,
            [FromQuery] int[]? createdBy = null,
            [FromQuery] string[]? deliveryIssue = null,
            [FromQuery] string? dateAxis = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? locationScope = null,
            [FromQuery] int[]? locationIds = null,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var effectiveLocations = await _effectiveLocations.ResolveAsync(
                    ownedLocation.LocationIds,
                    ownedLocation.Location!,
                    locationScope,
                    locationIds
                );

                var effectiveDenied = effectiveLocations.ToHttpResult();
                if (effectiveDenied != null)
                {
                    return effectiveDenied;
                }

                var response = await _campaignsList.ListAsync(
                    new CampaignsListQuery
                    {
                        LocationId = locationId,
                        LocationIds = effectiveLocations.LocationIds!,
                        LocationNamesById =
                            effectiveLocations.LocationNamesById!,
                        View = view,
                        Q = q,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                        Status = status ?? Array.Empty<string>(),
                        Channel = channel ?? Array.Empty<string>(),
                        GoalId = goalId ?? Array.Empty<string>(),
                        OfferStance = offerStance ?? Array.Empty<string>(),
                        CreatedBy = createdBy ?? Array.Empty<int>(),
                        DeliveryIssue = deliveryIssue ?? Array.Empty<string>(),
                        DateAxis = dateAxis,
                        DatePreset = datePreset,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        UtcOffsetMinutes = utcOffsetMinutes,
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
                    filterCatalog = new
                    {
                        createdBy = response.FilterCatalog.CreatedBy.Select(
                            option => new
                            {
                                id = option.Id,
                                label = option.Label,
                            }
                        ),
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
         CAMPAIGN OVERVIEW SUMMARY (OWNED)
         =========================================
        */

        [HttpGet("summary")]
        public async Task<IActionResult> GetCampaignsSummary(
            [FromQuery] int locationId,
            [FromQuery] DateTime? overviewDateFrom = null,
            [FromQuery] DateTime? overviewDateTo = null
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var summary = await _campaignsSummary.GetSummaryAsync(
                    new CampaignsSummaryQuery
                    {
                        LocationId = locationId,
                        OverviewDateFrom = overviewDateFrom,
                        OverviewDateTo = overviewDateTo,
                    }
                );

                return Ok(new
                {
                    success = true,
                    summary,
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
         CAMPAIGN ELIGIBILITY (AUDIENCE ESTIMATE)
         =========================================
        */

        [HttpGet("eligibility")]
        public async Task<IActionResult> GetCampaignEligibility(
            [FromQuery] int locationId,
            [FromQuery] string audienceKey
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var ownedLocation = await GateLocationAsync(
                locationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var eligibility = await _campaignEligibility.EvaluateAsync(
                    locationId,
                    audienceKey
                );

                return Ok(new
                {
                    success = true,
                    eligibility,
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

            var ownedLocation = await GateLocationAsync(
                request.LocationId
            );
            var denied = ownedLocation.ToHttpResult();
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

            var ownedLocation = await GateLocationAsync(
                request.LocationId
            );
            var denied = ownedLocation.ToHttpResult();
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

            var ownedLocation = await GateLocationAsync(
                request.LocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var campaign = await _campaignDrafts.CreateAsync(
                    request,
                    userId
                );
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

            var ownedLocation = await GateLocationAsync(
                campaign.LocationId
            );
            var denied = ownedLocation.ToHttpResult();
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

            var ownedLocation = await GateLocationAsync(
                locationId.Value
            );
            var denied = ownedLocation.ToHttpResult();
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
         CAMPAIGN SCHEDULE / SEND COMMIT (OWNED)
         =========================================
        */

        [HttpPost("{campaignId:int}/commit")]
        public async Task<IActionResult> CommitCampaignSchedule(
            int campaignId,
            [FromBody] CommitCampaignScheduleRequest request
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

            var ownedLocation = await GateLocationAsync(
                locationId.Value
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _campaignScheduleCommit.CommitAsync(
                    campaignId,
                    request
                );

                return result switch
                {
                    CampaignScheduleCommitResult.Ok ok => Ok(new
                    {
                        success = true,
                        campaign = ok.Campaign,
                    }),
                    CampaignScheduleCommitResult.NotFound => NotFound(new
                    {
                        success = false,
                        message = "Campaign not found.",
                    }),
                    CampaignScheduleCommitResult.NotDraft => Conflict(new
                    {
                        success = false,
                        message = "Only draft campaigns can be committed.",
                    }),
                    CampaignScheduleCommitResult.Conflict => Conflict(new
                    {
                        success = false,
                        message =
                            "This campaign was updated elsewhere. Reload and try again.",
                    }),
                    CampaignScheduleCommitResult.BillingReserveUnavailable =>
                        StatusCode(
                            StatusCodes.Status503ServiceUnavailable,
                            new
                            {
                                success = false,
                                code = "billing_reserve_unavailable",
                                message =
                                    "Billing Reserve is not available. Schedule and send stay blocked. Draft Save and Send test remain allowed.",
                            }
                        ),
                    CampaignScheduleCommitResult.ReserveFailed failed =>
                        UnprocessableEntity(new
                        {
                            success = false,
                            code = "reserve_failed",
                            message = failed.Message,
                        }),
                    CampaignScheduleCommitResult.ZeroEligible =>
                        UnprocessableEntity(new
                        {
                            success = false,
                            code = "zero_eligible",
                            message =
                                "No recipients are eligible on the selected channel.",
                        }),
                    CampaignScheduleCommitResult.InvalidSchedule invalid =>
                        BadRequest(new
                        {
                            success = false,
                            code = "invalid_schedule",
                            message = invalid.Message,
                        }),
                    CampaignScheduleCommitResult.NotReviewReady notReady =>
                        BadRequest(new
                        {
                            success = false,
                            code = "not_review_ready",
                            message = notReady.Message,
                        }),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            success = false,
                            message = "Unexpected campaign commit result.",
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
         CAMPAIGN LIFECYCLE ACTIONS (OWNED) — ticket 30
         =========================================
        */

        [HttpPost("{campaignId:int}/unschedule")]
        public Task<IActionResult> UnscheduleCampaign(
            int campaignId,
            [FromBody] CampaignLifecycleActionRequest request
        ) =>
            ExecuteLifecycleAsync(
                campaignId,
                request,
                (id, body, ct) => _campaignLifecycle.UnscheduleAsync(id, body, ct)
            );

        [HttpPost("{campaignId:int}/pause")]
        public Task<IActionResult> PauseCampaign(
            int campaignId,
            [FromBody] CampaignLifecycleActionRequest request
        ) =>
            ExecuteLifecycleAsync(
                campaignId,
                request,
                (id, body, ct) => _campaignLifecycle.PauseAsync(id, body, ct)
            );

        [HttpPost("{campaignId:int}/cancel")]
        public Task<IActionResult> CancelCampaign(
            int campaignId,
            [FromBody] CampaignLifecycleActionRequest request
        ) =>
            ExecuteLifecycleAsync(
                campaignId,
                request,
                (id, body, ct) => _campaignLifecycle.CancelAsync(id, body, ct)
            );

        [HttpPost("{campaignId:int}/resume")]
        public Task<IActionResult> ResumeCampaign(
            int campaignId,
            [FromBody] CampaignLifecycleActionRequest request
        ) =>
            ExecuteLifecycleAsync(
                campaignId,
                request,
                (id, body, ct) => _campaignLifecycle.ResumeAsync(id, body, ct)
            );

        [HttpPost("{campaignId:int}/retry-remaining")]
        public Task<IActionResult> RetryRemainingCampaign(
            int campaignId,
            [FromBody] CampaignLifecycleActionRequest request
        ) =>
            ExecuteLifecycleAsync(
                campaignId,
                request,
                (id, body, ct) =>
                    _campaignLifecycle.RetryRemainingAsync(id, body, ct)
            );

        [HttpPost("{campaignId:int}/duplicate")]
        public Task<IActionResult> DuplicateCampaign(
            int campaignId,
            [FromBody] CampaignLifecycleActionRequest request
        ) =>
            ExecuteLifecycleAsync(
                campaignId,
                request,
                (id, body, ct) =>
                    _campaignLifecycle.DuplicateAsDraftAsync(id, body, ct)
            );

        [HttpPost("{campaignId:int}/delete")]
        public Task<IActionResult> DeleteDraftCampaign(
            int campaignId,
            [FromBody] CampaignLifecycleActionRequest request
        ) =>
            ExecuteLifecycleAsync(
                campaignId,
                request,
                (id, body, ct) =>
                    _campaignLifecycle.DeleteDraftAsync(id, body, ct)
            );

        private async Task<IActionResult> ExecuteLifecycleAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            Func<
                int,
                CampaignLifecycleActionRequest,
                CancellationToken,
                Task<CampaignLifecycleResult>
            > action
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

            var ownedLocation = await GateLocationAsync(
                locationId.Value
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await action(campaignId, request, CancellationToken.None);

            return result switch
            {
                CampaignLifecycleResult.Ok ok => Ok(new
                {
                    success = true,
                    campaign = ok.Campaign,
                }),
                CampaignLifecycleResult.Duplicated duplicated => Ok(new
                {
                    success = true,
                    campaign = duplicated.Campaign,
                }),
                CampaignLifecycleResult.Deleted => Ok(new
                {
                    success = true,
                }),
                CampaignLifecycleResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Campaign not found.",
                }),
                CampaignLifecycleResult.Conflict => Conflict(new
                {
                    success = false,
                    message =
                        "This campaign was updated elsewhere. Reload and try again.",
                }),
                CampaignLifecycleResult.InvalidStatus invalid => Conflict(new
                {
                    success = false,
                    code = "invalid_status",
                    message = invalid.Message,
                }),
                CampaignLifecycleResult.BillingReserveUnavailable => StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        code = "billing_reserve_unavailable",
                        message =
                            "Billing Reserve is not available. Resume and retry stay blocked.",
                    }
                ),
                CampaignLifecycleResult.ReserveFailed failed =>
                    UnprocessableEntity(new
                    {
                        success = false,
                        code = "reserve_failed",
                        message = failed.Message,
                    }),
                CampaignLifecycleResult.ReleaseFailed releaseFailed =>
                    UnprocessableEntity(new
                    {
                        success = false,
                        code = "release_failed",
                        message = releaseFailed.Message,
                    }),
                CampaignLifecycleResult.ZeroEligible => UnprocessableEntity(new
                {
                    success = false,
                    code = "zero_eligible",
                    message =
                        "No remaining recipients are eligible on the selected channel.",
                }),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unexpected campaign lifecycle result.",
                    }
                ),
            };
        }

        /*
         =========================================
         CAMPAIGN FIRE (OWNED) — execute send / settle
         =========================================
        */

        [HttpPost("{campaignId:int}/fire")]
        public async Task<IActionResult> FireCampaign(int campaignId)
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

            var ownedLocation = await GateLocationAsync(
                locationId.Value
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _campaignFire.FireAsync(campaignId);

            return result switch
            {
                CampaignFireResult.Ok ok => Ok(new
                {
                    success = true,
                    campaign = ok.Campaign,
                }),
                CampaignFireResult.CannotStart cannotStart => Ok(new
                {
                    success = true,
                    campaign = cannotStart.Campaign,
                    code = "cannot_start",
                }),
                CampaignFireResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Campaign not found.",
                }),
                CampaignFireResult.NotDue => Conflict(new
                {
                    success = false,
                    code = "not_due",
                    message = "Scheduled campaign is not due yet.",
                }),
                CampaignFireResult.NotFireable notFireable => Conflict(new
                {
                    success = false,
                    code = "not_fireable",
                    message = notFireable.Message,
                }),
                CampaignFireResult.Conflict => Conflict(new
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
                        message = "Unexpected campaign fire result.",
                    }
                ),
            };
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

            var ownedLocation = await GateLocationAsync(
                dto.LocationId
            );
            var denied = ownedLocation.ToHttpResult();
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

        private async Task<RestaurantPermissionDecision> GateLocationAsync(
            int locationId
        )
        {
            var minimum = HttpMethods.IsGet(Request.Method)
                ? PermissionLevel.View
                : PermissionLevel.Manage;
            return await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Campaigns,
                minimum,
                locationId
            );
        }
    }
}
