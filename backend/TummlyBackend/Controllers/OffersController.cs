using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Offers catalog — create / list / lifecycle for Campaign attach (ticket 22).
    /// Not Feedback /recovery-offers endpoints.
    /// </summary>
    [ApiController]
    [Route("api/offers")]
    [Authorize]
    public class OffersController : ControllerBase
    {
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IOffersCatalogService _offers;
        private readonly IOffersMetricsService _metrics;
        private readonly IOfferIssueService _offerIssues;
        private readonly IOfferLifecycleService _lifecycle;
        private readonly IOfferVoidRequestService _voidRequests;

        public OffersController(
            IOwnedLocationService ownedLocation,
            IOffersCatalogService offers,
            IOffersMetricsService metrics,
            IOfferIssueService offerIssues,
            IOfferLifecycleService lifecycle,
            IOfferVoidRequestService voidRequests
        )
        {
            _ownedLocation = ownedLocation;
            _offers = offers;
            _metrics = metrics;
            _offerIssues = offerIssues;
            _lifecycle = lifecycle;
            _voidRequests = voidRequests;
        }

        [HttpGet]
        public async Task<IActionResult> ListOffers(
            [FromQuery] int locationId,
            [FromQuery] string view = "all",
            [FromQuery] string? q = null,
            [FromQuery] string sort = "recent-activity",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = OffersCatalogService.DefaultPageSize,
            [FromQuery] string[]? status = null,
            [FromQuery] string[]? attachSource = null,
            [FromQuery] int utcOffsetMinutes = 0
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
                var response = await _offers.ListAsync(
                    new CatalogOffersListQuery
                    {
                        LocationId = locationId,
                        View = view,
                        Q = q,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
                        Status = status ?? Array.Empty<string>(),
                        AttachSource = attachSource ?? Array.Empty<string>(),
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

        [HttpPost]
        public async Task<IActionResult> CreateOffer(
            [FromBody] CreateCatalogOfferRequest request
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
                var offer = await _offers.CreateActiveAsync(request, userId);
                return Ok(new
                {
                    success = true,
                    offer,
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

        /// <summary>
        /// Persist a stored Offers catalog Draft (badge Draft, not attachable until Active).
        /// Distinct from <see cref="CreateOffer"/> which always creates Active.
        /// </summary>
        [HttpPost("draft")]
        public async Task<IActionResult> CreateOfferDraft(
            [FromBody] CreateCatalogOfferRequest request
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
                var offer = await _offers.CreateDraftAsync(request, userId);
                return Ok(new
                {
                    success = true,
                    offer,
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

        [HttpPut("{offerId:int}")]
        public async Task<IActionResult> UpdateOffer(
            int offerId,
            [FromBody] CreateCatalogOfferRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var existing = await _offers.GetByIdAsync(offerId);
            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
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
                var result = await _offers.UpdateAsync(offerId, request);

                return result switch
                {
                    CatalogOfferLifecycleResult.Ok ok => Ok(new
                    {
                        success = true,
                        offer = ok.Offer,
                    }),
                    CatalogOfferLifecycleResult.NotFound => NotFound(new
                    {
                        success = false,
                        message = "Offer not found.",
                    }),
                    CatalogOfferLifecycleResult.InvalidStatus invalid => Conflict(new
                    {
                        success = false,
                        code = "invalid_status",
                        message = invalid.Message,
                    }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError),
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

        [HttpGet("{offerId:int}")]
        public async Task<IActionResult> GetOffer(
            int offerId,
            [FromQuery] int utcOffsetMinutes = 0
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var offer = await _offers.GetByIdAsync(offerId, utcOffsetMinutes);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            return Ok(new
            {
                success = true,
                offer,
            });
        }

        [HttpGet("{offerId:int}/metrics")]
        public async Task<IActionResult> GetOfferMetrics(
            int offerId,
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
            if (inclusiveCalendarDays > 180)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days.",
                });
            }

            var offer = await _offers.GetByIdAsync(offerId);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var dto = await _metrics.GetOfferMetricsAsync(
                offerId,
                fromUtc,
                toUtc
            );

            if (dto == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            return Ok(new
            {
                success = true,
                claims = dto.Claims,
                redemptions = dto.Redemptions,
                redemptionRate = dto.RedemptionRate,
                expiredUnused = dto.ExpiredUnused,
                failedAttempts = dto.FailedAttempts,
            });
        }

        /// <summary>
        /// Offer Details Claims tab — live issue rows for one catalog offer (ticket 40).
        /// </summary>
        [HttpGet("{offerId:int}/claims")]
        public async Task<IActionResult> ListOfferClaims(int offerId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var offer = await _offers.GetByIdAsync(offerId);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var dto = await _lifecycle.ListClaimsAsync(
                offerId,
                DateTime.UtcNow
            );

            if (dto == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            return Ok(new
            {
                success = true,
                items = dto.Items.Select(item => new
                {
                    id = item.Id,
                    guestName = item.GuestName,
                    guestId = item.GuestId,
                    claimCode = item.ClaimCode,
                    claimedAtUtc = item.ClaimedAtUtc,
                    issuedAtUtc = item.IssuedAtUtc,
                    source = item.Source,
                    sourceLabel = item.SourceLabel,
                    campaignName = item.CampaignName,
                    locationName = item.LocationName,
                    expiryAtUtc = item.ExpiryAtUtc,
                    status = item.Status,
                    statusLabel = item.StatusLabel,
                    passCodeMasked = item.PassCodeMasked,
                    offerTitle = item.OfferTitle,
                    linkedCampaignText = item.LinkedCampaignText,
                }),
            });
        }

        /// <summary>
        /// Offer Details Redemptions tab — redeemed + failed staff attempts (ticket 40).
        /// </summary>
        [HttpGet("{offerId:int}/redemptions")]
        public async Task<IActionResult> ListOfferRedemptions(int offerId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var offer = await _offers.GetByIdAsync(offerId);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var dto = await _lifecycle.ListRedemptionsAsync(
                offerId,
                DateTime.UtcNow
            );

            if (dto == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            return Ok(new
            {
                success = true,
                items = dto.Items.Select(MapRedemptionListItem),
            });
        }

        /// <summary>
        /// Location-wide redemption log — redeemed + failed attempts (ticket 42).
        /// </summary>
        [HttpGet("redemptions")]
        public async Task<IActionResult> ListLocationRedemptions(
            [FromQuery] int locationId
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

            var dto = await _lifecycle.ListLocationRedemptionsAsync(locationId);

            return Ok(new
            {
                success = true,
                items = dto.Items.Select(MapRedemptionListItem),
            });
        }

        /// <summary>
        /// Offer Details Campaigns → Linked campaigns (ticket 41).
        /// </summary>
        [HttpGet("{offerId:int}/linked-campaigns")]
        public async Task<IActionResult> ListOfferLinkedCampaigns(int offerId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var offer = await _offers.GetByIdAsync(offerId);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var dto = await _lifecycle.ListLinkedCampaignsAsync(offerId);
            if (dto == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            return Ok(new
            {
                success = true,
                items = dto.Items.Select(item => new
                {
                    id = item.Id,
                    campaignName = item.CampaignName,
                    status = item.Status,
                    statusLabel = item.StatusLabel,
                    locationName = item.LocationName,
                    channelLabel = item.ChannelLabel,
                    audienceLabel = item.AudienceLabel,
                    offerVersionLabel = item.OfferVersionLabel,
                    passesIssued = item.PassesIssued,
                    claims = item.Claims,
                    redemptions = item.Redemptions,
                    sendDateUtc = item.SendDateUtc,
                    sendDateLabel = item.SendDateLabel,
                }),
            });
        }

        /// <summary>
        /// Offer Details Campaigns → Issuance sources (ticket 41).
        /// </summary>
        [HttpGet("{offerId:int}/issuance-sources")]
        public async Task<IActionResult> ListOfferIssuanceSources(int offerId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var offer = await _offers.GetByIdAsync(offerId);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var dto = await _lifecycle.ListIssuanceSourcesAsync(offerId);
            if (dto == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            return Ok(new
            {
                success = true,
                items = dto.Items.Select(item => new
                {
                    id = item.Id,
                    sourceLabel = item.SourceLabel,
                    pathLabel = item.PathLabel,
                    passesIssued = item.PassesIssued,
                    lastIssuedAtUtc = item.LastIssuedAtUtc,
                    lastIssuedLabel = item.LastIssuedLabel,
                }),
            });
        }

        /// <summary>
        /// Offer Details Void requests tab — persisted rows for one catalog offer (ticket 41).
        /// </summary>
        [HttpGet("{offerId:int}/void-requests")]
        public async Task<IActionResult> ListOfferVoidRequests(int offerId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var offer = await _offers.GetByIdAsync(offerId);
            if (offer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, offer.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var dto = await _voidRequests.ListForOfferAsync(offerId);
            if (dto == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                });
            }

            return Ok(new
            {
                success = true,
                items = dto.Items.Select(item => new
                {
                    requestId = item.RequestId,
                    requestedAtUtc = item.RequestedAtUtc,
                    requestedAtText = item.RequestedAtText,
                    requestedByText = item.RequestedByText,
                    guestName = item.GuestName,
                    offerPassText = item.OfferPassText,
                    reasonId = item.ReasonId,
                    reasonText = item.ReasonText,
                    explanation = item.Explanation,
                    locationName = item.LocationName,
                    currentStateText = item.CurrentStateText,
                    correctionId = item.CorrectionId,
                    correctionText = item.CorrectionText,
                    status = item.Status,
                    statusLabel = item.StatusLabel,
                    passId = item.PassId,
                    passCodeMasked = item.PassCodeMasked,
                    expiresText = item.ExpiresText,
                    linkedCampaignText = item.LinkedCampaignText,
                    offerTitle = item.OfferTitle,
                }),
            });
        }

        private static object MapRedemptionListItem(
            OfferDetailsRedemptionListItemDto item
        ) => new
        {
            id = item.Id,
            kind = item.Kind,
            dateTimeUtc = item.DateTimeUtc,
            guestName = item.GuestName,
            guestId = item.GuestId,
            passReferenceText = item.PassReferenceText,
            passId = item.PassId,
            passCodeMasked = item.PassCodeMasked,
            locationName = item.LocationName,
            staffMemberText = item.StaffMemberText,
            outcome = item.Outcome,
            outcomeLabel = item.OutcomeLabel,
            reason = item.Reason,
            reasonLabel = item.ReasonLabel,
            offerVersionLabel = item.OfferVersionLabel,
            expiresAtUtc = item.ExpiresAtUtc,
            linkedCampaignText = item.LinkedCampaignText,
            offerTitle = item.OfferTitle,
        };

        /// <summary>
        /// Staff Redeem — Check offer (ticket 38). Location-wide Claim code lookup.
        /// </summary>
        [HttpPost("redeem/check")]
        public async Task<IActionResult> CheckRedeem(
            [FromBody] OfferRedeemCheckRequest request
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

            var result = await _offerIssues.CheckClaimCodeAsync(
                request.LocationId,
                request.Code ?? string.Empty,
                DateTime.UtcNow
            );

            return result switch
            {
                OfferRedeemCheckResult.Ok ok => Ok(new
                {
                    success = true,
                    preview = new
                    {
                        issueId = ok.Preview.IssueId,
                        offerTitle = ok.Preview.OfferTitle,
                        guestName = ok.Preview.GuestName,
                        validAt = ok.Preview.ValidAt,
                        expires = ok.Preview.Expires,
                        usage = ok.Preview.Usage,
                        staffInstruction = ok.Preview.StaffInstruction,
                    },
                }),
                OfferRedeemCheckResult.Failed failed => Ok(new
                {
                    success = false,
                    reason = failed.Reason,
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
            };
        }

        /// <summary>
        /// Staff Redeem — Mark as redeemed (ticket 38). Persists RedeemedAt.
        /// </summary>
        [HttpPost("redeem")]
        public async Task<IActionResult> MarkRedeemed(
            [FromBody] OfferRedeemMarkRequest request
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

            var result = await _offerIssues.RedeemClaimCodeAsync(
                request.LocationId,
                request.Code ?? string.Empty,
                request.IssueId ?? string.Empty,
                DateTime.UtcNow
            );

            return result switch
            {
                OfferRedeemMarkResult.Ok => Ok(new
                {
                    success = true,
                }),
                OfferRedeemMarkResult.Failed failed => Ok(new
                {
                    success = false,
                    reason = failed.Reason,
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
            };
        }

        /// <summary>
        /// Pending void requests grouped by catalog offer (Needs attention, ticket 39).
        /// </summary>
        [HttpGet("void-requests/open-attention")]
        public async Task<IActionResult> ListOpenVoidAttention(
            [FromQuery] int locationId
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

            var items = await _voidRequests.ListOpenAttentionAsync(locationId);

            return Ok(new
            {
                success = true,
                items,
            });
        }

        [HttpGet("void-requests/{requestId:int}")]
        public async Task<IActionResult> GetVoidRequest(int requestId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var detail = await _voidRequests.GetDetailAsync(requestId);
            if (detail == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Void request not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, detail.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            return Ok(new
            {
                success = true,
                request = detail,
            });
        }

        [HttpPost("void-requests")]
        public async Task<IActionResult> CreateVoidRequest(
            [FromBody] CreateOfferVoidRequestBody request
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

            var result = await _voidRequests.CreateAsync(
                userId,
                request,
                DateTime.UtcNow
            );

            return result.Status switch
            {
                OfferVoidCreateResultStatus.Created => Ok(new
                {
                    success = true,
                    requestId = result.RequestId,
                }),
                OfferVoidCreateResultStatus.PendingExists => Ok(new
                {
                    success = false,
                    reason = "pending_exists",
                }),
                OfferVoidCreateResultStatus.NotRedeemed => Ok(new
                {
                    success = false,
                    reason = "not_redeemed",
                }),
                OfferVoidCreateResultStatus.NotFound => NotFound(new
                {
                    success = false,
                    message = "Pass not found.",
                }),
                _ => BadRequest(new
                {
                    success = false,
                    message = "Invalid void request.",
                }),
            };
        }

        [HttpPost("void-requests/{requestId:int}/approve")]
        public Task<IActionResult> ApproveVoidRequest(int requestId)
            => ExecuteVoidOutcomeAsync(
                requestId,
                (id, user, ct) => _voidRequests.ApproveAsync(user, id, DateTime.UtcNow, ct)
            );

        [HttpPost("void-requests/{requestId:int}/reject")]
        public Task<IActionResult> RejectVoidRequest(int requestId)
            => ExecuteVoidOutcomeAsync(
                requestId,
                (id, user, ct) => _voidRequests.RejectAsync(user, id, DateTime.UtcNow, ct)
            );

        [HttpPost("void-requests/{requestId:int}/notify-approvers")]
        public Task<IActionResult> NotifyVoidApprovers(int requestId)
            => ExecuteVoidNotifyAsync(
                requestId,
                id => _voidRequests.NotifyApproversAsync(id)
            );

        [HttpPost("void-requests/{requestId:int}/notify-submitter")]
        public Task<IActionResult> NotifyVoidSubmitter(
            int requestId,
            [FromBody] NotifyVoidSubmitterBody body
        )
            => ExecuteVoidNotifyAsync(
                requestId,
                id => _voidRequests.NotifySubmitterAsync(id, body.Outcome ?? string.Empty)
            );

        private async Task<IActionResult> ExecuteVoidOutcomeAsync(
            int requestId,
            Func<int, int, CancellationToken, Task<OfferVoidOutcomeResult>> action
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var detail = await _voidRequests.GetDetailAsync(requestId);
            if (detail == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Void request not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, detail.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            var result = await action(requestId, userId, CancellationToken.None);

            return result.Status switch
            {
                OfferVoidOutcomeResultStatus.Ok => Ok(new { success = true }),
                OfferVoidOutcomeResultStatus.NotPending => Ok(new
                {
                    success = false,
                    reason = "not_pending",
                }),
                OfferVoidOutcomeResultStatus.NotFound => NotFound(new
                {
                    success = false,
                    message = "Void request not found.",
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
            };
        }

        private async Task<IActionResult> ExecuteVoidNotifyAsync(
            int requestId,
            Func<int, Task> notify
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var detail = await _voidRequests.GetDetailAsync(requestId);
            if (detail == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Void request not found.",
                });
            }

            var ownedLocation =
                await _ownedLocation.ResolveAsync(userId, detail.LocationId);

            var denied =
                OwnedLocationResponses.FromResult(ownedLocation);

            if (denied != null)
            {
                return denied;
            }

            await notify(requestId);

            return Ok(new { success = true });
        }

        [HttpPost("{offerId:int}/pause")]
        public Task<IActionResult> PauseOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, _, ct) => _offers.PauseAsync(id, cancellationToken: ct)
            );

        [HttpPost("{offerId:int}/resume")]
        public Task<IActionResult> ResumeOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, _, ct) => _offers.ResumeAsync(id, cancellationToken: ct)
            );

        [HttpPost("{offerId:int}/archive")]
        public Task<IActionResult> ArchiveOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, _, ct) => _offers.ArchiveAsync(id, cancellationToken: ct)
            );

        [HttpPost("{offerId:int}/duplicate")]
        public Task<IActionResult> DuplicateOffer(int offerId)
            => ExecuteLifecycleAsync(
                offerId,
                (id, userId, ct) => _offers.DuplicateAsync(
                    id,
                    createdByUserId: userId,
                    cancellationToken: ct
                )
            );

        private async Task<IActionResult> ExecuteLifecycleAsync(
            int offerId,
            Func<int, int, CancellationToken, Task<CatalogOfferLifecycleResult>> action
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var existing = await _offers.GetByIdAsync(offerId);
            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
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

            var result = await action(offerId, userId, CancellationToken.None);

            return result switch
            {
                CatalogOfferLifecycleResult.Ok ok => Ok(new
                {
                    success = true,
                    offer = ok.Offer,
                }),
                CatalogOfferLifecycleResult.Duplicated duplicated => Ok(new
                {
                    success = true,
                    offer = duplicated.Offer,
                }),
                CatalogOfferLifecycleResult.NotFound => NotFound(new
                {
                    success = false,
                    message = "Offer not found.",
                }),
                CatalogOfferLifecycleResult.InvalidStatus invalid => Conflict(new
                {
                    success = false,
                    code = "invalid_status",
                    message = invalid.Message,
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
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
