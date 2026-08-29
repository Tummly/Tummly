using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IGuestTaggingService _guestTagging;
        private readonly IFeedbackInternalNotesService _internalNotes;
        private readonly IFeedbackClassificationCorrectionsService _corrections;
        private readonly IFeedbackDetectedTagsService _detectedTags;
        private readonly IFeedbackWorkflowStatusChangesService _workflowStatusChanges;
        private readonly IFeedbackCloseOutsService _closeOuts;
        private readonly IFeedbackGuestResponsesService _guestResponses;
        private readonly IFeedbackGuestPreviewSendTestService _guestPreviewSendTest;
        private readonly IFeedbackInternalActionsService _internalActions;
        private readonly IFeedbackRespondAndRecordService _respondAndRecord;
        private readonly IFeedbackRecoveryOffersService _recoveryOffers;
        private readonly IFeedbackRecoveryOfferAttachService _recoveryOfferAttach;
        private readonly IFeedbackRecoveryCompletionsService _recoveryCompletions;
        private readonly IFeedbackRecoveryDraftsService _recoveryDrafts;
        private readonly IBilledAiActionCoordinator _billedAi;
        private readonly IFeedbackInboxListService _inboxList;

        public FeedbackController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IGuestTaggingService guestTagging,
            IFeedbackInternalNotesService internalNotes,
            IFeedbackClassificationCorrectionsService corrections,
            IFeedbackDetectedTagsService detectedTags,
            IFeedbackWorkflowStatusChangesService workflowStatusChanges,
            IFeedbackCloseOutsService closeOuts,
            IFeedbackGuestResponsesService guestResponses,
            IFeedbackGuestPreviewSendTestService guestPreviewSendTest,
            IFeedbackInternalActionsService internalActions,
            IFeedbackRespondAndRecordService respondAndRecord,
            IFeedbackRecoveryOffersService recoveryOffers,
            IFeedbackRecoveryOfferAttachService recoveryOfferAttach,
            IFeedbackRecoveryCompletionsService recoveryCompletions,
            IFeedbackRecoveryDraftsService recoveryDrafts,
            IBilledAiActionCoordinator billedAi,
            IFeedbackInboxListService inboxList
        )
        {
            _context = context;
            _permissions = permissions;
            _guestTagging = guestTagging;
            _internalNotes = internalNotes;
            _corrections = corrections;
            _detectedTags = detectedTags;
            _workflowStatusChanges = workflowStatusChanges;
            _closeOuts = closeOuts;
            _guestResponses = guestResponses;
            _guestPreviewSendTest = guestPreviewSendTest;
            _internalActions = internalActions;
            _respondAndRecord = respondAndRecord;
            _recoveryOffers = recoveryOffers;
            _recoveryOfferAttach = recoveryOfferAttach;
            _recoveryCompletions = recoveryCompletions;
            _recoveryDrafts = recoveryDrafts;
            _billedAi = billedAi;
            _inboxList = inboxList;
        }

        /*
         =========================================
         GET FEEDBACK STATS FOR A LOCATION
         =========================================
        */

        [HttpGet]
        public async Task<IActionResult> GetFeedback(
            [FromQuery] int locationId
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

            /*
             =========================================
             FEEDBACK STATS
             =========================================
            */

            var total = await _context.Feedbacks
                .CountAsync(f =>
                    f.RestaurantLocationId == locationId
                );

            var recentRows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recent = recentRows.Select(f =>
            {
                var classification =
                    FeedbackClassificationMapping.ToApiFields(f);

                return new
                {
                    id = f.Id,
                    guestName = f.GuestName,
                    guestContact = f.GuestContact,
                    contactType = f.ContactType.ToString(),
                    comment = f.Comment,
                    createdAt = f.CreatedAt,
                    classificationStatus =
                        classification.ClassificationStatus,
                    sentiment = classification.Sentiment,
                    detectedTags = classification.DetectedTags
                };
            });

            return Ok(new
            {
                success = true,
                total,
                recent
            });
        }

        /*
         =========================================
         LOCATION FEEDBACK SUMMARY (OWNED + RANGE)
         =========================================
        */

        private const int MaxInclusiveCalendarDays = 180;

        [HttpGet("summary")]
        public async Task<IActionResult> GetFeedbackSummary(
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
                    message = "from and to are required."
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to."
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days."
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

            // Previous period is the equal-length window immediately before [from, to).
            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var currentRows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                )
                .Select(f => new
                {
                    f.ClassificationStatus,
                    f.Sentiment,
                    f.WorkflowStatus,
                })
                .ToListAsync();

            var previousRows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= previousFromUtc
                    && f.CreatedAt < previousToUtc
                )
                .Select(f => new
                {
                    f.ClassificationStatus,
                    f.Sentiment,
                })
                .ToListAsync();

            static int CountSentiment(
                IEnumerable<(
                    ClassificationStatus ClassificationStatus,
                    FeedbackSentiment? Sentiment
                )> rows,
                FeedbackSentiment sentiment
            ) => rows.Count(r =>
                r.ClassificationStatus == ClassificationStatus.Succeeded
                && r.Sentiment == sentiment
            );

            var currentFacts = currentRows
                .Select(r => (r.ClassificationStatus, r.Sentiment))
                .ToList();
            var previousFacts = previousRows
                .Select(r => (r.ClassificationStatus, r.Sentiment))
                .ToList();

            var needsAttentionTotal = currentRows.Count(r =>
                r.ClassificationStatus == ClassificationStatus.Succeeded
                && r.Sentiment == FeedbackSentiment.Negative
                && r.WorkflowStatus != FeedbackWorkflowStatus.Resolved
            );

            return Ok(new
            {
                success = true,
                total = currentFacts.Count,
                positive = CountSentiment(
                    currentFacts,
                    FeedbackSentiment.Positive
                ),
                neutral = CountSentiment(
                    currentFacts,
                    FeedbackSentiment.Neutral
                ),
                negative = CountSentiment(
                    currentFacts,
                    FeedbackSentiment.Negative
                ),
                totalPrevious = previousFacts.Count,
                positivePrevious = CountSentiment(
                    previousFacts,
                    FeedbackSentiment.Positive
                ),
                neutralPrevious = CountSentiment(
                    previousFacts,
                    FeedbackSentiment.Neutral
                ),
                negativePrevious = CountSentiment(
                    previousFacts,
                    FeedbackSentiment.Negative
                ),
                needsAttentionTotal,
            });
        }

        /*
         =========================================
         LOCATION FEEDBACK INBOX (OWNED + RANGE)
         =========================================
        */

        [HttpGet("inbox")]
        public async Task<IActionResult> GetFeedbackInbox(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string tab = "all",
            [FromQuery] string? q = null,
            [FromQuery] string[]? sentiment = null,
            [FromQuery] string[]? detectedTags = null,
            [FromQuery] string[]? qrSource = null,
            [FromQuery] string[]? contact = null,
            [FromQuery] string[]? workflowStatus = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string sort = "newest-submitted",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int utcOffsetMinutes = 0
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
                    message = "from and to are required."
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to."
                });
            }

            // Inbox is not capped at 180 days: Home Needs attention uses an
            // all-time window so unresolved Negative Feedback still counts.
            // Summary and export keep the 180-day cap.

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
                var response = await _inboxList.ListAsync(
                    new FeedbackInboxListQuery
                    {
                        LocationId = locationId,
                        LocationName =
                            ownedLocation.Location!.LocationName,
                        FromUtc = fromUtc,
                        ToUtc = toUtc,
                        Tab = tab,
                        Q = q,
                        Sentiment = sentiment,
                        DetectedTags = detectedTags,
                        QrSource = qrSource,
                        Contact = contact,
                        WorkflowStatus = workflowStatus,
                        DatePreset = datePreset,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        Sort = sort,
                        Page = page,
                        PageSize = pageSize,
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
                        @new = response.TabCounts.New,
                        inProgress = response.TabCounts.InProgress,
                        resolved = response.TabCounts.Resolved,
                    },
                    digitalGuestLinks = response.DigitalGuestLinks,
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
         LOCATION FEEDBACK EXPORT (OWNED + RANGE)
         =========================================
        */

        [HttpGet("export")]
        public async Task<IActionResult> ExportFeedback(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string scope = "current",
            [FromQuery] string format = "xlsx",
            [FromQuery] bool includeGuestContact = false,
            [FromQuery] int? feedbackId = null,
            [FromQuery] string tab = "all",
            [FromQuery] string? q = null,
            [FromQuery] string[]? sentiment = null,
            [FromQuery] string[]? detectedTags = null,
            [FromQuery] string[]? qrSource = null,
            [FromQuery] string[]? contact = null,
            [FromQuery] string[]? workflowStatus = null,
            [FromQuery] string? datePreset = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string sort = "newest-submitted",
            [FromQuery] int utcOffsetMinutes = 0
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
                    message = "from and to are required."
                });
            }

            var fromUtc = EnsureUtc(from.Value);
            var toUtc = EnsureUtc(to.Value);

            if (fromUtc >= toUtc)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "from must be before to."
                });
            }

            var inclusiveCalendarDays = (toUtc.Date - fromUtc.Date).Days;
            if (inclusiveCalendarDays > MaxInclusiveCalendarDays)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Date range cannot exceed 180 days."
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

            try
            {
                var result = await _inboxList.ExportAsync(
                    new FeedbackExportQuery
                    {
                        LocationId = locationId,
                        LocationName =
                            ownedLocation.Location!.LocationName,
                        FromUtc = fromUtc,
                        ToUtc = toUtc,
                        Scope = scope,
                        Format = format,
                        IncludeGuestContact = includeGuestContact,
                        FeedbackId = feedbackId,
                        Tab = tab,
                        Q = q,
                        Sentiment = sentiment,
                        DetectedTags = detectedTags,
                        QrSource = qrSource,
                        Contact = contact,
                        WorkflowStatus = workflowStatus,
                        DatePreset = datePreset,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        Sort = sort,
                        UtcOffsetMinutes = utcOffsetMinutes,
                    }
                );

                return File(
                    result.Content,
                    result.ContentType,
                    result.FileName
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

        [HttpGet("{feedbackId:int}/export")]
        public async Task<IActionResult> ExportSingleFeedback(
            int feedbackId,
            [FromQuery] int locationId,
            [FromQuery] string format = "xlsx",
            [FromQuery] bool includeGuestContact = false
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
                var result = await _inboxList.ExportAsync(
                    new FeedbackExportQuery
                    {
                        LocationId = locationId,
                        LocationName =
                            ownedLocation.Location!.LocationName,
                        FeedbackId = feedbackId,
                        Format = format,
                        Scope = "current",
                        IncludeGuestContact = includeGuestContact,
                    }
                );

                return File(
                    result.Content,
                    result.ContentType,
                    result.FileName
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

        /*
         =========================================
         GET ONE FEEDBACK (OWNED LOCATION)
         =========================================
        */

        [HttpGet("{feedbackId:int}")]
        public async Task<IActionResult> GetFeedbackDetails(int feedbackId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .Include(f => f.RestaurantLocation)
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var classification =
                FeedbackClassificationMapping.ToApiFields(feedback);

            var internalNotes = await _internalNotes.ListForFeedbackAsync(
                feedback.Id
            );
            var noteActivityFacts =
                await _internalNotes.ListActivityFactsForFeedbackAsync(
                    feedback.Id
                );
            var corrections = await _corrections.ListForFeedbackAsync(
                feedback.Id
            );
            var detectedTagsChanges =
                await _detectedTags.ListForFeedbackAsync(feedback.Id);
            var workflowChanges =
                await _workflowStatusChanges.ListForFeedbackAsync(feedback.Id);
            var closeOuts = await _closeOuts.ListForFeedbackAsync(feedback.Id);
            var guestResponses =
                await _guestResponses.ListForFeedbackAsync(feedback.Id);
            var internalActions =
                await _internalActions.ListForFeedbackAsync(feedback.Id);
            var recoveryCompletions =
                await _recoveryCompletions.ListForFeedbackAsync(feedback.Id);
            var recoveryOffers =
                await _recoveryOffers.ListForFeedbackAsync(feedback.Id);
            var activityHistory = FeedbackActivityHistory.Derive(
                feedback.CreatedAt,
                noteActivityFacts,
                corrections,
                workflowChanges,
                closeOuts,
                guestResponses,
                recoveryCompletions,
                internalActions,
                recoveryOffers,
                detectedTagsChanges
            );

            // Separate load so orphan QrCodeId (legacy fixtures) still returns
            // details with qrSource null — Include on required FK is an inner join.
            QrCode? qrCode = null;
            if (feedback.QrCodeId > 0)
            {
                qrCode = await _context.QrCodes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == feedback.QrCodeId);
            }

            LocationGuestMarketingPreference? loadedPreference = null;
            if (feedback.LocationGuestId is int locationGuestId)
            {
                loadedPreference = await _context.LocationGuests
                    .AsNoTracking()
                    .Where(lg => lg.Id == locationGuestId)
                    .Select(lg => (LocationGuestMarketingPreference?)lg.MarketingPreference)
                    .FirstOrDefaultAsync();
            }

            var marketingPreference =
                loadedPreference ?? LocationGuestMarketingPreference.NotRecorded;

            return Ok(new
            {
                success = true,
                id = feedback.Id,
                guestName = feedback.GuestName,
                guestContact = feedback.GuestContact,
                contactType = feedback.ContactType.ToString(),
                comment = feedback.Comment,
                createdAt = feedback.CreatedAt,
                classifiedAt = feedback.ClassifiedAt,
                locationId = feedback.RestaurantLocationId,
                locationName = feedback.RestaurantLocation!.LocationName,
                address = feedback.RestaurantLocation.Address,
                qrSource = FeedbackQrSourceMapping.ToDisplay(qrCode),
                classificationStatus =
                    classification.ClassificationStatus,
                sentiment = classification.Sentiment,
                detectedTags = classification.DetectedTags,
                locationGuestId = feedback.LocationGuestId,
                marketingPreference = marketingPreference.ToWireString(),
                workflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(
                        feedback.WorkflowStatus
                    ),
                needsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                internalNotes,
                activityHistory,
            });
        }

        /*
         =========================================
         CREATE FEEDBACK INTERNAL NOTE (OWNED)
         =========================================
        */

        [HttpPost("{feedbackId:int}/notes")]
        public async Task<IActionResult> CreateFeedbackInternalNote(
            int feedbackId,
            [FromBody] CreateFeedbackInternalNoteRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var note = await _internalNotes.CreateAsync(
                    feedbackId,
                    userId,
                    request.Body
                );

                if (note == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    note,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         UPDATE FEEDBACK INTERNAL NOTE (OWNED)
         =========================================
        */

        [HttpPut("{feedbackId:int}/notes/{noteId:int}")]
        public async Task<IActionResult> UpdateFeedbackInternalNote(
            int feedbackId,
            int noteId,
            [FromBody] UpdateFeedbackInternalNoteRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var note = await _internalNotes.UpdateAsync(
                    feedbackId,
                    noteId,
                    userId,
                    request.Body
                );

                if (note == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Note not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    note,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         SOFT-DELETE FEEDBACK INTERNAL NOTE (OWNED)
         =========================================
        */

        [HttpDelete("{feedbackId:int}/notes/{noteId:int}")]
        public async Task<IActionResult> SoftDeleteFeedbackInternalNote(
            int feedbackId,
            int noteId
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var deleted = await _internalNotes.SoftDeleteAsync(
                    feedbackId,
                    noteId,
                    userId
                );

                if (deleted == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Note not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    deletedAt = deleted.DeletedAt,
                    deletedByDisplayName = deleted.DeletedByDisplayName,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        /*
         =========================================
         CORRECT CLASSIFICATION SENTIMENT (OWNED)
         =========================================
        */

        [HttpPut("{feedbackId:int}/classification")]
        public async Task<IActionResult> CorrectClassification(
            int feedbackId,
            [FromBody] CorrectFeedbackClassificationDto dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackClassificationMapping.TryParseWireSentiment(
                    dto.Sentiment,
                    out var sentiment
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Sentiment must be positive, neutral, or negative."
                });
            }

            if (!FeedbackClassificationCorrectionMapping.TryParseReason(
                    dto.Reason,
                    out var reason
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Classification correction reason is invalid."
                });
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            if (feedback.ClassificationStatus
                != ClassificationStatus.Succeeded)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Classification can only be corrected when it has succeeded."
                });
            }

            if (feedback.Sentiment is not FeedbackSentiment fromSentiment)
            {
                return Conflict(new
                {
                    success = false,
                    message =
                        "Classification can only be corrected when it has succeeded."
                });
            }

            if (fromSentiment == sentiment)
            {
                var unchanged =
                    FeedbackClassificationMapping.ToApiFields(feedback);

                return Ok(new
                {
                    success = true,
                    id = feedback.Id,
                    classificationStatus =
                        unchanged.ClassificationStatus,
                    sentiment = unchanged.Sentiment,
                    detectedTags = unchanged.DetectedTags,
                    activityEvent = (FeedbackActivityEventDto?)null,
                });
            }

            feedback.Sentiment = sentiment;

            FeedbackClassificationCorrectionItemDto? recorded;
            try
            {
                recorded = await _corrections.RecordAsync(
                    feedback.Id,
                    userId,
                    fromSentiment,
                    sentiment,
                    reason,
                    dto.Note
                );
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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

            if (recorded == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            await _guestTagging.UnionDetectedTagsFromFeedbackAsync(feedback);

            var classification =
                FeedbackClassificationMapping.ToApiFields(feedback);

            return Ok(new
            {
                success = true,
                id = feedback.Id,
                classificationStatus =
                    classification.ClassificationStatus,
                sentiment = classification.Sentiment,
                detectedTags = classification.DetectedTags,
                activityEvent = FeedbackActivityHistory.ToActivityEvent(
                    recorded
                ),
            });
        }

        /*
         =========================================
         DETECTED TAGS (OWNED) — Issue tags edit
         =========================================
        */

        [HttpPut("{feedbackId:int}/detected-tags")]
        public async Task<IActionResult> UpdateDetectedTags(
            int feedbackId,
            [FromBody] UpdateFeedbackDetectedTagsRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!DetectedTagSet.TryNormalize(
                    dto.DetectedTags,
                    out var tags,
                    out var tagError
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = tagError ?? "Detected tags are invalid.",
                });
            }

            var sentimentProvided = dto.Sentiment != null;
            FeedbackSentiment? sentiment = null;
            if (sentimentProvided)
            {
                if (!FeedbackClassificationMapping.TryParseWireSentiment(
                        dto.Sentiment,
                        out var parsedSentiment
                    ))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Sentiment must be positive, neutral, or negative.",
                    });
                }

                sentiment = parsedSentiment;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            UpdateFeedbackDetectedTagsResultDto? result;
            try
            {
                result = await _detectedTags.UpdateAsync(
                    feedbackId,
                    userId,
                    tags,
                    sentiment,
                    sentimentProvided
                );
            }
            catch (FeedbackDetectedTagsConflictException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            return Ok(new
            {
                success = true,
                id = feedbackId,
                classificationStatus = result.ClassificationStatus,
                sentiment = result.Sentiment,
                detectedTags = result.DetectedTags,
                needsAttention = result.NeedsAttention,
                classifiedAt = result.ClassifiedAt,
                activityEvent = result.ActivityEvent,
            });
        }

        /*
         =========================================
         FEEDBACK CLOSE-OUT (OWNED)
         =========================================
        */

        [HttpPost("{feedbackId:int}/close-out")]
        public async Task<IActionResult> CloseOutFeedback(
            int feedbackId,
            [FromBody] CloseOutFeedbackRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackCloseOutMapping.TryParseIntent(
                    dto.Intent,
                    out var intent
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Intent must be mark_resolved or mark_no_action_needed."
                });
            }

            if (!FeedbackCloseOutMapping.TryParseReason(
                    dto.Reason,
                    out var reason
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Close-out reason is invalid."
                });
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _closeOuts.CloseOutAsync(
                    feedbackId,
                    userId,
                    intent,
                    reason,
                    dto.NoteBody
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found."
                    });
                }

                FeedbackActivityEventDto? noteActivityEvent = null;
                if (result.Note != null)
                {
                    noteActivityEvent = new FeedbackActivityEventDto
                    {
                        Kind = "note_added",
                        At = result.Note.CreatedAt,
                        ActorDisplayName = result.Note.AuthorDisplayName,
                    };
                }

                return Ok(new
                {
                    success = true,
                    id = feedbackId,
                    workflowStatus = result.WorkflowStatus,
                    needsAttention = result.NeedsAttention,
                    activityEvent = FeedbackActivityHistory.ToActivityEvent(
                        result.CloseOut
                    ),
                    noteActivityEvent,
                    note = result.Note,
                });
            }
            catch (FeedbackAlreadyResolvedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         SEND GUEST RESPONSE (OWNED) — recovery send
         =========================================
        */

        [HttpPost("{feedbackId:int}/guest-responses")]
        public async Task<IActionResult> SendGuestResponse(
            int feedbackId,
            [FromBody] SendFeedbackGuestResponseRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackGuestResponseMapping.TryParseChannel(
                    dto.Channel,
                    out var channel
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Channel must be email or sms.",
                });
            }

            if (!FeedbackGuestResponseMapping.TryParseIntent(
                    dto.Intent,
                    out var intent
                )
                || intent != FeedbackRecoveryIntent.RespondToGuest)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Intent must be respond_to_guest.",
                });
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader);
                var idempotencyKey = idempotencyHeader.FirstOrDefault();

                var result = await _guestResponses.SendAsync(
                    feedbackId,
                    userId,
                    channel,
                    intent,
                    dto.Subject,
                    dto.Body,
                    dto.Purpose,
                    dto.Tone,
                    dto.IncludeNotes,
                    idempotencyKey
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    id = feedbackId,
                    workflowStatus = result.WorkflowStatus,
                    needsAttention = result.NeedsAttention,
                    activityEvent = FeedbackActivityHistory.ToActivityEvent(
                        result.GuestResponse
                    ),
                    guestResponse = result.GuestResponse,
                });
            }
            catch (FeedbackAlreadyResolvedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (RecoverySmsBillingUnavailableException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        code = "billing_reserve_unavailable",
                        message =
                            "Billing Reserve is not available. Recovery SMS send stays blocked.",
                    }
                );
            }
            catch (RecoverySmsCreditRefusedException ex)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        code = ex.Code,
                        message = ex.Message,
                        channel = CreditChannels.Sms,
                        remaining = ex.Remaining,
                        requested = ex.Requested,
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         GUEST PREVIEW SEND TEST (OWNED) — operator only
         =========================================
        */

        [HttpPost("{feedbackId:int}/guest-preview-send-test")]
        public async Task<IActionResult> SendGuestPreviewTest(
            int feedbackId,
            [FromBody] SendGuestPreviewTestRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _guestPreviewSendTest.SendAsync(
                    feedbackId,
                    userId,
                    dto.Subject,
                    dto.Body,
                    dto.Offer,
                    dto.ToEmail
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                });
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains(
                    "Operator account email is required",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
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
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         PREPARE / REWRITE RECOVERY DRAFT (OWNED)
         =========================================
        */

        [HttpPost("{feedbackId:int}/recovery-draft")]
        public async Task<IActionResult> PrepareRecoveryDraft(
            int feedbackId,
            [FromBody] PrepareFeedbackRecoveryDraftRequest dto,
            CancellationToken cancellationToken
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackGuestResponseMapping.TryParseChannel(
                    dto.Channel,
                    out var channel
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Channel must be email or sms.",
                    retryable = false,
                });
            }

            var mode = dto.Mode?.Trim().ToLowerInvariant();
            if (mode is not (
                "prepare"
                or "rewrite_subject"
                or "rewrite_message"
            ))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Mode must be prepare, rewrite_subject, or rewrite_message.",
                    retryable = false,
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Purpose)
                || string.IsNullOrWhiteSpace(dto.Tone))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Purpose and tone are required.",
                    retryable = false,
                });
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var channelWire =
                FeedbackGuestResponseMapping.ToWireChannel(channel);

            try
            {
                var packKey = BilledAiPackKeys.ForRecovery(mode);
                var billingResult = await _billedAi.ExecuteAsync(
                    new BilledAiActionRequest
                    {
                        RestaurantId = ownedLocation.RestaurantId,
                        LocationId = feedback.RestaurantLocationId,
                        IdempotencyKey = Request.Headers["Idempotency-Key"]
                            .FirstOrDefault(),
                        PackKey = packKey,
                    },
                    async ct =>
                    {
                        var result = await _recoveryDrafts.PrepareAsync(
                            feedbackId,
                            channelWire,
                            dto.Purpose.Trim(),
                            dto.Tone.Trim(),
                            dto.IncludeNotes,
                            mode,
                            dto.CurrentBody,
                            dto.CurrentSubject,
                            dto.ConfirmedInternalActionCategory,
                            dto.ConfirmedInternalActionNote,
                            dto.ConfirmedOffer,
                            ct
                        );

                        if (result == null)
                        {
                            return new BilledAiGenerationResult.NotFound(
                                "Feedback not found."
                            );
                        }

                        if (!result.Success)
                        {
                            return new BilledAiGenerationResult.Failed(
                                result.Message
                                    ?? "We could not prepare a draft.",
                                result.Retryable
                            );
                        }

                        return new BilledAiGenerationResult.Ok(
                            new BilledAiDraftPayload
                            {
                                Body = result.Body ?? string.Empty,
                                Subject = result.Subject,
                                Channel = result.Channel ?? channelWire,
                            }
                        );
                    },
                    cancellationToken
                );

                return billingResult.ToActionResult();
            }
            catch (FeedbackAlreadyResolvedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        /*
         =========================================
         RECORD INTERNAL ACTION (Feedback recovery)
         =========================================
        */

        [HttpPost("{feedbackId:int}/internal-actions")]
        public async Task<IActionResult> RecordInternalAction(
            int feedbackId,
            [FromBody] RecordFeedbackInternalActionRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackInternalActionMapping.TryParseCategory(
                    dto.Category,
                    out var category
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Category is required and must be a known value.",
                });
            }

            if (!FeedbackInternalActionMapping.TryParseIntent(
                    dto.Intent,
                    out var intent
                )
                || intent != FeedbackRecoveryIntent.RecordInternalActionOnly)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Intent must be record_internal_action_only.",
                });
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _internalActions.RecordAsync(
                    feedbackId,
                    userId,
                    category,
                    dto.Note,
                    intent
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    id = feedbackId,
                    workflowStatus = result.WorkflowStatus,
                    needsAttention = result.NeedsAttention,
                    activityEvent = FeedbackActivityHistory.ToActivityEvent(
                        result.InternalAction
                    ),
                    internalAction = result.InternalAction,
                });
            }
            catch (FeedbackAlreadyResolvedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         RESPOND AND RECORD INTERNAL ACTION (OWNED)
         Atomic guest response + internal-action fact
         =========================================
        */

        [HttpPost("{feedbackId:int}/respond-and-record-internal-action")]
        public async Task<IActionResult> RespondAndRecordInternalAction(
            int feedbackId,
            [FromBody] RespondAndRecordInternalActionRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackGuestResponseMapping.TryParseChannel(
                    dto.Channel,
                    out var channel
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Channel must be email or sms.",
                });
            }

            if (!FeedbackInternalActionMapping.TryParseCategory(
                    dto.Category,
                    out var category
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Category is required and must be a known value.",
                });
            }

            if (!FeedbackInternalActionMapping.TryParseIntent(
                    dto.Intent,
                    out var intent
                )
                || intent
                    != FeedbackRecoveryIntent.RespondAndRecordInternalAction)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Intent must be respond_and_record_internal_action.",
                });
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _respondAndRecord.SendAndRecordAsync(
                    feedbackId,
                    userId,
                    channel,
                    category,
                    dto.Note,
                    dto.Subject,
                    dto.Body,
                    dto.Purpose,
                    dto.Tone,
                    dto.IncludeNotes
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    id = feedbackId,
                    workflowStatus = result.WorkflowStatus,
                    needsAttention = result.NeedsAttention,
                    guestResponseActivityEvent =
                        FeedbackActivityHistory.ToActivityEvent(
                            result.GuestResponse
                        ),
                    internalActionActivityEvent =
                        FeedbackActivityHistory.ToActivityEvent(
                            result.InternalAction
                        ),
                    guestResponse = result.GuestResponse,
                    internalAction = result.InternalAction,
                });
            }
            catch (FeedbackAlreadyResolvedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         RECOVERY OFFER ATTACH (OWNED — durable OfferId)
         =========================================
        */

        [HttpGet("{feedbackId:int}/recovery-offer-attach")]
        public async Task<IActionResult> GetRecoveryOfferAttach(int feedbackId)
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var offerId = await _recoveryOfferAttach.GetAsync(feedbackId);

            return Ok(new FeedbackRecoveryOfferAttachResponse
            {
                Success = true,
                OfferId = offerId,
            });
        }

        [HttpPut("{feedbackId:int}/recovery-offer-attach")]
        public async Task<IActionResult> SetRecoveryOfferAttach(
            int feedbackId,
            [FromBody] SetFeedbackRecoveryOfferAttachRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                await _recoveryOfferAttach.SetAsync(feedbackId, dto.OfferId);
                var offerId = await _recoveryOfferAttach.GetAsync(feedbackId);
                return Ok(new FeedbackRecoveryOfferAttachResponse
                {
                    Success = true,
                    OfferId = offerId,
                });
            }
            catch (PlanEntitlementCapException ex)
            {
                return PlanEntitlementHttp.ToConflict(this, ex);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message,
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
         SEND AND ISSUE RECOVERY OFFER (OWNED)
         =========================================
        */

        [HttpPost("{feedbackId:int}/recovery-offers")]
        public async Task<IActionResult> SendAndIssueRecoveryOffer(
            int feedbackId,
            [FromBody] SendAndIssueFeedbackRecoveryOfferRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _recoveryOffers.SendAndIssueAsync(
                    feedbackId,
                    userId,
                    dto
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    id = feedbackId,
                    workflowStatus = result.WorkflowStatus,
                    needsAttention = result.NeedsAttention,
                    activityEvent = FeedbackActivityHistory.ToActivityEvent(
                        result.GuestResponse
                    ),
                    recoveryOfferActivityEvent =
                        FeedbackActivityHistory.ToActivityEvent(
                            result.RecoveryOffer
                        ),
                    guestResponse = result.GuestResponse,
                    recoveryOffer = result.RecoveryOffer,
                });
            }
            catch (FeedbackAlreadyResolvedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (OfferIssueCodeAllocationException ex)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        message = ex.Message,
                    }
                );
            }
            catch (FeedbackRecoveryOfferCodeAllocationException ex)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        success = false,
                        message = ex.Message,
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         COMPLETE RECOVERY (OWNED) — success Mark resolved
         =========================================
        */

        [HttpPost("{feedbackId:int}/recovery-completion")]
        public async Task<IActionResult> CompleteRecovery(
            int feedbackId,
            [FromBody] CompleteFeedbackRecoveryRequest dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackGuestResponseMapping.TryParseIntent(
                    dto.Intent,
                    out var intent
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Intent must be respond_to_guest, record_internal_action_only, or respond_and_record_internal_action.",
                });
            }

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found.",
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                var result = await _recoveryCompletions.CompleteAsync(
                    feedbackId,
                    userId,
                    intent
                );

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Feedback not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    id = feedbackId,
                    workflowStatus = result.WorkflowStatus,
                    needsAttention = result.NeedsAttention,
                    activityEvent = FeedbackActivityHistory.ToActivityEvent(
                        result.Completion
                    ),
                });
            }
            catch (FeedbackAlreadyResolvedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
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
         SET WORKFLOW STATUS (OWNED)
         =========================================
        */

        [HttpPut("{feedbackId:int}/workflow-status")]
        public async Task<IActionResult> SetWorkflowStatus(
            int feedbackId,
            [FromBody] SetFeedbackWorkflowStatusDto dto
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!FeedbackWorkflowStatusMapping.TryParseWire(
                    dto.WorkflowStatus,
                    out var toStatus
                ))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Workflow status must be new, in_progress, or resolved."
                });
            }

            if (toStatus == FeedbackWorkflowStatus.Resolved)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Resolved can only be set via Feedback close-out or recovery completion."
                });
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            var ownedLocation = await GateLocationAsync(
                feedback.RestaurantLocationId
            );
            var denied = ownedLocation.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var fromStatus = feedback.WorkflowStatus;

            if (fromStatus == toStatus)
            {
                return Ok(new
                {
                    success = true,
                    id = feedback.Id,
                    workflowStatus =
                        FeedbackWorkflowStatusMapping.ToWire(
                            feedback.WorkflowStatus
                        ),
                    needsAttention =
                        FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                    activityEvent = (FeedbackActivityEventDto?)null,
                });
            }

            feedback.WorkflowStatus = toStatus;

            FeedbackWorkflowStatusChangeItemDto? recorded;
            try
            {
                recorded = await _workflowStatusChanges.RecordAsync(
                    feedback.Id,
                    userId,
                    fromStatus,
                    toStatus
                );
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message,
                });
            }

            if (recorded == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Feedback not found."
                });
            }

            return Ok(new
            {
                success = true,
                id = feedback.Id,
                workflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(
                        feedback.WorkflowStatus
                    ),
                needsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                activityEvent = FeedbackActivityHistory.ToActivityEvent(
                    recorded
                ),
            });
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
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
                OperatorAreaIds.Feedback,
                minimum,
                locationId
            );
        }
    }
}
