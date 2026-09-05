using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.OwnedLocation;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantConversationService : IAssistantConversationService
    {
        private static readonly AssistantRetrievedEvidence EmptyEvidence =
            AssistantRetrievedEvidence.Empty;

        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IAssistantLiveAnswerProvider _liveAnswer;
        private readonly IAssistantFeedbackRetrieve _feedbackRetrieve;
        private readonly IAssistantOffersRetrieve _offersRetrieve;
        private readonly IAssistantCampaignsRetrieve _campaignsRetrieve;
        private readonly IAssistantCaptureRetrieve _captureRetrieve;
        private readonly IAssistantHomeKpiRetrieve _homeRetrieve;
        private readonly IAssistantGuestsRetrieve _guestsRetrieve;
        private readonly IAssistantProgressPublisher _progress;
        private readonly ICampaignDraftService _campaignDrafts;
        private readonly ICampaignEligibilityService _campaignEligibility;
        private readonly ICampaignMessageDraftService _campaignMessageDrafts;
        private readonly IOffersCatalogService _offersCatalog;
        private readonly IFeedbackRecoveryDraftsService _recoveryDrafts;
        private readonly IAssistantAttentionRetrieve _attentionRetrieve;
        private readonly ICaptureThankYouOfferService _thankYouOffers;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IAssistantAiBilling _aiBilling;
        private readonly TimeProvider _clock;
        private readonly FeedbackClassificationSettings _liveAnswerSettings;
        private readonly IRestaurantContextSnapshotService? _restaurantContextSnapshot;
        private readonly RestaurantContextSnapshotSettings _snapshotSettings;
        private readonly IAssistantAdvisoryReasonProvider? _advisoryReason;
        private readonly ILogger<AssistantConversationService> _logger;
        private string? _pendingAssistantBodyPrefix;

        public AssistantConversationService(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            IAssistantLiveAnswerProvider liveAnswer,
            IAssistantFeedbackRetrieve feedbackRetrieve,
            IAssistantOffersRetrieve offersRetrieve,
            IAssistantCampaignsRetrieve campaignsRetrieve,
            IAssistantCaptureRetrieve captureRetrieve,
            IAssistantHomeKpiRetrieve homeRetrieve,
            IAssistantGuestsRetrieve guestsRetrieve,
            IAssistantProgressPublisher progress,
            ICampaignDraftService campaignDrafts,
            ICampaignEligibilityService campaignEligibility,
            ICampaignMessageDraftService campaignMessageDrafts,
            IOffersCatalogService offersCatalog,
            IFeedbackRecoveryDraftsService recoveryDrafts,
            IAssistantAttentionRetrieve attentionRetrieve,
            ICaptureThankYouOfferService thankYouOffers,
            IRestaurantPermissionHelper permissions,
            IAssistantAiBilling aiBilling,
            TimeProvider? timeProvider = null,
            IOptions<FeedbackClassificationSettings>? liveAnswerSettings = null,
            IRestaurantContextSnapshotService? restaurantContextSnapshot = null,
            IOptions<RestaurantContextSnapshotSettings>? snapshotSettings = null,
            IAssistantAdvisoryReasonProvider? advisoryReason = null,
            ILogger<AssistantConversationService>? logger = null
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _liveAnswer = liveAnswer;
            _feedbackRetrieve = feedbackRetrieve;
            _offersRetrieve = offersRetrieve;
            _campaignsRetrieve = campaignsRetrieve;
            _captureRetrieve = captureRetrieve;
            _homeRetrieve = homeRetrieve;
            _guestsRetrieve = guestsRetrieve;
            _progress = progress;
            _campaignDrafts = campaignDrafts;
            _campaignEligibility = campaignEligibility;
            _campaignMessageDrafts = campaignMessageDrafts;
            _offersCatalog = offersCatalog;
            _recoveryDrafts = recoveryDrafts;
            _attentionRetrieve = attentionRetrieve;
            _thankYouOffers = thankYouOffers;
            _permissions = permissions;
            _aiBilling = aiBilling;
            _clock = timeProvider ?? TimeProvider.System;
            _liveAnswerSettings = liveAnswerSettings?.Value
                ?? new FeedbackClassificationSettings();
            _restaurantContextSnapshot = restaurantContextSnapshot;
            _snapshotSettings = snapshotSettings?.Value
                ?? new RestaurantContextSnapshotSettings();
            _advisoryReason = advisoryReason;
            _logger = logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AssistantConversationService>.Instance;
        }

        public async Task<AssistantTurnOutcome> SendTurnAsync(
            int ownerUserId,
            SendAssistantTurnRequest request,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default
        )
        {
            var message = request.Message?.Trim() ?? string.Empty;
            if (message.Length == 0)
            {
                return new AssistantTurnOutcome.Invalid("Message is required.");
            }

            var authorized = await TryAuthorizeScopeAsync(
                ownerUserId,
                request.AnalysisScope,
                cancellationToken
            );
            if (authorized.Error is not null)
            {
                return authorized.Error;
            }

            var cached = await TryReplayIdempotentTurnAsync(
                request.AnalysisScope,
                idempotencyKey,
                cancellationToken
            );
            if (cached is not null)
            {
                return cached;
            }

            var locationName = authorized.LocationName;
            AssistantConversation conversation;

            if (request.ConversationId is int conversationId)
            {
                var existing = await LoadOwnedConversationAsync(
                    ownerUserId,
                    conversationId,
                    cancellationToken
                );

                if (existing is null)
                {
                    return new AssistantTurnOutcome.NotFound();
                }

                conversation = existing;
            }
            else
            {
                conversation = new AssistantConversation
                {
                    OwnerUserId = ownerUserId,
                    Title = AssistantAnalysisScope.TitleFromFirstUserMessage(message),
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsArchived = false,
                };
                AssistantAnalysisScope.CopyToConversation(
                    conversation,
                    request.AnalysisScope,
                    locationName
                );
                _context.AssistantConversations.Add(conversation);
            }

            var now = DateTime.UtcNow;
            var userMessage = new AssistantMessage
            {
                Role = AssistantMessageRole.User,
                Body = message,
                CreatedAt = now,
            };
            AssistantAnalysisScope.CopyToUserMessage(
                userMessage,
                request.AnalysisScope,
                locationName
            );
            conversation.Messages.Add(userMessage);
            conversation.LastActivityAt = now;
            await _context.SaveChangesAsync(cancellationToken);

            return await CompleteTurnAsync(
                conversation,
                message,
                request.AnalysisScope,
                locationName,
                replaceFailure: null,
                idempotencyKey,
                cancellationToken
            );
        }

        public async Task<AssistantTurnOutcome> RetryTurnAsync(
            int ownerUserId,
            int conversationId,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default
        )
        {
            var conversation = await LoadOwnedConversationAsync(
                ownerUserId,
                conversationId,
                cancellationToken
            );

            if (conversation is null)
            {
                return new AssistantTurnOutcome.NotFound();
            }

            var lastUser = conversation.Messages
                .Where(message => message.Role == AssistantMessageRole.User)
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .LastOrDefault();
            var lastAssistant = conversation.Messages
                .Where(message => message.Role == AssistantMessageRole.Assistant)
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .LastOrDefault();

            if (lastUser is null
                || lastAssistant is null
                || lastAssistant.Class != AssistantMessageClass.Failure)
            {
                return new AssistantTurnOutcome.Invalid("No failure turn to retry.");
            }

            var sendScope = AssistantAnalysisScope.FromUserMessage(lastUser);
            if (sendScope is null)
            {
                return new AssistantTurnOutcome.Invalid("Send Analysis scope is missing.");
            }

            var authorized = await TryAuthorizeScopeAsync(
                ownerUserId,
                sendScope,
                cancellationToken
            );
            if (authorized.Error is not null)
            {
                return authorized.Error;
            }

            var cached = await TryReplayIdempotentTurnAsync(
                sendScope,
                idempotencyKey,
                cancellationToken
            );
            if (cached is not null)
            {
                return cached;
            }

            return await CompleteTurnAsync(
                conversation,
                lastUser.Body,
                sendScope,
                authorized.LocationName,
                lastAssistant,
                idempotencyKey,
                cancellationToken
            );
        }

        public async Task<AssistantTurnOutcome> GetAsync(
            int ownerUserId,
            int conversationId,
            CancellationToken cancellationToken = default
        )
        {
            var conversation = await LoadOwnedConversationAsync(
                ownerUserId,
                conversationId,
                cancellationToken
            );

            if (conversation is null)
            {
                return new AssistantTurnOutcome.NotFound();
            }

            if (!AssistantAnalysisScope.IsAll(conversation))
            {
                if (conversation.OwnedLocationId is not int locationId)
                {
                    return new AssistantTurnOutcome.LocationDenied(
                        new OwnedLocationResult
                        {
                            Status = OwnedLocationResolveStatus.NotFound
                        }
                    );
                }

                var locationResult = await _ownedLocation.ResolveAsync(
                    ownerUserId,
                    locationId
                );

                if (locationResult.Status != OwnedLocationResolveStatus.Found)
                {
                    return new AssistantTurnOutcome.LocationDenied(locationResult);
                }
            }

            return new AssistantTurnOutcome.Ok(
                AssistantAnalysisScope.ToConversationDto(conversation)
            );
        }

        public async Task<AssistantTurnOutcome> ApplyScopeAsync(
            int ownerUserId,
            int conversationId,
            ApplyAssistantScopeRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var conversation = await LoadOwnedConversationAsync(
                ownerUserId,
                conversationId,
                cancellationToken
            );

            if (conversation is null)
            {
                return new AssistantTurnOutcome.NotFound();
            }

            var authorized = await TryAuthorizeScopeAsync(
                ownerUserId,
                request.AnalysisScope,
                cancellationToken
            );
            if (authorized.Error is not null)
            {
                return authorized.Error;
            }

            var lastActivity = conversation.LastActivityAt;
            AssistantAnalysisScope.CopyToConversation(
                conversation,
                request.AnalysisScope,
                authorized.LocationName
            );
            conversation.LastActivityAt = lastActivity;
            await _context.SaveChangesAsync(cancellationToken);

            return new AssistantTurnOutcome.Ok(
                AssistantAnalysisScope.ToConversationDto(conversation)
            );
        }

        public async Task<AssistantListOutcome> ListAsync(
            int ownerUserId,
            bool archived,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.AssistantConversations
                .AsNoTracking()
                .Where(conversation =>
                    conversation.OwnerUserId == ownerUserId
                    && conversation.IsArchived == archived
                )
                .OrderByDescending(conversation => conversation.LastActivityAt)
                .ThenByDescending(conversation => conversation.Id)
                .Select(conversation => new AssistantConversationListItemDto
                {
                    Id = conversation.Id,
                    Title = conversation.Title,
                    OwnedLocationName = conversation.OwnedLocationName,
                    LastActivityAt = conversation.LastActivityAt,
                    IsArchived = conversation.IsArchived,
                })
                .ToListAsync(cancellationToken);

            return new AssistantListOutcome.Ok(rows);
        }

        public async Task<AssistantTurnOutcome> SetArchivedAsync(
            int ownerUserId,
            int conversationId,
            bool archived,
            CancellationToken cancellationToken = default
        )
        {
            var conversation = await LoadOwnedConversationAsync(
                ownerUserId,
                conversationId,
                cancellationToken
            );

            if (conversation is null)
            {
                return new AssistantTurnOutcome.NotFound();
            }

            var lastActivity = conversation.LastActivityAt;
            conversation.IsArchived = archived;
            conversation.LastActivityAt = lastActivity;
            await _context.SaveChangesAsync(cancellationToken);

            return new AssistantTurnOutcome.Ok(
                AssistantAnalysisScope.ToConversationDto(conversation)
            );
        }

        public async Task<AssistantTurnOutcome> ClearDraftInterviewAsync(
            int ownerUserId,
            int conversationId,
            CancellationToken cancellationToken = default
        )
        {
            var conversation = await LoadOwnedConversationAsync(
                ownerUserId,
                conversationId,
                cancellationToken
            );
            if (conversation is null)
            {
                return new AssistantTurnOutcome.NotFound();
            }

            conversation.DraftInterviewJson = null;
            await _context.SaveChangesAsync(cancellationToken);
            return new AssistantTurnOutcome.Ok(
                AssistantAnalysisScope.ToConversationDto(conversation)
            );
        }

        public async Task<AssistantDeleteOutcome> DeleteAsync(
            int ownerUserId,
            int conversationId,
            CancellationToken cancellationToken = default
        )
        {
            var conversation = await LoadOwnedConversationAsync(
                ownerUserId,
                conversationId,
                cancellationToken
            );

            if (conversation is null)
            {
                return new AssistantDeleteOutcome.NotFound();
            }

            _context.AssistantConversations.Remove(conversation);
            await _context.SaveChangesAsync(cancellationToken);
            return new AssistantDeleteOutcome.Ok();
        }

        public async Task DeleteAllForOwnerAsync(
            int ownerUserId,
            CancellationToken cancellationToken = default
        )
        {
            var conversations = await _context.AssistantConversations
                .Where(row => row.OwnerUserId == ownerUserId)
                .ToListAsync(cancellationToken);

            if (conversations.Count == 0)
            {
                return;
            }

            var conversationIds = conversations.Select(row => row.Id).ToList();
            var messages = await _context.AssistantMessages
                .Where(row => conversationIds.Contains(row.ConversationId))
                .ToListAsync(cancellationToken);

            _context.AssistantMessages.RemoveRange(messages);
            _context.AssistantConversations.RemoveRange(conversations);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Prior chat messages sent to the live answer model: everything
        /// strictly before the last user message. The last user message is the
        /// current ask; anything after it (for example a failure reply being
        /// retried) is replaced this turn and never shipped as history.
        /// </summary>
        private IReadOnlyList<AssistantLiveAnswerHistoryTurn> BuildLiveAnswerHistory(
            AssistantConversation conversation
        )
        {
            var ordered = conversation.Messages
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .ToList();

            var lastUserIndex = -1;
            for (var i = ordered.Count - 1; i >= 0; i--)
            {
                if (ordered[i].Role == AssistantMessageRole.User)
                {
                    lastUserIndex = i;
                    break;
                }
            }

            var cap = Math.Max(0, _liveAnswerSettings.AssistantHistoryMessageCap);
            var historyEnd = Math.Max(0, lastUserIndex);
            return ordered
                .Take(historyEnd)
                .TakeLast(cap)
                .Select(message => new AssistantLiveAnswerHistoryTurn(
                    message.Role,
                    message.Body
                ))
                .ToList();
        }

        private async Task<AssistantTurnOutcome> CompleteTurnAsync(
            AssistantConversation conversation,
            string userMessage,
            AssistantAnalysisScopeDto scope,
            string locationName,
            AssistantMessage? replaceFailure,
            string? idempotencyKey,
            CancellationToken cancellationToken
        )
        {
            _pendingAssistantBodyPrefix = null;
            var ownedLocations = await LoadOwnedLocationsAsync(
                conversation.OwnedLocationId,
                conversation.OwnerUserId,
                cancellationToken
            );
            var isSingleMode = ownedLocations.Count < 2
                || ownedLocations.Any(location =>
                    string.Equals(
                        location.AccountType,
                        "Single",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            var locationRefs = ownedLocations
                .Select(location => new AssistantOwnedLocationRef(
                    location.Id,
                    location.Name,
                    location.Address,
                    location.CaptureStatus
                ))
                .ToList();
            if (conversation.DraftInterviewJson is not null
                && AssistantGapTurn.Parse(conversation.DraftInterviewJson) is null
                && AssistantDraftTargetChoice.Parse(conversation.DraftInterviewJson)
                    is null)
            {
                // Legacy draft-interview JSON from retired interview drivers.
                conversation.DraftInterviewJson = null;
            }

            var draftTargetChoiceState = AssistantDraftTargetChoice.Parse(
                conversation.DraftInterviewJson
            );
            var gapState = AssistantGapTurn.Parse(conversation.DraftInterviewJson);
            if (gapState is null && draftTargetChoiceState is not null)
            {
                gapState = AssistantGapTurn.CreateTarget(
                    draftTargetChoiceState.Options,
                    userMessage,
                    AssistantTask.CreateCampaignDraft
                );
            }
            if ((gapState is not null || draftTargetChoiceState is not null)
                && AssistantFlowControl.IsClearCancel(userMessage))
            {
                conversation.DraftInterviewJson = null;
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    GroundedMessage(
                        DateTime.UtcNow,
                        AssistantFlowControl.CancelConfirmTitle,
                        AssistantFlowControl.CancelConfirmBody,
                        []
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            // Unmatched routing: cancel already handled. A new create drops
            // the open Gap. Retrieve, Refuse, and confused fills keep it.
            // Advisory Gaps route through TryResumeGapAsync (same entry as
            // Creation); a resolving choice continues the advisory path.
            if (gapState is not null
                && AssistantGapTurn.IsAdvisoryGap(gapState))
            {
                var resumed = await TryResumeGapAsync(
                    conversation,
                    gapState,
                    userMessage,
                    locationName,
                    ownedLocations,
                    replaceFailure,
                    cancellationToken
                );
                if (resumed.Outcome is not null)
                {
                    return resumed.Outcome;
                }

                gapState = AssistantGapTurn.Parse(conversation.DraftInterviewJson);
                if (resumed.MergedUserMessage is not null)
                {
                    userMessage = resumed.MergedUserMessage;
                }
            }
            else if (gapState is not null
                && AssistantGapAsk.LooksLikeNewCreateDuringGap(userMessage))
            {
                conversation.DraftInterviewJson = null;
                gapState = null;
                _pendingAssistantBodyPrefix = AssistantGapAsk.PreviousDraftDropped;
            }

            var productTopics = AssistantProductExpertTopics.Detect(userMessage);
            var helpCentreAsk = AssistantAskIntent.IsHelpCentreAsk(userMessage);
            if (!helpCentreAsk
                && AssistantExplainWhyFollowUp.IsExplainWhyFollowUp(userMessage))
            {
                var explainTurn = await TryFinishExplainWhyFollowUpAsync(
                    conversation,
                    userMessage,
                    scope,
                    locationName,
                    locationRefs,
                    replaceFailure,
                    cancellationToken
                );
                if (explainTurn is not null)
                {
                    return explainTurn;
                }
            }

            var productExpertTurn = productTopics.Count > 0 && !helpCentreAsk;
            var mixedProductRetrieve = productExpertTurn
                && AssistantProductExpertTopics.IsMixedRetrieve(userMessage);
            var pureProductExpert = productExpertTurn && !mixedProductRetrieve;
            var compareOutcome = AssistantCompareTurn.Resolve(
                productExpertTurn
                    ? AssistantProductExpertTopics.StripMatchedNeedles(userMessage)
                    : userMessage,
                conversation.OwnedLocationId,
                locationRefs,
                AssistantCompareTurn.ParseLocationIds(
                    conversation.LastCompareLocationIdsJson
                ),
                isSingleMode,
                AssistantAnalysisScope.IsAll(conversation)
            );
            if (gapState is not null
                && compareOutcome is AssistantCompareOutcome.Clarify
                && !AssistantTaskClassification.LooksLikeCreateTurn(userMessage))
            {
                conversation.DraftInterviewJson = null;
                gapState = null;
            }
            await TryPublishProgressAsync(
                conversation.OwnerUserId,
                conversation.Id,
                AssistantTurnProgressSteps.Checking,
                cancellationToken
            );

            var isCreateTurn = gapState is not null
                || AssistantTaskClassification.LooksLikeCreateTurn(userMessage);
            if (compareOutcome is AssistantCompareOutcome.Clarify clarify
                && !isCreateTurn
                && !helpCentreAsk
                && !pureProductExpert)
            {
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    ClarifyMessage(DateTime.UtcNow, clarify.Body),
                    replaceFailure,
                    cancellationToken
                );
            }

            IReadOnlyList<string> draftTargets =
                AssistantCreateTargets.Detect(userMessage);
            int? boundCreateLocationId = null;
            string? boundCreateLocationName = null;
            AssistantCampaignDraftBindOutcome? preparedCampaignBind = null;
            var ownedLocationIds = ownedLocations
                .Select(location => location.Id)
                .ToList();
            if (gapState is not null
                && !AssistantGapAsk.LooksLikeKeepGapAnswer(userMessage))
            {
                var resumed = await TryResumeGapAsync(
                    conversation,
                    gapState,
                    userMessage,
                    locationName,
                    ownedLocations,
                    replaceFailure,
                    cancellationToken
                );
                if (resumed.Outcome is not null)
                {
                    return resumed.Outcome;
                }

                if (resumed.DraftTargets is not null)
                {
                    draftTargets = resumed.DraftTargets;
                }

                if (resumed.LocationId is int resumedLocationId)
                {
                    boundCreateLocationId = resumedLocationId;
                    boundCreateLocationName = resumed.LocationName;
                }
            }
            else if (draftTargets.Count > 1)
            {
                return await FinishGapTurnAsync(
                    conversation,
                    AssistantGapTurn.CreateTarget(
                        draftTargets,
                        userMessage,
                        AssistantTaskClassification.ForCreateTargetGap(
                            draftTargets,
                            userMessage
                        )
                    ),
                    AssistantGapTurn.CreateTargetBody(draftTargets),
                    replaceFailure,
                    cancellationToken
                );
            }
            else if (
                (
                    AssistantTaskClassification.LooksLikeCreateCampaignDraft(userMessage)
                    || AssistantTaskClassification.LooksLikeOfferPath(userMessage)
                    || AssistantTaskClassification.LooksLikeCreateCampaignWithOffer(userMessage)
                )
                && !AssistantAskIntent.IsHelpCentreAsk(userMessage)
            )
            {
                var createTask = AssistantTaskClassification.LooksLikeCreateCampaignWithOffer(
                    userMessage
                )
                    ? AssistantTask.CreateCampaignWithOffer
                    : AssistantTaskClassification.LooksLikeCreateCampaignDraft(
                        userMessage
                    )
                        ? AssistantTask.CreateCampaignDraft
                        : AssistantTask.OfferPath;
                var offerTerms = createTask == AssistantTask.OfferPath
                    ? AssistantOfferPathTerms.Parse(userMessage)
                    : null;
                var locationOutcome = ResolveCreateLocation(
                    userMessage,
                    conversation,
                    locationName,
                    ownedLocations,
                    uniqueNameIsChoice: false,
                    createTask
                );
                var locationTurn = await TryFinishLocationOutcomeAsync(
                    conversation,
                    userMessage,
                    locationName,
                    locationOutcome,
                    replaceFailure,
                    cancellationToken,
                    createTask,
                    offerTerms
                );
                if (locationTurn.Outcome is not null)
                {
                    return locationTurn.Outcome;
                }

                boundCreateLocationId = locationTurn.LocationId;
                boundCreateLocationName = locationTurn.LocationName;
                if (createTask is AssistantTask.CreateCampaignDraft
                    or AssistantTask.CreateCampaignWithOffer)
                {
                    preparedCampaignBind = await BindCampaignAsync(
                        userMessage,
                        CreatePersistLocationId(boundCreateLocationId, conversation),
                        boundCreateLocationName ?? locationName,
                        ownedLocationIds,
                        cancellationToken,
                        ignoreOffers: createTask == AssistantTask.CreateCampaignWithOffer
                    );
                    var bindAbort = await TryFinishBindOutcomeAsync(
                        conversation,
                        userMessage,
                        preparedCampaignBind,
                        replaceFailure,
                        cancellationToken,
                        createTask
                    );
                    if (bindAbort is not null)
                    {
                        return bindAbort;
                    }

                    if (createTask == AssistantTask.CreateCampaignWithOffer)
                    {
                        var combinedGap = await TryFinishCombinedCreatePrePersistGapsAsync(
                            conversation,
                            userMessage,
                            CreatePersistLocationId(boundCreateLocationId, conversation),
                            ownedLocationIds,
                            replaceFailure,
                            cancellationToken
                        );
                        if (combinedGap is not null)
                        {
                            return combinedGap;
                        }
                    }
                }
            }
            var hasExplicitRetrieveAsk =
                AssistantAskIntent.HasExplicitRetrieveAsk(userMessage);
            var hasRetrieveAsk =
                AssistantAskIntent.HasRetrieveAsk(userMessage)
                && (
                    hasExplicitRetrieveAsk
                    || draftTargets.Count == 0
                );

            if (gapState is null
                && !AssistantTaskClassification.LooksLikeCreateCampaignDraft(userMessage)
                && !AssistantTaskClassification.LooksLikeOfferPath(userMessage)
                && !AssistantTaskClassification.LooksLikeRecoveryPath(userMessage)
                && !productExpertTurn)
            {
                var sendScheduleTurn = await TryFinishSendScheduleRouteAsync(
                    conversation,
                    userMessage,
                    replaceFailure,
                    cancellationToken
                );
                if (sendScheduleTurn is not null)
                {
                    return sendScheduleTurn;
                }
            }

            var attentionSurface = AssistantAttentionAsk.Detect(userMessage);
            var attentionAsk = attentionSurface != AssistantAttentionSurface.None
                && gapState is null
                && !AssistantAskIntent.IsHelpCentreAsk(userMessage)
                && !AssistantTaskClassification.LooksLikeCreateCampaignDraft(userMessage)
                && !AssistantTaskClassification.LooksLikeOfferPath(userMessage)
                && !AssistantTaskClassification.LooksLikeRecoveryPath(userMessage);
            if (attentionAsk && AssistantAnalysisScope.IsAll(conversation))
            {
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    GroundedMessage(
                        DateTime.UtcNow,
                        AssistantAttentionCopy.AllOwnedLocationsPickOneTitle,
                        AssistantAttentionCopy.AllOwnedLocationsPickOneBody,
                        []
                    ),
                    replaceFailure,
                    cancellationToken,
                    liveAnswerAlreadyCompleted: true
                );
            }

            if (attentionAsk
                && compareOutcome is AssistantCompareOutcome.NotCompare
                && conversation.OwnedLocationId is int attentionLocationId)
            {
                return await FinishAttentionRetrieveAsync(
                    conversation,
                    attentionSurface,
                    attentionLocationId,
                    locationName,
                    scope.ReportingPeriod,
                    replaceFailure,
                    cancellationToken
                );
            }

            var periodPhrase = AssistantAnalysisScope.PeriodPhrase(scope.ReportingPeriod);
            var window = AssistantReportingPeriodWindow.Resolve(
                scope.ReportingPeriod,
                DateTime.UtcNow
            );
            var recoveryIdentityNeeded =
                AssistantAnalysisScope.IsAll(conversation)
                && (
                    AssistantTaskClassification.LooksLikeRecoveryPath(userMessage)
                    || gapState?.Kind == AssistantGapTurn.KindFeedback
                );
            AssistantFeedbackEvidence? recoveryIdentity = null;
            if (recoveryIdentityNeeded)
            {
                recoveryIdentity = await RetrieveRecoveryIdentityUnionAsync(
                    conversation.OwnerUserId,
                    ownedLocations,
                    window.FromUtc,
                    window.ToUtc,
                    cancellationToken
                );
                if (recoveryIdentity is null)
                {
                    conversation.LastCompareLocationIdsJson = null;
                    return await PersistAssistantAsync(
                        conversation,
                        FailureMessage(DateTime.UtcNow),
                        replaceFailure,
                        cancellationToken
                    );
                }
            }
            var skipCompareAll =
                pureProductExpert
                || helpCentreAsk
                || attentionSurface != AssistantAttentionSurface.None
                || isCreateTurn
                || AssistantTaskClassification.LooksLikeRecoveryPath(userMessage)
                || recoveryIdentityNeeded
                || AssistantAskIntent.IsFullRefusal(AssistantAskIntent.Classify(userMessage));
            var namedCompare = compareOutcome as AssistantCompareOutcome.Compare;
            var isCompareAll = !skipCompareAll
                && AssistantAnalysisScope.IsAll(conversation)
                && (
                    namedCompare is { IsCompareAll: true }
                    || (namedCompare is null
                        && compareOutcome is AssistantCompareOutcome.NotCompare)
                );
            var compareIds = isCompareAll
                ? AssistantCompareTurn.AllOwnedIdsByName(locationRefs)
                : namedCompare is not null
                    ? namedCompare.LocationIds
                    : conversation.OwnedLocationId is int savedLocationId
                        ? (IReadOnlyList<int>)[savedLocationId]
                        : [];
            var droppedUnknown = namedCompare?.DroppedUnknownSentence;
            var caveat = compareOutcome switch
            {
                AssistantCompareOutcome.SingleCaveat =>
                    AssistantCompareTurn.SingleCaveatSentence(locationName),
                AssistantCompareOutcome.MentionCaveat mention =>
                    AssistantCompareTurn.MentionCaveatSentence(
                        locationName,
                        mention.MentionedLocationName
                    ),
                AssistantCompareOutcome.TwoPeriodCaveat =>
                    AssistantCompareTurn.TwoPeriodCaveatSentence(periodPhrase),
                _ => null,
            };

            IReadOnlyList<AssistantCompareLocationEvidence>? compareEvidence = null;
            IReadOnlyList<string> failedLocationNames = [];
            IReadOnlyList<string> notStartedLocationNames = [];
            AssistantRetrievedEvidence savedEvidence;

            // Advisory Gap pre-check before expensive retrieve / live LLM.
            // Clear with an injected Reason provider finishes on that path;
            // without it, Clear falls through to retrieve + live answer.
            if (_restaurantContextSnapshot is not null
                && gapState is null
                && !isCreateTurn
                && !helpCentreAsk
                && !pureProductExpert
                && !attentionAsk
                && AssistantAdvisoryIntent.LooksLikeAdvisoryRetrieve(userMessage))
            {
                var advisoryTurn = await TryFinishAdvisoryPreCheckAsync(
                    conversation,
                    userMessage,
                    ownedLocations,
                    compareIds,
                    replaceFailure,
                    boundCreateLocationId,
                    idempotencyKey,
                    cancellationToken
                );
                if (advisoryTurn is not null)
                {
                    return advisoryTurn;
                }
            }

            try
            {
                if (pureProductExpert)
                {
                    savedEvidence = EmptyEvidence;
                }
                else if (recoveryIdentity is not null)
                {
                    savedEvidence = AssistantRetrievedEvidence.FromFeedback(recoveryIdentity);
                }
                else if (isCompareAll)
                {
                    if (hasRetrieveAsk)
                    {
                        await TryPublishProgressAsync(
                            conversation.OwnerUserId,
                            conversation.Id,
                            AssistantTurnProgressSteps.Retrieving,
                            cancellationToken
                        );
                    }

                    var compareAll = await RetrieveCompareAllAsync(
                        conversation.OwnerUserId,
                        compareIds,
                        locationRefs,
                        window.FromUtc,
                        window.ToUtc,
                        AssistantAskIntent.NeedsCampaignCopy(userMessage),
                        cancellationToken
                    );
                    if (compareAll.Landed.Count == 0)
                    {
                        conversation.LastCompareLocationIdsJson = null;
                        return await PersistAssistantAsync(
                            conversation,
                            FailureMessage(DateTime.UtcNow),
                            replaceFailure,
                            cancellationToken
                        );
                    }

                    compareEvidence = compareAll.Landed;
                    failedLocationNames = compareAll.FailedNames;
                    notStartedLocationNames = compareAll.NotStartedNames;
                    savedEvidence = EmptyEvidence;
                }
                else
                {
                    if (hasRetrieveAsk)
                    {
                        await TryPublishProgressAsync(
                            conversation.OwnerUserId,
                            conversation.Id,
                            AssistantTurnProgressSteps.Retrieving,
                            cancellationToken
                        );
                    }
                    var retrieved = await RetrieveForTurnAsync(
                        conversation.OwnerUserId,
                        compareIds,
                        conversation.OwnedLocationId,
                        locationRefs,
                        window.FromUtc,
                        window.ToUtc,
                        AssistantAskIntent.NeedsCampaignCopy(userMessage),
                        cancellationToken
                    );
                    if (retrieved is null)
                    {
                        conversation.LastCompareLocationIdsJson = null;
                        return await PersistAssistantAsync(
                            conversation,
                            FailureMessage(DateTime.UtcNow),
                            replaceFailure,
                            cancellationToken
                        );
                    }

                    compareEvidence = namedCompare is not null
                        ? retrieved.CompareRows
                        : null;
                    savedEvidence = retrieved.SavedEvidence;
                }
            }
            catch (OperationCanceledException)
            {
                conversation.LastCompareLocationIdsJson = null;
                await PersistAssistantAsync(
                    conversation,
                    FailureMessage(DateTime.UtcNow),
                    replaceFailure,
                    CancellationToken.None
                );
                throw;
            }

            var askKind = AssistantAskIntent.Classify(userMessage);

            if (gapState is not null
                && gapState.Kind == AssistantGapTurn.KindFeedback
                && !AssistantGapAsk.LooksLikeKeepGapAnswer(userMessage)
                && AssistantGapTurn.Parse(conversation.DraftInterviewJson)
                    is { Kind: AssistantGapTurn.KindFeedback } openFeedbackGap)
            {
                return await ResumeFeedbackGapAsync(
                    conversation,
                    openFeedbackGap,
                    userMessage,
                    recoveryIdentity ?? savedEvidence.Feedback,
                    savedEvidence.Offers,
                    replaceFailure,
                    cancellationToken
                );
            }

            AssistantTurnBilling? turnBilling;
            var billingGate = await TryBeginBilledLiveAnswerAsync(
                conversation,
                boundCreateLocationId,
                idempotencyKey,
                cancellationToken
            );
            if (billingGate.Error is not null)
            {
                return billingGate.Error;
            }

            turnBilling = billingGate.Billing;

            AssistantLiveAnswerResult answer;
            try
            {
                await TryPublishProgressAsync(
                    conversation.OwnerUserId,
                    conversation.Id,
                    AssistantTurnProgressSteps.Preparing,
                    cancellationToken
                );
                answer = await _liveAnswer.CompleteAsync(
                    new AssistantLiveAnswerInput(
                        userMessage,
                        locationName,
                        periodPhrase,
                        savedEvidence,
                        compareEvidence,
                        caveat,
                        droppedUnknown,
                        SuppressMixedRefusal: false,
                        CompareAll: isCompareAll,
                        FailedLocationNames: failedLocationNames,
                        NotStartedLocationNames: notStartedLocationNames,
                        History: BuildLiveAnswerHistory(conversation)
                    ),
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                conversation.LastCompareLocationIdsJson = null;
                await PersistAssistantAsync(
                    conversation,
                    FailureMessage(DateTime.UtcNow),
                    replaceFailure,
                    CancellationToken.None
                );
                throw;
            }

            var assistantNow = DateTime.UtcNow;
            AssistantMessage assistantMessage;
            string? proposedConversationTitle = null;
            if (answer is AssistantLiveAnswerResult.Succeeded succeeded)
            {
                turnBilling?.MarkLiveAnswerSucceeded();
                proposedConversationTitle = succeeded.ConversationTitle;
                AssistantConversationTitle.TryApply(
                    conversation,
                    new AssistantMessage
                    {
                        Class = succeeded.Class,
                        Title = succeeded.Title,
                    },
                    proposedConversationTitle
                );
                if (string.Equals(
                        succeeded.AssistantTask,
                        AssistantTask.CreateCampaignWithOffer,
                        StringComparison.Ordinal
                    )
                    && !productExpertTurn
                    && !AssistantAskIntent.IsHelpCentreAsk(userMessage))
                {
                    var persistLocationId = CreatePersistLocationId(boundCreateLocationId, conversation);
                    var persistLocationName = boundCreateLocationName ?? locationName;
                    // Model-extracted terms win when a field is set; stored
                    // resume terms and parse of the send fill the rest so an
                    // incomplete model extract cannot wipe named facts.
                    var combinedResume = TryGetStoredCreateResumeContext(
                        conversation,
                        userMessage
                    );
                    var combinedTerms = AssistantOfferPathTerms.Overlay(
                        succeeded.OfferTerms,
                        AssistantOfferPathTerms.Overlay(
                            combinedResume?.PriorTerms,
                            AssistantOfferPathTerms.Parse(userMessage)
                        )
                    );
                    var combinedTermsGap = await TryFinishOfferTermsGapAsync(
                        conversation,
                        combinedResume?.SourceUserMessage ?? userMessage,
                        combinedTerms,
                        replaceFailure,
                        cancellationToken,
                        AssistantTask.CreateCampaignWithOffer
                    );
                    if (combinedTermsGap is not null)
                    {
                        conversation.LastCompareLocationIdsJson = null;
                        return combinedTermsGap;
                    }

                    var preparedCombinedBind = combinedResume is null
                        ? preparedCampaignBind
                        : await BindCampaignAsync(
                            combinedResume.SourceUserMessage,
                            persistLocationId,
                            persistLocationName,
                            ownedLocationIds,
                            cancellationToken,
                            ignoreOffers: true
                        );
                    var persist = await PersistCreateCampaignWithOfferAsync(
                        conversation,
                        userMessage,
                        persistLocationId,
                        persistLocationName,
                        ownedLocationIds,
                        cancellationToken,
                        preparedBind: preparedCombinedBind,
                        priorTerms: combinedTerms,
                        questionBody: string.Empty
                    );
                    conversation.DraftInterviewJson = persist.GapState is null
                        ? null
                        : AssistantGapTurn.Serialize(persist.GapState);
                    conversation.LastCompareLocationIdsJson = null;
                    if (persist.CreatedCampaignId is int createdCombinedCampaignId)
                    {
                        conversation.CreatedCampaignId = createdCombinedCampaignId;
                    }
                    if (persist.CreatedOfferId is int createdCombinedOfferId)
                    {
                        conversation.CreatedOfferId = createdCombinedOfferId;
                    }
                    assistantMessage = persist.Class == AssistantMessageClass.Gap
                        ? GapMessage(assistantNow, persist.Body)
                        : GroundedMessage(
                            assistantNow,
                            persist.Title,
                            persist.Body,
                            persist.Actions
                        );
                }
                else if (string.Equals(
                        succeeded.AssistantTask,
                        AssistantTask.CreateCampaignDraft,
                        StringComparison.Ordinal
                    )
                    && !productExpertTurn
                    && !AssistantAskIntent.IsHelpCentreAsk(userMessage))
                {
                    var persistLocationId = CreatePersistLocationId(boundCreateLocationId, conversation);
                    var persistLocationName = boundCreateLocationName ?? locationName;
                    var persist = await PersistCreateCampaignDraftAsync(
                        conversation,
                        userMessage,
                        persistLocationId,
                        persistLocationName,
                        ownedLocationIds,
                        cancellationToken,
                        preparedBind: preparedCampaignBind
                    );
                    conversation.DraftInterviewJson = persist.GapState is null
                        ? null
                        : AssistantGapTurn.Serialize(persist.GapState);
                    conversation.LastCompareLocationIdsJson = null;
                    if (persist.CreatedCampaignId is int createdCampaignId)
                    {
                        conversation.CreatedCampaignId = createdCampaignId;
                    }
                    assistantMessage = PersistTurnMessage(assistantNow, persist);
                }
                else if (string.Equals(
                        succeeded.AssistantTask,
                        AssistantTask.OfferPath,
                        StringComparison.Ordinal
                    )
                    && !productExpertTurn
                    && !AssistantAskIntent.IsHelpCentreAsk(userMessage))
                {
                    var persistLocationId = CreatePersistLocationId(boundCreateLocationId, conversation);
                    var persistLocationName = boundCreateLocationName ?? locationName;
                    // Model-extracted terms win when a field is set; stored
                    // resume terms and parse of the send fill the rest so an
                    // incomplete model extract cannot wipe named facts.
                    var offerResume = TryGetStoredCreateResumeContext(
                        conversation,
                        userMessage
                    );
                    var terms = AssistantOfferPathTerms.Overlay(
                        succeeded.OfferTerms,
                        AssistantOfferPathTerms.Overlay(
                            offerResume?.PriorTerms,
                            AssistantOfferPathTerms.Parse(userMessage)
                        )
                    );
                    var termsGap = await TryFinishOfferTermsGapAsync(
                        conversation,
                        offerResume?.SourceUserMessage ?? userMessage,
                        terms,
                        replaceFailure,
                        cancellationToken,
                        AssistantTask.OfferPath
                    );
                    if (termsGap is not null)
                    {
                        return termsGap;
                    }

                    var persist = await PersistCreateOfferDraftAsync(
                        conversation,
                        userMessage,
                        persistLocationId,
                        persistLocationName,
                        terms,
                        cancellationToken
                    );
                    conversation.DraftInterviewJson = null;
                    conversation.LastCompareLocationIdsJson = null;
                    if (persist.CreatedOfferId is int createdOfferId)
                    {
                        conversation.CreatedOfferId = createdOfferId;
                    }
                    assistantMessage = GroundedMessage(
                        assistantNow,
                        persist.Title,
                        persist.Body,
                        persist.Actions
                    );
                }
                else if (string.Equals(
                        succeeded.AssistantTask,
                        AssistantTask.RecoveryPath,
                        StringComparison.Ordinal
                    )
                    && !productExpertTurn
                    && !AssistantAskIntent.IsHelpCentreAsk(userMessage))
                {
                    var persist = await PersistPrepareRecoveryAsync(
                        conversation,
                        userMessage,
                        recoveryIdentity ?? savedEvidence.Feedback,
                        savedEvidence.Offers,
                        cancellationToken
                    );
                    if (persist.Gap is AssistantGapState feedbackGap)
                    {
                        return await FinishGapTurnAsync(
                            conversation,
                            feedbackGap,
                            persist.Body,
                            replaceFailure,
                            cancellationToken,
                            liveAnswerAlreadyCompleted: true,
                            turnBilling
                        );
                    }

                    ApplyRecoveryPersist(conversation, persist);
                    assistantMessage = GroundedMessage(
                        assistantNow,
                        persist.Title,
                        persist.Body,
                        persist.Actions
                    );
                }
                else
                {
                    var groundedAsk = AssistantAskIntent.ClassifyGrounded(userMessage);
                    var actions = AssistantActionCatalog.Validate(
                        succeeded.Actions,
                        succeeded.Class,
                        savedEvidence,
                        groundedAsk
                    );
                    var redactionTokens = savedEvidence.Feedback.ContactRedactionTokens
                        .Concat(savedEvidence.Guests.ContactRedactionTokens)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var title = succeeded.Class == AssistantMessageClass.Grounded
                        ? AssistantContactRedaction.RedactTitle(
                            succeeded.Title,
                            redactionTokens
                        )
                        : null;
                    var body = AssistantContactRedaction.RedactBody(
                        succeeded.Body,
                        redactionTokens
                    );
                    if (succeeded.Class == AssistantMessageClass.Grounded
                        && savedEvidence.IsEmpty
                        && groundedAsk != AssistantGroundedAsk.ListGuests
                        && compareEvidence is not { Count: >= 2 }
                        && !pureProductExpert
                        && !isCompareAll
                        && attentionSurface == AssistantAttentionSurface.None)
                    {
                        var empty = AssistantLiveAnswerCopy.EmptyGrounded(
                            locationName,
                            periodPhrase
                        );
                        var withCaveat = AssistantLiveAnswerCopy.WithSentences(
                            empty,
                            caveat,
                            droppedUnknown
                        );
                        title = withCaveat.Title;
                        body = withCaveat.Body;
                        actions = withCaveat.Actions;
                    }
                    if (isCompareAll)
                    {
                        actions = [];
                    }
                    if (productExpertTurn)
                    {
                        var canned = AssistantProductExpertTopics.Assemble(productTopics);
                        if (pureProductExpert)
                        {
                            title = canned.Title;
                            body = canned.Body;
                            actions = [];
                            proposedConversationTitle = canned.ConversationTitle;
                            if (conversation.Messages.Count(
                                    message => message.Role == AssistantMessageRole.User
                                ) == 1)
                            {
                                conversation.Title = canned.ConversationTitle;
                            }
                        }
                        else
                        {
                            body = $"{body}\n\n{canned.Body}";
                        }
                    }
                    assistantMessage = new AssistantMessage
                    {
                        Role = AssistantMessageRole.Assistant,
                        Class = productExpertTurn
                            ? AssistantMessageClass.Grounded
                            : succeeded.Class,
                        Title = title,
                        Body = body,
                        ActionsJson = AssistantAnalysisScope.SerializeActions(actions),
                        CreatedAt = assistantNow,
                    };
                    conversation.LastCompareLocationIdsJson =
                        !AssistantAnalysisScope.IsAll(conversation)
                        && compareOutcome is AssistantCompareOutcome.Compare compareOk
                        && succeeded.Class == AssistantMessageClass.Grounded
                            ? AssistantCompareTurn.SerializeLocationIds(compareOk.LocationIds)
                            : null;
                }
            }
            else
            {
                conversation.LastCompareLocationIdsJson = null;
                assistantMessage = FailureMessage(assistantNow);
            }

            return await PersistAssistantAsync(
                conversation,
                assistantMessage,
                replaceFailure,
                cancellationToken,
                proposedConversationTitle,
                liveAnswerAlreadyCompleted: true,
                turnBilling: turnBilling
            );
        }

        private sealed record CreateCampaignDraftTurn(
            AssistantMessageClass Class,
            string Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions,
            int? CreatedCampaignId,
            AssistantGapState? GapState
        );

        private async Task<CreateCampaignDraftTurn> PersistCreateCampaignDraftAsync(
            AssistantConversation conversation,
            string userMessage,
            int locationId,
            string locationName,
            IReadOnlyList<int> ownedLocationIds,
            CancellationToken cancellationToken,
            AssistantCampaignDraftBindChoice? choice = null,
            AssistantCampaignDraftBindOutcome? preparedBind = null
        )
        {
            var bind = choice is { HasValue: true } || preparedBind is null
                ? await BindCampaignAsync(
                    userMessage,
                    locationId,
                    locationName,
                    ownedLocationIds,
                    cancellationToken,
                    choice
                )
                : preparedBind;
            switch (bind)
            {
                case AssistantCampaignDraftBindOutcome.Gap gap:
                    return new CreateCampaignDraftTurn(
                        AssistantMessageClass.Gap,
                        string.Empty,
                        gap.Body,
                        [],
                        null,
                        AssistantGapTurn.CreateBindKind(
                            gap.Kind,
                            gap.Options,
                            userMessage,
                            AssistantTask.CreateCampaignDraft
                        )
                    );
                case AssistantCampaignDraftBindOutcome.UnevaluableAudience unevaluable:
                    return new CreateCampaignDraftTurn(
                        AssistantMessageClass.Grounded,
                        AssistantCampaignDraftPersistCopy.FailureTitle,
                        unevaluable.Body,
                        [],
                        null,
                        null
                    );
                case AssistantCampaignDraftBindOutcome.Bound bound:
                    return await PersistBoundCampaignDraftAsync(
                        conversation,
                        locationId,
                        locationName,
                        bound.Fields,
                        cancellationToken
                    );
                default:
                    throw new InvalidOperationException("Unknown Campaign Draft bind.");
            }
        }

        private async Task<CreateCampaignDraftTurn> PersistBoundCampaignDraftAsync(
            AssistantConversation conversation,
            int locationId,
            string locationName,
            AssistantCampaignDraftBindFields fields,
            CancellationToken cancellationToken
        )
        {
            var campaignName = fields.Name;
            string? messageSubject = null;
            string? messageBody = null;
            try
            {
                var copy = await _campaignMessageDrafts.PrepareAsync(
                    locationName,
                    new PrepareCampaignMessageDraftRequest
                    {
                        LocationId = locationId,
                        Channel = fields.Channel,
                        GoalId = fields.GoalId,
                        AudienceKey = fields.AudienceKey,
                        OfferStance = fields.OfferStance,
                        CampaignName = campaignName,
                        Tone = "friendly_and_clear",
                        Mode = "prepare",
                    },
                    cancellationToken
                );
                if (copy is CampaignMessageDraftServiceResult.Ok okCopy)
                {
                    messageSubject = string.Equals(
                        fields.Channel,
                        "sms",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? null
                        : okCopy.Subject;
                    messageBody = okCopy.Body;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Persist with empty message fields when copy generate fails.
            }

            CampaignDraftDto created;
            if (!await CanPersistDraftAsync(
                conversation.OwnerUserId,
                OperatorAreaIds.Campaigns,
                locationId
            ))
            {
                return new CreateCampaignDraftTurn(
                    AssistantMessageClass.Grounded,
                    AssistantCampaignDraftPersistCopy.FailureTitle,
                    AssistantCampaignDraftPersistCopy.FailureBody("Campaign create"),
                    [],
                    null,
                    null
                );
            }

            try
            {
                created = await _campaignDrafts.CreateAsync(
                    new CreateCampaignDraftRequest
                    {
                        LocationId = locationId,
                        Name = campaignName,
                        GoalId = fields.GoalId,
                        AudienceKey = fields.AudienceKey,
                        Channel = fields.Channel,
                        OfferStance = fields.OfferStance,
                        OfferId = fields.OfferId,
                        TemplateId = fields.TemplateId,
                        MessageSubject = messageSubject,
                        MessageBody = messageBody,
                    },
                    conversation.OwnerUserId,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new CreateCampaignDraftTurn(
                    AssistantMessageClass.Grounded,
                    AssistantCampaignDraftPersistCopy.FailureTitle,
                    AssistantCampaignDraftPersistCopy.FailureBody("Campaign create"),
                    [],
                    null,
                    null
                );
            }

            int? eligibleCount = null;
            try
            {
                var eligibility = await _campaignEligibility.EvaluateAsync(
                    locationId,
                    fields.AudienceKey,
                    cancellationToken
                );
                eligibleCount = string.Equals(
                    fields.Channel,
                    "sms",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? eligibility.SmsEligible
                    : eligibility.EmailEligible;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                eligibleCount = null;
            }

            return new CreateCampaignDraftTurn(
                AssistantMessageClass.Grounded,
                AssistantCampaignDraftPersistCopy.SuccessTitle,
                AssistantCampaignDraftPersistCopy.SuccessBody(
                    locationName,
                    fields.ChannelLabel,
                    fields.AudienceLabel,
                    eligibleCount,
                    created.Name,
                    created.OfferStance == "existing-offer" && created.OfferId is not null
                        ? fields.OfferLabel
                        : "No Offer",
                    fields.OfferNote
                ),
                AssistantActionCatalog.ValidateReviewCampaign(
                    created.Id,
                    AssistantMessageClass.Grounded,
                    created.OfferStance,
                    created.OfferId
                ),
                created.Id,
                null
            );
        }

        private sealed record CombinedCreateTurn(
            AssistantMessageClass Class,
            string Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions,
            int? CreatedCampaignId,
            int? CreatedOfferId,
            AssistantGapState? GapState
        );

        private async Task<CombinedCreateTurn> PersistCreateCampaignWithOfferAsync(
            AssistantConversation conversation,
            string userMessage,
            int locationId,
            string locationName,
            IReadOnlyList<int> ownedLocationIds,
            CancellationToken cancellationToken,
            AssistantCampaignDraftBindOutcome? preparedBind = null,
            AssistantCampaignDraftBindChoice? choice = null,
            AssistantOfferPathTermsState? priorTerms = null,
            string questionBody = ""
        )
        {
            var bind = preparedBind is AssistantCampaignDraftBindOutcome.Bound
                ? preparedBind
                : await BindCampaignAsync(
                    userMessage,
                    locationId,
                    locationName,
                    ownedLocationIds,
                    cancellationToken,
                    ignoreOffers: true
                );
            switch (bind)
            {
                case AssistantCampaignDraftBindOutcome.Gap gap:
                    return new CombinedCreateTurn(
                        AssistantMessageClass.Gap,
                        string.Empty,
                        gap.Body,
                        [],
                        null,
                        null,
                        AssistantGapTurn.CreateBindKind(
                            gap.Kind,
                            gap.Options,
                            userMessage,
                            AssistantTask.CreateCampaignWithOffer
                        )
                    );
                case AssistantCampaignDraftBindOutcome.UnevaluableAudience unevaluable:
                    return CombinedFullFailure(unevaluable.Body);
                case AssistantCampaignDraftBindOutcome.Bound bound:
                    return await PersistBoundCampaignWithOfferAsync(
                        conversation,
                        userMessage,
                        locationId,
                        locationName,
                        ownedLocationIds,
                        bound.Fields,
                        cancellationToken,
                        choice,
                        priorTerms,
                        questionBody
                    );
                default:
                    throw new InvalidOperationException(
                        "Unknown Campaign with Offer bind."
                    );
            }
        }

        private async Task<CombinedCreateTurn> PersistBoundCampaignWithOfferAsync(
            AssistantConversation conversation,
            string userMessage,
            int locationId,
            string locationName,
            IReadOnlyList<int> ownedLocationIds,
            AssistantCampaignDraftBindFields fields,
            CancellationToken cancellationToken,
            AssistantCampaignDraftBindChoice? choice = null,
            AssistantOfferPathTermsState? priorTerms = null,
            string questionBody = ""
        )
        {
            var attachable = await LoadAttachableOffersAsync(
                locationId,
                ownedLocationIds,
                cancellationToken
            );
            var matches = choice?.OfferTitle is { Length: > 0 } chosenTitle
                ? attachable
                    .Where(offer =>
                        offer.Attachable
                        && offer.Title.Equals(
                            chosenTitle,
                            StringComparison.OrdinalIgnoreCase
                        ))
                    .ToList()
                : AssistantCampaignDraftBind.MatchAttachable(
                    userMessage,
                    attachable
                );
            if (matches.Count >= 2 && choice?.OfferTitle is null)
            {
                var titles = matches.Select(offer => offer.Title).ToList();
                return new CombinedCreateTurn(
                    AssistantMessageClass.Gap,
                    string.Empty,
                    AssistantCampaignDraftBind.OfferClashBody(titles),
                    [],
                    null,
                    null,
                    AssistantGapTurn.CreateOffer(
                        titles,
                        userMessage,
                        AssistantTask.CreateCampaignWithOffer
                    )
                );
            }

            string offerStance;
            int offerId;
            CatalogOfferDto offer;
            var createdOfferThisTurn = false;
            if (matches.Count == 1)
            {
                var matched = matches[0];
                var loaded = await _offersCatalog.GetByIdAsync(
                    matched.Id,
                    utcOffsetMinutes: 0,
                    cancellationToken
                );
                if (loaded is null)
                {
                    return CombinedFullFailure(
                        AssistantCombinedCreatePersistCopy.FullFailureBody("Offer match")
                    );
                }

                offer = loaded;
                offerId = loaded.Id;
                offerStance = "existing-offer";
            }
            else
            {
                var terms = priorTerms ?? AssistantOfferPathTerms.Parse(userMessage);
                AssistantOfferPathTerms.ProposeCopy(terms);
                if (!AssistantOfferPathTerms.IsComplete(terms))
                {
                    return new CombinedCreateTurn(
                        AssistantMessageClass.Gap,
                        string.Empty,
                        AssistantGapAsk.ForOfferTerms(terms),
                        [],
                        null,
                        null,
                        AssistantGapTurn.CreateCombinedOfferTerms(
                            userMessage,
                            terms,
                            AssistantTask.CreateCampaignWithOffer
                        )
                    );
                }

                try
                {
                    if (!await CanPersistDraftAsync(
                        conversation.OwnerUserId,
                        OperatorAreaIds.Offers,
                        locationId
                    ))
                    {
                        return CombinedFullFailure(
                            AssistantCombinedCreatePersistCopy.FullFailureBody("Offer create")
                        );
                    }

                    offer = await _offersCatalog.CreateDraftAsync(
                        AssistantOfferPathTerms.ToCreateRequest(terms, locationId),
                        conversation.OwnerUserId,
                        cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    return CombinedFullFailure(
                        AssistantCombinedCreatePersistCopy.FullFailureBody("Offer create")
                    );
                }

                offerId = offer.Id;
                offerStance = "create-new-offer";
                createdOfferThisTurn = true;
            }

            var campaigns = await LoadLocationCampaignRefsAsync(locationId, cancellationToken);
            var campaignOutcome = AssistantCombinedCreateCampaignResolve.Resolve(
                userMessage,
                campaigns,
                choice?.CampaignTitle
            );
            switch (campaignOutcome)
            {
                case AssistantCombinedCreateCampaignOutcome.Gap gap:
                    return new CombinedCreateTurn(
                        AssistantMessageClass.Gap,
                        string.Empty,
                        gap.Body,
                        [],
                        null,
                        null,
                        AssistantGapTurn.CreateCampaignTitle(
                            gap.Options,
                            userMessage,
                            AssistantTask.CreateCampaignWithOffer
                        )
                    );
                case AssistantCombinedCreateCampaignOutcome.RefuseInFlight refuse:
                    if (createdOfferThisTurn)
                    {
                        return new CombinedCreateTurn(
                            AssistantMessageClass.Grounded,
                            AssistantCombinedCreatePersistCopy.FailureTitle,
                            AssistantCombinedCreatePersistCopy.PartialFailureBody(
                                "Campaign create",
                                locationName,
                                AssistantCombinedCreatePersistCopy.TypeLabel(offer),
                                AssistantCombinedCreatePersistCopy.ValueLabel(offer),
                                AssistantCombinedCreatePersistCopy.ValidityLabel(offer),
                                offer.Title
                            ),
                            AssistantActionCatalog.ValidateReviewOffer(
                                offerId,
                                AssistantMessageClass.Grounded
                            ),
                            null,
                            offerId,
                            null
                        );
                    }

                    return CombinedFullFailure(refuse.Body);
                case AssistantCombinedCreateCampaignOutcome.UpdateExisting update:
                    return await AttachOfferToExistingCampaignDraftAsync(
                        locationId,
                        locationName,
                        update.CampaignId,
                        offerId,
                        offerStance,
                        offer,
                        createdOfferThisTurn,
                        cancellationToken
                    );
                case AssistantCombinedCreateCampaignOutcome.CreateNew createNew:
                    var campaignName = !string.IsNullOrWhiteSpace(createNew.NamedTitle)
                        ? createNew.NamedTitle.Trim()
                        : fields.Name;
                    return await CreateCampaignDraftWithOfferAttachAsync(
                        conversation,
                        locationId,
                        locationName,
                        fields with { Name = campaignName },
                        offerId,
                        offerStance,
                        offer,
                        createdOfferThisTurn,
                        cancellationToken
                    );
                default:
                    throw new InvalidOperationException(
                        "Unknown combined create Campaign outcome."
                    );
            }
        }

        private async Task<CombinedCreateTurn> AttachOfferToExistingCampaignDraftAsync(
            int locationId,
            string locationName,
            int campaignId,
            int offerId,
            string offerStance,
            CatalogOfferDto offer,
            bool createdOfferThisTurn,
            CancellationToken cancellationToken
        )
        {
            var existing = await _campaignDrafts.GetByIdAsync(campaignId, cancellationToken);
            if (existing is null)
            {
                if (createdOfferThisTurn)
                {
                    return CombinedFullFailure(
                        AssistantCombinedCreatePersistCopy.FullFailureBody("Campaign create")
                    );
                }

                return CombinedFullFailure(
                    AssistantCombinedCreatePersistCopy.FullFailureBody("Campaign match")
                );
            }

            CampaignDraftWriteResult patchResult;
            try
            {
                patchResult = await _campaignDrafts.PatchAsync(
                    campaignId,
                    new PatchCampaignDraftRequest
                    {
                        RowVersion = existing.RowVersion,
                        OfferStance = offerStance,
                        OfferId = offerId,
                    },
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                patchResult = new CampaignDraftWriteResult.NotFound();
            }

            if (patchResult is not CampaignDraftWriteResult.Ok okPatch)
            {
                if (!createdOfferThisTurn
                    && !string.Equals(
                        offer.Status,
                        CatalogOfferStatus.Draft,
                        StringComparison.Ordinal
                    ))
                {
                    return CombinedFullFailure(
                        AssistantCombinedCreatePersistCopy.FullFailureBody("Campaign create")
                    );
                }

                return new CombinedCreateTurn(
                    AssistantMessageClass.Grounded,
                    AssistantCombinedCreatePersistCopy.FailureTitle,
                    AssistantCombinedCreatePersistCopy.PartialFailureBody(
                        "Campaign create",
                        locationName,
                        AssistantCombinedCreatePersistCopy.TypeLabel(offer),
                        AssistantCombinedCreatePersistCopy.ValueLabel(offer),
                        AssistantCombinedCreatePersistCopy.ValidityLabel(offer),
                        offer.Title
                    ),
                    AssistantActionCatalog.ValidateReviewOffer(
                        offerId,
                        AssistantMessageClass.Grounded
                    ),
                    null,
                    offerId,
                    null
                );
            }

            return await BuildCombinedCreateSuccessTurnAsync(
                locationId,
                locationName,
                okPatch.Campaign,
                offerId,
                offer,
                cancellationToken
            );
        }

        private async Task<CombinedCreateTurn> CreateCampaignDraftWithOfferAttachAsync(
            AssistantConversation conversation,
            int locationId,
            string locationName,
            AssistantCampaignDraftBindFields fields,
            int offerId,
            string offerStance,
            CatalogOfferDto offer,
            bool createdOfferThisTurn,
            CancellationToken cancellationToken
        )
        {
            string? messageSubject = null;
            string? messageBody = null;
            try
            {
                var copy = await _campaignMessageDrafts.PrepareAsync(
                    locationName,
                    new PrepareCampaignMessageDraftRequest
                    {
                        LocationId = locationId,
                        Channel = fields.Channel,
                        GoalId = fields.GoalId,
                        AudienceKey = fields.AudienceKey,
                        OfferStance = offerStance,
                        CampaignName = fields.Name,
                        Tone = "friendly_and_clear",
                        Mode = "prepare",
                    },
                    cancellationToken
                );
                if (copy is CampaignMessageDraftServiceResult.Ok okCopy)
                {
                    messageSubject = string.Equals(
                        fields.Channel,
                        "sms",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? null
                        : okCopy.Subject;
                    messageBody = okCopy.Body;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Persist with empty message fields when copy generate fails.
            }

            CampaignDraftDto created;
            if (!await CanPersistDraftAsync(
                conversation.OwnerUserId,
                OperatorAreaIds.Campaigns,
                locationId
            ))
            {
                return CombinedFullFailure(
                    AssistantCombinedCreatePersistCopy.FullFailureBody("Campaign create")
                );
            }

            try
            {
                created = await _campaignDrafts.CreateAsync(
                    new CreateCampaignDraftRequest
                    {
                        LocationId = locationId,
                        Name = fields.Name,
                        GoalId = fields.GoalId,
                        AudienceKey = fields.AudienceKey,
                        Channel = fields.Channel,
                        OfferStance = offerStance,
                        OfferId = offerId,
                        TemplateId = fields.TemplateId,
                        MessageSubject = messageSubject,
                        MessageBody = messageBody,
                    },
                    conversation.OwnerUserId,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                if (!createdOfferThisTurn
                    && !string.Equals(
                        offer.Status,
                        CatalogOfferStatus.Draft,
                        StringComparison.Ordinal
                    ))
                {
                    return CombinedFullFailure(
                        AssistantCombinedCreatePersistCopy.FullFailureBody("Campaign create")
                    );
                }

                return new CombinedCreateTurn(
                    AssistantMessageClass.Grounded,
                    AssistantCombinedCreatePersistCopy.FailureTitle,
                    AssistantCombinedCreatePersistCopy.PartialFailureBody(
                        "Campaign create",
                        locationName,
                        AssistantCombinedCreatePersistCopy.TypeLabel(offer),
                        AssistantCombinedCreatePersistCopy.ValueLabel(offer),
                        AssistantCombinedCreatePersistCopy.ValidityLabel(offer),
                        offer.Title
                    ),
                    AssistantActionCatalog.ValidateReviewOffer(
                        offerId,
                        AssistantMessageClass.Grounded
                    ),
                    null,
                    offerId,
                    null
                );
            }

            return await BuildCombinedCreateSuccessTurnAsync(
                locationId,
                locationName,
                created,
                offerId,
                offer,
                cancellationToken,
                fields.ChannelLabel,
                fields.AudienceLabel,
                fields.AudienceKey
            );
        }

        private async Task<CombinedCreateTurn> BuildCombinedCreateSuccessTurnAsync(
            int locationId,
            string locationName,
            CampaignDraftDto campaign,
            int offerId,
            CatalogOfferDto offer,
            CancellationToken cancellationToken,
            string? channelLabel = null,
            string? audienceLabel = null,
            string? audienceKey = null
        )
        {
            var attached = await _offersCatalog.GetByIdAsync(
                offerId,
                utcOffsetMinutes: 0,
                cancellationToken
            ) ?? offer;

            var resolvedChannelLabel = channelLabel
                ?? (string.Equals(
                    campaign.Channel,
                    "sms",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "SMS"
                    : "Email");
            var resolvedAudienceKey = audienceKey ?? campaign.AudienceKey
                ?? AssistantCampaignDraftBind.AudienceAllEligible;
            var resolvedAudienceLabel = audienceLabel
                ?? (AssistantCampaignDraftBind.AudienceLabels.TryGetValue(
                    resolvedAudienceKey,
                    out var label
                )
                    ? label
                    : "All eligible guests");

            int? eligibleCount = null;
            try
            {
                var eligibility = await _campaignEligibility.EvaluateAsync(
                    locationId,
                    resolvedAudienceKey,
                    cancellationToken
                );
                eligibleCount = string.Equals(
                    campaign.Channel ?? "email",
                    "sms",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? eligibility.SmsEligible
                    : eligibility.EmailEligible;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                eligibleCount = null;
            }

            return new CombinedCreateTurn(
                AssistantMessageClass.Grounded,
                AssistantCombinedCreatePersistCopy.SuccessTitle,
                AssistantCombinedCreatePersistCopy.SuccessBody(
                    locationName,
                    resolvedChannelLabel,
                    resolvedAudienceLabel,
                    eligibleCount,
                    AssistantCombinedCreatePersistCopy.TypeLabel(attached),
                    AssistantCombinedCreatePersistCopy.ValueLabel(attached),
                    AssistantCombinedCreatePersistCopy.ValidityLabel(attached),
                    attached.Title,
                    campaign.Name
                ),
                AssistantActionCatalog.ValidateCombinedCreate(
                    campaign.Id,
                    offerId,
                    AssistantMessageClass.Grounded
                ),
                campaign.Id,
                offerId,
                null
            );
        }

        private static CombinedCreateTurn CombinedFullFailure(string body)
            => new(
                AssistantMessageClass.Grounded,
                AssistantCombinedCreatePersistCopy.FailureTitle,
                body,
                [],
                null,
                null,
                null
            );

        private sealed record CreateOfferDraftPersistTurn(
            string Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions,
            int? CreatedOfferId,
            string ThankYouAttach = "none",
            string? ThankYouOfferTitle = null,
            bool ThankYouOfferLive = false
        );

        private async Task<CreateOfferDraftPersistTurn> PersistCreateOfferDraftAsync(
            AssistantConversation conversation,
            string userMessage,
            int locationId,
            string locationName,
            AssistantOfferPathTermsState? priorTerms,
            CancellationToken cancellationToken
        )
        {
            var terms = priorTerms ?? AssistantOfferPathTerms.Parse(userMessage);
            AssistantOfferPathTerms.ProposeCopy(terms);
            if (!AssistantOfferPathTerms.IsComplete(terms))
            {
                return new CreateOfferDraftPersistTurn(
                    AssistantOfferPathPersistCopy.FailureTitle,
                    AssistantOfferPathPersistCopy.FailureBody("Offer create"),
                    [],
                    null
                );
            }

            CatalogOfferDto created;
            if (!await CanPersistDraftAsync(
                conversation.OwnerUserId,
                OperatorAreaIds.Offers,
                locationId
            ))
            {
                return new CreateOfferDraftPersistTurn(
                    AssistantOfferPathPersistCopy.FailureTitle,
                    AssistantOfferPathPersistCopy.FailureBody("Offer create"),
                    [],
                    null
                );
            }

            try
            {
                created = await _offersCatalog.CreateDraftAsync(
                    AssistantOfferPathTerms.ToCreateRequest(terms, locationId),
                    conversation.OwnerUserId,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ArgumentException argumentError)
            {
                return new CreateOfferDraftPersistTurn(
                    AssistantOfferPathPersistCopy.FailureTitle,
                    AssistantOfferPathPersistCopy.InvalidValueBody(
                        argumentError.Message
                    ),
                    [],
                    null
                );
            }
            catch
            {
                return new CreateOfferDraftPersistTurn(
                    AssistantOfferPathPersistCopy.FailureTitle,
                    AssistantOfferPathPersistCopy.FailureBody("Offer create"),
                    [],
                    null
                );
            }

            var thankYouAttach = "none";
            string? thankYouOfferTitle = null;
            var thankYouOfferLive = false;
            if (string.Equals(
                    terms.Placement,
                    AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                    StringComparison.Ordinal
                ))
            {
                try
                {
                    var setResult = await _thankYouOffers.SetAsync(
                        locationId,
                        created.Id,
                        cancellationToken
                    );
                    if (setResult is CaptureThankYouOfferSetResult.Ok ok
                        && ok.Value.ThankYouOfferId == created.Id)
                    {
                        thankYouAttach = "attached";
                        thankYouOfferTitle = ok.Value.ThankYouOfferTitle;
                        thankYouOfferLive = ok.Value.ThankYouOfferLive;
                    }
                    else
                    {
                        thankYouAttach = "failed";
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    thankYouAttach = "failed";
                }
            }

            return new CreateOfferDraftPersistTurn(
                AssistantOfferPathPersistCopy.TitleFor(thankYouAttach),
                AssistantOfferPathPersistCopy.SuccessBody(
                    locationName,
                    AssistantOfferPathTerms.TypeLabel(terms.OfferType),
                    AssistantOfferPathTerms.ValueLabel(terms),
                    AssistantOfferPathTerms.ValidityLabel(terms),
                    created.Title,
                    terms.WantsActivate,
                    thankYouAttach,
                    thankYouOfferLive
                ),
                AssistantActionCatalog.ValidateReviewOffer(
                    created.Id,
                    AssistantMessageClass.Grounded
                ),
                created.Id,
                thankYouAttach,
                thankYouOfferTitle,
                thankYouOfferLive
            );
        }

        private sealed record RecoveryPersistTurn(
            string Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions,
            AssistantRecoveryWorkState? Work,
            AssistantGapState? Gap
        );

        private async Task<AssistantTurnOutcome> ResumeFeedbackGapAsync(
            AssistantConversation conversation,
            AssistantGapState gapState,
            string userMessage,
            AssistantFeedbackEvidence feedbackEvidence,
            AssistantOffersEvidence offersEvidence,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            var includeVenue = AssistantAnalysisScope.IsAll(conversation);
            var match = AssistantRecoveryIdentity.Resolve(
                userMessage,
                feedbackEvidence.Rows
            );
            switch (match)
            {
                case AssistantRecoveryIdentity.Match.Many many:
                    return await FinishGapTurnAsync(
                        conversation,
                        AssistantGapTurn.CreateFeedback(
                            many.Rows
                                .Select(row =>
                                    AssistantRecoveryIdentity.FormatLabel(row, includeVenue)
                                )
                                .ToList(),
                            gapState.SourceUserMessage
                        ),
                        AssistantRecoveryIdentity.RepeatGapBody(
                            many.Rows
                                .Select(row =>
                                    AssistantRecoveryIdentity.FormatLabel(row, includeVenue)
                                )
                                .ToList()
                        ),
                        replaceFailure,
                        cancellationToken
                    );
                case AssistantRecoveryIdentity.Match.None:
                    return await FinishGapTurnAsync(
                        conversation,
                        gapState,
                        AssistantRecoveryIdentity.RepeatGapBody(gapState.Options),
                        replaceFailure,
                        cancellationToken
                    );
                case AssistantRecoveryIdentity.Match.One one:
                    var persist = await PersistPrepareRecoveryAsync(
                        conversation,
                        gapState.SourceUserMessage,
                        feedbackEvidence,
                        offersEvidence,
                        cancellationToken,
                        one.Row
                    );
                    ApplyRecoveryPersist(conversation, persist);
                    return await PersistAssistantAsync(
                        conversation,
                        GroundedMessage(
                            DateTime.UtcNow,
                            persist.Title,
                            persist.Body,
                            persist.Actions
                        ),
                        replaceFailure,
                        cancellationToken
                    );
                default:
                    return await FinishGapTurnAsync(
                        conversation,
                        gapState,
                        AssistantRecoveryIdentity.RepeatGapBody(gapState.Options),
                        replaceFailure,
                        cancellationToken
                    );
            }
        }

        private async Task<RecoveryPersistTurn> PersistPrepareRecoveryAsync(
            AssistantConversation conversation,
            string userMessage,
            AssistantFeedbackEvidence feedbackEvidence,
            AssistantOffersEvidence offersEvidence,
            CancellationToken cancellationToken,
            AssistantFeedbackEvidenceRow? boundRow = null
        )
        {
            IReadOnlyList<AssistantActionDto> none = [];
            var intent = AssistantRecoveryIntent.Bind(userMessage);
            string? internalCategory = null;
            string? internalNote = null;
            if (intent == AssistantRecoveryEligibility.IntentInternalOnly
                || intent == AssistantRecoveryEligibility.IntentRespondAndRecord)
            {
                (internalCategory, internalNote) =
                    AssistantRecoveryIntent.BindInternalFields(userMessage);
                if (internalCategory is null || internalNote is null)
                {
                    return new RecoveryPersistTurn(
                        AssistantRecoveryPersistCopy.FailureTitle,
                        AssistantRecoveryPersistCopy.InternalUnboundBody(),
                        none,
                        null,
                        null
                    );
                }
            }

            var includeVenue = AssistantAnalysisScope.IsAll(conversation);
            var match = boundRow is not null
                ? new AssistantRecoveryIdentity.Match.One(boundRow)
                : AssistantRecoveryIdentity.Resolve(
                    userMessage,
                    feedbackEvidence.Rows
                );
            switch (match)
            {
                case AssistantRecoveryIdentity.Match.None miss:
                    var scope = AssistantAnalysisScope.FromConversation(conversation);
                    return new RecoveryPersistTurn(
                        AssistantRecoveryPersistCopy.FailureTitle,
                        AssistantRecoveryPersistCopy.ZeroMatchBody(
                            miss.Reason,
                            conversation.OwnedLocationName,
                            AssistantAnalysisScope.PeriodPhrase(scope.ReportingPeriod)
                        ),
                        none,
                        null,
                        null
                    );
                case AssistantRecoveryIdentity.Match.Many many:
                    var labels = many.Rows
                        .Select(row =>
                            AssistantRecoveryIdentity.FormatLabel(row, includeVenue)
                        )
                        .ToList();
                    return new RecoveryPersistTurn(
                        "",
                        AssistantRecoveryIdentity.GapBody(many.Rows, includeVenue),
                        none,
                        null,
                        AssistantGapTurn.CreateFeedback(labels, userMessage)
                    );
            }

            var row = ((AssistantRecoveryIdentity.Match.One)match).Row;
            Feedback? feedback;
            try
            {
                feedback = await _context.Feedbacks
                    .Include(item => item.LocationGuest)
                    .FirstOrDefaultAsync(item => item.Id == row.Id, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new RecoveryPersistTurn(
                    AssistantRecoveryPersistCopy.FailureTitle,
                    AssistantRecoveryPersistCopy.UnavailableBody(),
                    none,
                    null,
                    null
                );
            }

            if (feedback is null)
            {
                return new RecoveryPersistTurn(
                    AssistantRecoveryPersistCopy.FailureTitle,
                    AssistantRecoveryPersistCopy.UnavailableBody(),
                    none,
                    null,
                    null
                );
            }

            if (!await CanPersistDraftAsync(
                conversation.OwnerUserId,
                OperatorAreaIds.Feedback,
                feedback.RestaurantLocationId
            ))
            {
                return new RecoveryPersistTurn(
                    AssistantRecoveryPersistCopy.FailureTitle,
                    AssistantRecoveryPersistCopy.FailureBody("Recovery prepare"),
                    none,
                    null,
                    null
                );
            }

            var persistLocationId = row.LocationId ?? feedback.RestaurantLocationId;
            var eligibility = AssistantRecoveryEligibility.Evaluate(feedback, intent);
            if (eligibility is AssistantRecoveryEligibility.Outcome.Blocked blocked)
            {
                return new RecoveryPersistTurn(
                    AssistantRecoveryPersistCopy.FailureTitle,
                    blocked.Body,
                    none,
                    null,
                    null
                );
            }

            var allowed = (AssistantRecoveryEligibility.Outcome.Allowed)eligibility;
            int? offerId = null;
            if (intent == AssistantRecoveryEligibility.IntentRecoveryOffer)
            {
                if (AssistantAnalysisScope.IsAll(conversation))
                {
                    var loadedOffers = await RetrieveOffersAtVenueAsync(
                        conversation.OwnerUserId,
                        persistLocationId,
                        conversation,
                        cancellationToken
                    );
                    if (loadedOffers is null)
                    {
                        return new RecoveryPersistTurn(
                            AssistantRecoveryPersistCopy.FailureTitle,
                            AssistantRecoveryPersistCopy.UnavailableBody(),
                            none,
                            null,
                            null
                        );
                    }

                    offersEvidence = loadedOffers;
                }

                offerId = AssistantRecoveryIntent.BindOfferId(
                    userMessage,
                    offersEvidence.Catalog
                );
                if (offerId is null)
                {
                    return new RecoveryPersistTurn(
                        AssistantRecoveryPersistCopy.FailureTitle,
                        AssistantRecoveryPersistCopy.OfferUnboundBody(),
                        none,
                        null,
                        null
                    );
                }
            }

            var includeNotes = AssistantRecoveryIntent.BindIncludeNotes(userMessage);
            if (intent == AssistantRecoveryEligibility.IntentInternalOnly)
            {
                var internalWork = new AssistantRecoveryWorkState
                {
                    FeedbackId = row.Id,
                    LocationId = persistLocationId,
                    Intent = intent,
                    IncludeNotes = includeNotes,
                    Category = internalCategory,
                    Note = internalNote,
                    EligibilitySnapshot = allowed.Snapshot,
                };
                return new RecoveryPersistTurn(
                    AssistantRecoveryPersistCopy.SuccessTitle,
                    AssistantRecoveryPersistCopy.SuccessInternalBody(row.GuestName),
                    ReviewRecoveryActions(row.Id, intent),
                    internalWork,
                    null
                );
            }

            var purpose = AssistantRecoveryIntent.BindPurpose(userMessage, intent);
            var tone = AssistantRecoveryIntent.BindTone(userMessage);
            var channel = allowed.Channel ?? "";
            var prepareNotes = string.IsNullOrWhiteSpace(includeNotes)
                ? null
                : includeNotes;
            var recordCategory =
                intent == AssistantRecoveryEligibility.IntentRespondAndRecord
                    ? internalCategory
                    : null;
            var recordNote =
                intent == AssistantRecoveryEligibility.IntentRespondAndRecord
                    ? internalNote
                    : null;
            PrepareFeedbackRecoveryDraftResultDto? copy;
            try
            {
                copy = await _recoveryDrafts.PrepareAsync(
                    row.Id,
                    channel,
                    purpose,
                    tone,
                    includeNotes: prepareNotes,
                    mode: "prepare",
                    currentBody: null,
                    currentSubject: null,
                    confirmedInternalActionCategory: recordCategory,
                    confirmedInternalActionNote: recordNote,
                    cancellationToken: cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new RecoveryPersistTurn(
                    AssistantRecoveryPersistCopy.FailureTitle,
                    AssistantRecoveryPersistCopy.FailureBody("copy prepare"),
                    none,
                    null,
                    null
                );
            }

            if (copy is null || !copy.Success)
            {
                return new RecoveryPersistTurn(
                    AssistantRecoveryPersistCopy.FailureTitle,
                    AssistantRecoveryPersistCopy.FailureBody("copy prepare"),
                    none,
                    null,
                    null
                );
            }

            var work = new AssistantRecoveryWorkState
            {
                FeedbackId = row.Id,
                LocationId = persistLocationId,
                Intent = intent,
                Channel = channel,
                Purpose = purpose,
                Tone = tone,
                IncludeNotes = includeNotes,
                Subject = copy.Subject,
                Message = copy.Body,
                Category = recordCategory,
                Note = recordNote,
                OfferId = offerId,
                UseConfirmedActionForGuestResponse =
                    intent == AssistantRecoveryEligibility.IntentRespondAndRecord,
                EligibilitySnapshot = allowed.Snapshot,
            };
            return new RecoveryPersistTurn(
                AssistantRecoveryPersistCopy.SuccessTitle,
                AssistantRecoveryPersistCopy.SuccessBody(
                    intent,
                    row.GuestName,
                    AssistantRecoveryPersistCopy.ChannelLabel(channel)
                ),
                ReviewRecoveryActions(row.Id, intent),
                work,
                null
            );
        }

        private static void ApplyRecoveryPersist(
            AssistantConversation conversation,
            RecoveryPersistTurn persist
        )
        {
            conversation.DraftInterviewJson = null;
            conversation.LastCompareLocationIdsJson = null;
            if (persist.Work is AssistantRecoveryWorkState work)
            {
                conversation.RecoveryWorkJson =
                    AssistantRecoveryWork.Serialize(work);
            }
        }

        private static IReadOnlyList<AssistantActionDto> ReviewRecoveryActions(
            int feedbackId,
            string intent
        )
            => AssistantActionCatalog.ValidateOpenRecovery(
                [
                    new AssistantActionDto
                    {
                        Type = "open-recovery",
                        FeedbackId = feedbackId,
                        Intent = intent,
                    },
                ],
                AssistantMessageClass.Grounded
            );

        private async Task<AssistantTurnOutcome?> TryFinishCombinedCreatePrePersistGapsAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            int locationId,
            IReadOnlyList<int> ownedLocationIds,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            AssistantCampaignDraftBindChoice? choice = null,
            AssistantOfferPathTermsState? priorTerms = null
        )
        {
            var attachable = await LoadAttachableOffersAsync(
                locationId,
                ownedLocationIds,
                cancellationToken
            );
            var offerMatches = AssistantCampaignDraftBind.MatchAttachable(
                sourceUserMessage,
                attachable
            );
            if (offerMatches.Count >= 2 && choice?.OfferTitle is null)
            {
                var titles = offerMatches.Select(offer => offer.Title).ToList();
                return await FinishGapTurnAsync(
                    conversation,
                    AssistantGapTurn.CreateOffer(
                        titles,
                        sourceUserMessage,
                        AssistantTask.CreateCampaignWithOffer
                    ),
                    AssistantCampaignDraftBind.OfferClashBody(titles),
                    replaceFailure,
                    cancellationToken
                );
            }

            // Offer-terms completeness is gated after the live answer so
            // named facts and product-owned Gap asks can use overlay.

            var campaigns = await LoadLocationCampaignRefsAsync(
                locationId,
                cancellationToken
            );
            var campaignOutcome = AssistantCombinedCreateCampaignResolve.Resolve(
                sourceUserMessage,
                campaigns,
                choice?.CampaignTitle
            );
            return campaignOutcome switch
            {
                AssistantCombinedCreateCampaignOutcome.Gap gap =>
                    await FinishGapTurnAsync(
                        conversation,
                        AssistantGapTurn.CreateCampaignTitle(
                            gap.Options,
                            sourceUserMessage,
                            AssistantTask.CreateCampaignWithOffer
                        ),
                        gap.Body,
                        replaceFailure,
                        cancellationToken
                    ),
                AssistantCombinedCreateCampaignOutcome.RefuseInFlight refuse =>
                    await PersistCombinedCreateRefusalAsync(
                        conversation,
                        refuse.Body,
                        replaceFailure,
                        cancellationToken
                    ),
                _ => null,
            };
        }

        private async Task<AssistantTurnOutcome> PersistCombinedCreateRefusalAsync(
            AssistantConversation conversation,
            string body,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            conversation.DraftInterviewJson = null;
            conversation.LastCompareLocationIdsJson = null;
            return await PersistAssistantAsync(
                conversation,
                GroundedMessage(
                    DateTime.UtcNow,
                    AssistantCombinedCreatePersistCopy.FailureTitle,
                    body,
                    []
                ),
                replaceFailure,
                cancellationToken
            );
        }

        private async Task<IReadOnlyList<AssistantCombinedCreateCampaignRef>> LoadLocationCampaignRefsAsync(
            int locationId,
            CancellationToken cancellationToken
        )
            => await _context.Campaigns
                .AsNoTracking()
                .Where(campaign => campaign.RestaurantLocationId == locationId)
                .Select(campaign => new AssistantCombinedCreateCampaignRef(
                    campaign.Id,
                    campaign.Name,
                    campaign.Status
                ))
                .ToListAsync(cancellationToken);

        private async Task<AssistantTurnOutcome?> TryFinishOfferTermsGapAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            AssistantOfferPathTermsState terms,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            string assistantTask
        )
        {
            if (AssistantOfferPathTerms.IsComplete(terms))
            {
                return null;
            }

            return await FinishGapTurnAsync(
                conversation,
                AssistantGapTurn.CreateCombinedOfferTerms(
                    sourceUserMessage,
                    terms,
                    assistantTask
                ),
                AssistantGapAsk.ForOfferTerms(terms),
                replaceFailure,
                cancellationToken
            );
        }

        private sealed record GapResume(
            AssistantTurnOutcome? Outcome,
            IReadOnlyList<string>? DraftTargets,
            int? LocationId = null,
            string? LocationName = null,
            string? MergedUserMessage = null
        );

        private sealed record CombinedCreateResumeContext(
            string SourceUserMessage,
            AssistantOfferPathTermsState PriorTerms
        );

        /// <summary>
        /// Stored create-flow terms state for a resume turn: the source ask
        /// plus stored terms merged with the answer message. Covers offer-path
        /// terms/location gaps and combined-create terms gaps. Null on fresh
        /// turns.
        /// </summary>
        private static CombinedCreateResumeContext? TryGetStoredCreateResumeContext(
            AssistantConversation conversation,
            string userMessage
        )
        {
            var gapState = AssistantGapTurn.Parse(conversation.DraftInterviewJson);
            if (gapState is null
                || gapState.Kind
                    is not (AssistantGapTurn.KindOfferTerms
                        or AssistantGapTurn.KindLocation)
                || !string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                )
                && !string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.CreateCampaignWithOffer,
                    StringComparison.Ordinal
                ))
            {
                return null;
            }

            var prior = AssistantOfferPathTerms.FromJson(gapState.OfferTermsJson)
                ?? AssistantOfferPathTerms.Parse(gapState.SourceUserMessage);
            return new CombinedCreateResumeContext(
                gapState.SourceUserMessage,
                AssistantOfferPathTerms.Merge(prior, userMessage)
            );
        }

        private sealed record LocationFinish(
            AssistantTurnOutcome? Outcome,
            int? LocationId,
            string? LocationName
        );

        private IReadOnlyList<AssistantGapLocation> ToGapLocations(
            IReadOnlyList<OwnedLocationRow> ownedLocations
        )
            => ownedLocations
                .Select(location => new AssistantGapLocation(location.Id, location.Name))
                .ToList();

        private async Task<AssistantCampaignDraftBindOutcome> BindCampaignAsync(
            string userMessage,
            int locationId,
            string locationName,
            IReadOnlyList<int> ownedLocationIds,
            CancellationToken cancellationToken,
            AssistantCampaignDraftBindChoice? choice = null,
            bool ignoreOffers = false
        )
        {
            IReadOnlyList<AssistantCatalogOfferRef> locationOffers;
            IReadOnlyList<AssistantCatalogOfferRef> otherOffers;
            if (ignoreOffers)
            {
                locationOffers = [];
                otherOffers = [];
            }
            else
            {
                (locationOffers, otherOffers) = await LoadBindOffersAsync(
                    locationId,
                    ownedLocationIds,
                    cancellationToken
                );
            }
            var templates = CampaignTemplateSeed.All
                .Select(template => new AssistantCampaignTemplateRef(
                    template.Id,
                    template.Title
                ))
                .ToList();
            return AssistantCampaignDraftBind.Resolve(
                userMessage,
                locationName,
                locationOffers,
                templates,
                choice,
                otherOffers
            );
        }

        private async Task<(
            IReadOnlyList<AssistantCatalogOfferRef> LocationOffers,
            IReadOnlyList<AssistantCatalogOfferRef> OtherOffers
        )> LoadBindOffersAsync(
            int locationId,
            IReadOnlyList<int> ownedLocationIds,
            CancellationToken cancellationToken,
            bool includeStoredDraft = false
        )
        {
            var today = CatalogOfferStatus.VenueLocalToday(DateTime.UtcNow, 0);
            var ids = ownedLocationIds.Count == 0
                ? (IReadOnlyList<int>)[locationId]
                : ownedLocationIds;
            var rows = await _context.CatalogOffers
                .AsNoTracking()
                .Where(offer => ids.Contains(offer.RestaurantLocationId))
                .ToListAsync(cancellationToken);

            AssistantCatalogOfferRef ToRef(CatalogOffer offer)
            {
                var attachable = includeStoredDraft
                    ? CatalogOfferStatus.IsAttachable(
                        offer.Status,
                        offer.Validity,
                        offer.CustomExpiryDate,
                        today
                    )
                    : CatalogOfferStatus.IsAttachableActive(
                        offer.Status,
                        offer.Validity,
                        offer.CustomExpiryDate,
                        today
                    );
                return new(
                    offer.Id,
                    offer.Title,
                    offer.Status,
                    attachable,
                    offer.DiscountPercentage,
                    offer.DiscountAmount,
                    offer.FreeItemText
                );
            }

            var locationOffers = rows
                .Where(offer => offer.RestaurantLocationId == locationId)
                .Select(ToRef)
                .ToList();
            var otherOffers = rows
                .Where(offer => offer.RestaurantLocationId != locationId)
                .Select(ToRef)
                .ToList();
            return (locationOffers, otherOffers);
        }

        private async Task<IReadOnlyList<AssistantCatalogOfferRef>> LoadAttachableOffersAsync(
            int locationId,
            IReadOnlyList<int> ownedLocationIds,
            CancellationToken cancellationToken
        )
        {
            var (locationOffers, _) = await LoadBindOffersAsync(
                locationId,
                ownedLocationIds,
                cancellationToken,
                includeStoredDraft: true
            );
            return locationOffers;
        }

        private async Task<AssistantTurnOutcome?> TryFinishBindOutcomeAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            AssistantCampaignDraftBindOutcome bind,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            string assistantTask = AssistantTask.CreateCampaignDraft
        )
        {
            switch (bind)
            {
                case AssistantCampaignDraftBindOutcome.Gap gap:
                    return await FinishGapTurnAsync(
                        conversation,
                        AssistantGapTurn.CreateBindKind(
                            gap.Kind,
                            gap.Options,
                            sourceUserMessage,
                            assistantTask
                        ),
                        gap.Body,
                        replaceFailure,
                        cancellationToken
                    );
                case AssistantCampaignDraftBindOutcome.UnevaluableAudience unevaluable:
                    conversation.DraftInterviewJson = null;
                    conversation.LastCompareLocationIdsJson = null;
                    return await PersistAssistantAsync(
                        conversation,
                        GroundedMessage(
                            DateTime.UtcNow,
                            AssistantCampaignDraftPersistCopy.FailureTitle,
                            unevaluable.Body,
                            []
                        ),
                        replaceFailure,
                        cancellationToken
                    );
                default:
                    return null;
            }
        }

        private static AssistantMessage PersistTurnMessage(
            DateTime createdAt,
            CreateCampaignDraftTurn persist
        )
            => persist.Class == AssistantMessageClass.Gap
                ? GapMessage(createdAt, persist.Body)
                : GroundedMessage(
                    createdAt,
                    persist.Title,
                    persist.Body,
                    persist.Actions
                );

        private static int CreatePersistLocationId(
            int? boundLocationId,
            AssistantConversation conversation
        )
            => boundLocationId
                ?? conversation.OwnedLocationId
                ?? throw new InvalidOperationException(
                    "Create persist needs one Owned location."
                );

        private static bool ShouldUpdateScopeOnCreateBind(AssistantConversation conversation)
            => !AssistantAnalysisScope.IsAll(conversation);

        private AssistantLocationGapOutcome ResolveCreateLocation(
            string userMessage,
            AssistantConversation conversation,
            string analysisScopeLocationName,
            IReadOnlyList<OwnedLocationRow> ownedLocations,
            bool uniqueNameIsChoice = false,
            string? assistantTask = null
        )
            => AssistantCreateLocationGap.Resolve(
                userMessage,
                conversation.OwnedLocationId,
                string.IsNullOrWhiteSpace(conversation.OwnedLocationName)
                    ? analysisScopeLocationName
                    : conversation.OwnedLocationName,
                ToGapLocations(ownedLocations),
                uniqueNameIsChoice,
                AssistantGapTurn.LocationDraftNoun(assistantTask)
            );

        private static string? CreateTargetForTask(string? assistantTask)
            => assistantTask switch
            {
                AssistantTask.CreateCampaignDraft => AssistantCreateTargets.Campaign,
                AssistantTask.CreateCampaignWithOffer => AssistantCreateTargets.Campaign,
                AssistantTask.OfferPath => AssistantCreateTargets.Offer,
                AssistantTask.RecoveryPath => AssistantCreateTargets.Recovery,
                _ => null,
            };

        private async Task<GapResume> TryResumeGapAsync(
            AssistantConversation conversation,
            AssistantGapState gapState,
            string userMessage,
            string analysisScopeLocationName,
            IReadOnlyList<OwnedLocationRow> ownedLocations,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            if (AssistantGapTurn.IsAdvisoryGap(gapState))
            {
                return await ResumeAdvisoryGapAsync(
                    conversation,
                    gapState,
                    userMessage,
                    replaceFailure,
                    cancellationToken
                );
            }

            var stayOnOfferPath = AssistantGapTurn.IsOfferPathGap(gapState);
            var detected = AssistantCreateTargets.Detect(userMessage);
            if (detected.Count > 1 && !stayOnOfferPath)
            {
                return new GapResume(
                    await FinishGapTurnAsync(
                        conversation,
                        AssistantGapTurn.CreateTarget(
                            detected,
                            userMessage,
                            AssistantTaskClassification.ForCreateTargetGap(
                                detected,
                                userMessage
                            )
                        ),
                        AssistantGapTurn.CreateTargetBody(detected),
                        replaceFailure,
                        cancellationToken
                    ),
                    null
                );
            }

            if (gapState.Kind == AssistantGapTurn.KindFeedback)
            {
                if (detected.Count == 1
                    && detected[0] != AssistantCreateTargets.Recovery)
                {
                    conversation.DraftInterviewJson = null;
                    return new GapResume(null, detected);
                }

                if (AssistantCampaignDraftBind.ResolveNamedChoice(
                        gapState.Options,
                        userMessage
                    ) is not null)
                {
                    return new GapResume(null, null);
                }

                return new GapResume(
                    await FinishGapTurnAsync(
                        conversation,
                        gapState,
                        AssistantGapAsk.ExplainBind(
                            AssistantGapTurn.KindFeedback,
                            gapState.Options
                        ),
                        replaceFailure,
                        cancellationToken
                    ),
                    null
                );
            }

            var gapTarget = CreateTargetForTask(gapState.AssistantTask);
            if (detected.Count == 1
                && gapTarget is not null
                && !string.Equals(detected[0], gapTarget, StringComparison.Ordinal)
                && !stayOnOfferPath)
            {
                if (detected[0] == AssistantCreateTargets.Offer)
                {
                    // The new ask routes through the live answer like any
                    // fresh offer create; the model drives from there.
                    conversation.DraftInterviewJson = null;
                    return new GapResume(null, detected);
                }

                conversation.DraftInterviewJson = null;
                return new GapResume(null, detected);
            }

            if (detected.Count == 1
                && gapState.Kind == AssistantGapTurn.KindCreateTarget
                && !gapState.Options.Contains(detected[0], StringComparer.Ordinal)
                && detected[0] != AssistantCreateTargets.Campaign
                && detected[0] != AssistantCreateTargets.Offer)
            {
                conversation.DraftInterviewJson = null;
                return new GapResume(null, detected);
            }

            if (gapState.Kind == AssistantGapTurn.KindCreateTarget)
            {
                var resolved = AssistantCreateTargets.Resolve(
                    gapState.Options,
                    userMessage
                );
                if (resolved is null)
                {
                    return new GapResume(
                        await FinishGapTurnAsync(
                            conversation,
                            gapState,
                            AssistantGapAsk.ExplainBind(
                                AssistantGapTurn.KindCreateTarget,
                                gapState.Options
                            ),
                            replaceFailure,
                            cancellationToken
                        ),
                        null
                    );
                }

                if (resolved == AssistantCreateTargets.Campaign)
                {
                    var locationOutcome = ResolveCreateLocation(
                        gapState.SourceUserMessage,
                        conversation,
                        analysisScopeLocationName,
                        ownedLocations,
                        uniqueNameIsChoice: false,
                        AssistantTask.CreateCampaignDraft
                    );
                    var finished = await TryFinishLocationOutcomeAsync(
                        conversation,
                        gapState.SourceUserMessage,
                        analysisScopeLocationName,
                        locationOutcome,
                        replaceFailure,
                        cancellationToken,
                        AssistantTask.CreateCampaignDraft
                    );
                    if (finished.Outcome is not null)
                    {
                        return new GapResume(finished.Outcome, null);
                    }

                    conversation.DraftInterviewJson = null;
                    var persist = await PersistCreateAndStoreAsync(
                        conversation,
                        gapState.SourceUserMessage,
                        CreatePersistLocationId(finished.LocationId, conversation),
                        finished.LocationName ?? analysisScopeLocationName,
                        updateScope: false,
                        replaceFailure,
                        cancellationToken,
                        ownedLocations.Select(location => location.Id).ToList()
                    );
                    return new GapResume(persist, null);
                }

                if (resolved == AssistantCreateTargets.Offer)
                {
                    // The create resumes through the live answer: the model
                    // sees the whole thread and drives the flow from there.
                    conversation.DraftInterviewJson = null;
                    return new GapResume(null, [resolved]);
                }

                conversation.DraftInterviewJson = null;
                return new GapResume(null, [resolved]);
            }

            if (AssistantGapTurn.IsBindKind(gapState.Kind))
            {
                var choice = AssistantCampaignDraftBind.ResolveNamedChoice(
                    gapState.Options,
                    userMessage
                );
                if (choice is null)
                {
                    return new GapResume(
                        await FinishGapTurnAsync(
                            conversation,
                            gapState,
                            AssistantGapTurn.RepeatBindBody(gapState),
                            replaceFailure,
                            cancellationToken
                        ),
                        null
                    );
                }

                if (string.Equals(
                        gapState.AssistantTask,
                        AssistantTask.CreateCampaignWithOffer,
                        StringComparison.Ordinal
                    ))
                {
                    return await ResumeCombinedCreateAsync(
                        conversation,
                        gapState.SourceUserMessage,
                        userMessage,
                        analysisScopeLocationName,
                        ownedLocations,
                        AssistantCampaignDraftBindChoice.FromGapKind(
                            gapState.Kind,
                            choice
                        ),
                        updateScope: false,
                        replaceFailure,
                        cancellationToken
                    );
                }

                var boundLocation = ResolveCreateLocation(
                    gapState.SourceUserMessage,
                    conversation,
                    analysisScopeLocationName,
                    ownedLocations
                );
                var boundFinish = await TryFinishLocationOutcomeAsync(
                    conversation,
                    gapState.SourceUserMessage,
                    analysisScopeLocationName,
                    boundLocation,
                    replaceFailure,
                    cancellationToken
                );
                if (boundFinish.Outcome is not null)
                {
                    return new GapResume(boundFinish.Outcome, null);
                }

                conversation.DraftInterviewJson = null;
                var resumed = await PersistCreateAndStoreAsync(
                    conversation,
                    gapState.SourceUserMessage,
                    CreatePersistLocationId(boundFinish.LocationId, conversation),
                    boundFinish.LocationName ?? analysisScopeLocationName,
                    updateScope: false,
                    replaceFailure,
                    cancellationToken,
                    ownedLocations.Select(location => location.Id).ToList(),
                    choice: AssistantCampaignDraftBindChoice.FromGapKind(
                        gapState.Kind,
                        choice
                    )
                );
                return new GapResume(resumed, null);
            }

            if (gapState.Kind == AssistantGapTurn.KindCampaignTitle)
            {
                var choice = AssistantCampaignDraftBind.ResolveNamedChoice(
                    gapState.Options,
                    userMessage
                );
                if (choice is null)
                {
                    return new GapResume(
                        await FinishGapTurnAsync(
                            conversation,
                            gapState,
                            AssistantGapTurn.RepeatBindBody(gapState),
                            replaceFailure,
                            cancellationToken
                        ),
                        null
                    );
                }

                return await ResumeCombinedCreateAsync(
                    conversation,
                    gapState.SourceUserMessage,
                    userMessage,
                    analysisScopeLocationName,
                    ownedLocations,
                    new AssistantCampaignDraftBindChoice(CampaignTitle: choice),
                    updateScope: false,
                    replaceFailure,
                    cancellationToken
                );
            }

            if (gapState.Kind == AssistantGapTurn.KindOfferTerms)
            {
                var prior = AssistantOfferPathTerms.FromJson(gapState.OfferTermsJson)
                    ?? AssistantOfferPathTerms.Parse(gapState.SourceUserMessage);
                var merged = AssistantOfferPathTerms.Merge(prior, userMessage);
                if (!AssistantOfferPathTerms.IsComplete(merged))
                {
                    if (AssistantOfferPathTerms.HasNewlyFilledRule(prior, merged))
                    {
                        return new GapResume(
                            await FinishGapTurnAsync(
                                conversation,
                                AssistantGapTurn.CreateCombinedOfferTerms(
                                    gapState.SourceUserMessage,
                                    merged,
                                    gapState.AssistantTask
                                ),
                                AssistantGapAsk.ForOfferTerms(merged),
                                replaceFailure,
                                cancellationToken
                            ),
                            null
                        );
                    }

                    var lastAsk = AssistantGapAsk.ForOfferTerms(prior);
                    if (AssistantGapAsk.LooksLikeConfusedPhrase(userMessage)
                        || AssistantGapAsk.LooksLikeQuestionNamingAsk(userMessage, lastAsk)
                        || !AssistantAskIntent.HasReplacingRetrieveAsk(userMessage)
                            && !AssistantAskIntent.IsHelpCentreAsk(userMessage)
                            && !AssistantSendScheduleAsk.LooksLikeSendOrSchedule(userMessage))
                    {
                        return new GapResume(
                            await FinishGapTurnAsync(
                                conversation,
                                AssistantGapTurn.CreateCombinedOfferTerms(
                                    gapState.SourceUserMessage,
                                    merged,
                                    gapState.AssistantTask
                                ),
                                AssistantGapAsk.ExplainOfferTerms(merged),
                                replaceFailure,
                                cancellationToken
                            ),
                            null
                        );
                    }

                    return new GapResume(null, null);
                }

                if (string.Equals(
                        gapState.AssistantTask,
                        AssistantTask.CreateCampaignWithOffer,
                        StringComparison.Ordinal
                    ))
                {
                    return await ResumeCombinedCreateAsync(
                        conversation,
                        gapState.SourceUserMessage,
                        userMessage,
                        analysisScopeLocationName,
                        ownedLocations,
                        AssistantCampaignDraftBindChoice.Empty,
                        updateScope: false,
                        replaceFailure,
                        cancellationToken,
                        priorTerms: merged
                    );
                }

                var resumedLocationOutcome = ResolveCreateLocation(
                    userMessage,
                    conversation,
                    analysisScopeLocationName,
                    ownedLocations,
                    uniqueNameIsChoice: true,
                    gapState.AssistantTask
                );
                if (resumedLocationOutcome
                    is not AssistantLocationGapOutcome.Unique)
                {
                    resumedLocationOutcome = ResolveCreateLocation(
                        gapState.SourceUserMessage,
                        conversation,
                        analysisScopeLocationName,
                        ownedLocations,
                        uniqueNameIsChoice: false,
                        gapState.AssistantTask
                    );
                }

                var resumedLocation = await TryFinishLocationOutcomeAsync(
                    conversation,
                    gapState.SourceUserMessage,
                    analysisScopeLocationName,
                    resumedLocationOutcome,
                    replaceFailure,
                    cancellationToken,
                    gapState.AssistantTask,
                    merged
                );
                if (resumedLocation.Outcome is not null)
                {
                    return new GapResume(resumedLocation.Outcome, null);
                }

                return new GapResume(
                    null,
                    null,
                    resumedLocation.LocationId,
                    resumedLocation.LocationName
                );
            }

            if (AssistantAskIntent.HasExplicitRetrieveAsk(userMessage)
                && ResolveCreateLocation(
                    userMessage,
                    conversation,
                    analysisScopeLocationName,
                    ownedLocations
                ) is AssistantLocationGapOutcome.Unnamed
                && detected.Count == 0)
            {
                return new GapResume(null, null);
            }

            var answerOutcome = ResolveCreateLocation(
                userMessage,
                conversation,
                analysisScopeLocationName,
                ownedLocations,
                uniqueNameIsChoice: true,
                gapState.AssistantTask
            );
            if (answerOutcome is AssistantLocationGapOutcome.Unnamed)
            {
                if (!AssistantGapTurn.LooksLikeContinueAnswer(userMessage))
                {
                    return new GapResume(
                        await FinishGapTurnAsync(
                            conversation,
                            gapState,
                            AssistantGapAsk.ExplainLocation(
                                AssistantGapTurn.LocationDraftNoun(gapState.AssistantTask),
                                gapState.Options
                            ),
                            replaceFailure,
                            cancellationToken
                        ),
                        null
                    );
                }

                answerOutcome = ResolveCreateLocation(
                    gapState.SourceUserMessage,
                    conversation,
                    analysisScopeLocationName,
                    ownedLocations,
                    uniqueNameIsChoice: false,
                    gapState.AssistantTask
                );
            }

            if (!string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.CreateCampaignDraft,
                    StringComparison.Ordinal
                )
                && !string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.CreateCampaignWithOffer,
                    StringComparison.Ordinal
                )
                && !string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                )
                && answerOutcome is AssistantLocationGapOutcome.Unique)
            {
                conversation.DraftInterviewJson = null;
                return new GapResume(null, null);
            }

            var rememberedTerms = AssistantOfferPathTerms.FromJson(gapState.OfferTermsJson)
                ?? (string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                )
                    ? AssistantOfferPathTerms.Parse(gapState.SourceUserMessage)
                    : null);
            if (string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                ))
            {
                rememberedTerms = AssistantOfferPathTerms.Merge(rememberedTerms, userMessage);
            }
            else if (string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.CreateCampaignWithOffer,
                    StringComparison.Ordinal
                ))
            {
                rememberedTerms = AssistantOfferPathTerms.Merge(rememberedTerms, userMessage);
            }

            var locationFinish = await TryFinishLocationOutcomeAsync(
                conversation,
                gapState.SourceUserMessage,
                analysisScopeLocationName,
                answerOutcome,
                replaceFailure,
                cancellationToken,
                gapState.AssistantTask,
                rememberedTerms
            );
            if (locationFinish.Outcome is not null)
            {
                return new GapResume(locationFinish.Outcome, null);
            }

            if (string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.CreateCampaignWithOffer,
                    StringComparison.Ordinal
                ))
            {
                return await ResumeCombinedCreateAsync(
                    conversation,
                    gapState.SourceUserMessage,
                    userMessage,
                    analysisScopeLocationName,
                    ownedLocations,
                    AssistantCampaignDraftBindChoice.Empty,
                    updateScope: ShouldUpdateScopeOnCreateBind(conversation),
                    replaceFailure,
                    cancellationToken,
                    rememberedTerms
                );
            }

            if (string.Equals(
                    gapState.AssistantTask,
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                )
                && rememberedTerms is not null)
            {
                // Offer-path resumes fall through to the live answer: the
                // model re-extracts every term from the whole thread and the
                // post-model gate asks with model text. The location binding
                // resolved above is carried through.
                return new GapResume(
                    null,
                    null,
                    locationFinish.LocationId,
                    locationFinish.LocationName
                );
            }

            conversation.DraftInterviewJson = null;
            var persisted = await PersistCreateAndStoreAsync(
                conversation,
                gapState.SourceUserMessage,
                CreatePersistLocationId(locationFinish.LocationId, conversation),
                locationFinish.LocationName ?? analysisScopeLocationName,
                updateScope: ShouldUpdateScopeOnCreateBind(conversation),
                replaceFailure,
                cancellationToken,
                ownedLocations.Select(location => location.Id).ToList(),
                gapState.AssistantTask,
                rememberedTerms
            );
            return new GapResume(persisted, null);
        }

        private async Task<GapResume> ResumeCombinedCreateAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            string userMessage,
            string analysisScopeLocationName,
            IReadOnlyList<OwnedLocationRow> ownedLocations,
            AssistantCampaignDraftBindChoice choice,
            bool updateScope,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            AssistantOfferPathTermsState? priorTerms = null
        )
        {
            var ownedLocationIds = ownedLocations
                .Select(location => location.Id)
                .ToList();
            var gapJsonState = AssistantGapTurn.Parse(conversation.DraftInterviewJson);
            var storedTerms = priorTerms
                ?? AssistantOfferPathTerms.FromJson(gapJsonState?.OfferTermsJson)
                ?? AssistantOfferPathTerms.Parse(sourceUserMessage);
            var mergedTerms = AssistantOfferPathTerms.Merge(storedTerms, userMessage);
            if (!AssistantOfferPathTerms.IsComplete(mergedTerms))
            {
                // Incomplete terms on a resume turn fall through to the live
                // answer: the model re-extracts from the whole thread and the
                // post-model gate asks with model text.
                var gateLocationOutcome = ResolveCreateLocation(
                    sourceUserMessage,
                    conversation,
                    analysisScopeLocationName,
                    ownedLocations,
                    uniqueNameIsChoice: false,
                    AssistantTask.CreateCampaignWithOffer
                );
                var gateLocationFinish = await TryFinishLocationOutcomeAsync(
                    conversation,
                    sourceUserMessage,
                    analysisScopeLocationName,
                    gateLocationOutcome,
                    replaceFailure,
                    cancellationToken,
                    AssistantTask.CreateCampaignWithOffer,
                    mergedTerms
                );
                if (gateLocationFinish.Outcome is not null)
                {
                    return new GapResume(gateLocationFinish.Outcome, null);
                }

                return new GapResume(
                    null,
                    null,
                    gateLocationFinish.LocationId,
                    gateLocationFinish.LocationName
                );
            }

            var locationOutcome = ResolveCreateLocation(
                sourceUserMessage,
                conversation,
                analysisScopeLocationName,
                ownedLocations,
                uniqueNameIsChoice: false,
                AssistantTask.CreateCampaignWithOffer
            );
            var locationFinish = await TryFinishLocationOutcomeAsync(
                conversation,
                sourceUserMessage,
                analysisScopeLocationName,
                locationOutcome,
                replaceFailure,
                cancellationToken,
                AssistantTask.CreateCampaignWithOffer,
                priorTerms
            );
            if (locationFinish.Outcome is not null)
            {
                return new GapResume(locationFinish.Outcome, null);
            }

            var locationId = CreatePersistLocationId(locationFinish.LocationId, conversation);
            var locationName = locationFinish.LocationName ?? analysisScopeLocationName;
            if (updateScope && locationId != conversation.OwnedLocationId)
            {
                var scope = AssistantAnalysisScope.FromConversation(conversation);
                scope.OwnedLocationId = locationId;
                AssistantAnalysisScope.CopyToConversation(
                    conversation,
                    scope,
                    locationName
                );
            }

            var prePersistGap = await TryFinishCombinedCreatePrePersistGapsAsync(
                conversation,
                sourceUserMessage,
                locationId,
                ownedLocationIds,
                replaceFailure,
                cancellationToken,
                choice,
                priorTerms
            );
            if (prePersistGap is not null)
            {
                return new GapResume(prePersistGap, null);
            }

            var bind = await BindCampaignAsync(
                sourceUserMessage,
                locationId,
                locationName,
                ownedLocationIds,
                cancellationToken,
                choice,
                ignoreOffers: true
            );
            var bindAbort = await TryFinishBindOutcomeAsync(
                conversation,
                sourceUserMessage,
                bind,
                replaceFailure,
                cancellationToken,
                AssistantTask.CreateCampaignWithOffer
            );
            if (bindAbort is not null)
            {
                return new GapResume(bindAbort, null);
            }

            if (bind is not AssistantCampaignDraftBindOutcome.Bound bound)
            {
                throw new InvalidOperationException(
                    "Combined create resume expected bound Campaign fields."
                );
            }

            var persist = await PersistCreateCampaignWithOfferAsync(
                conversation,
                sourceUserMessage,
                locationId,
                locationName,
                ownedLocationIds,
                cancellationToken,
                preparedBind: bound,
                choice: choice,
                priorTerms: priorTerms
            );
            conversation.DraftInterviewJson = persist.GapState is null
                ? null
                : AssistantGapTurn.Serialize(persist.GapState);
            conversation.LastCompareLocationIdsJson = null;
            if (persist.CreatedCampaignId is int createdCampaignId)
            {
                conversation.CreatedCampaignId = createdCampaignId;
            }
            if (persist.CreatedOfferId is int createdOfferId)
            {
                conversation.CreatedOfferId = createdOfferId;
            }

            var assistantMessage = persist.Class == AssistantMessageClass.Gap
                ? GapMessage(DateTime.UtcNow, persist.Body)
                : GroundedMessage(
                    DateTime.UtcNow,
                    persist.Title,
                    persist.Body,
                    persist.Actions
                );
            var turn = await PersistAssistantAsync(
                conversation,
                assistantMessage,
                replaceFailure,
                cancellationToken,
                liveAnswerAlreadyCompleted: true
            );
            return new GapResume(turn, null);
        }

        private async Task<LocationFinish> TryFinishLocationOutcomeAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            string analysisScopeLocationName,
            AssistantLocationGapOutcome locationOutcome,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            string assistantTask = AssistantTask.CreateCampaignDraft,
            AssistantOfferPathTermsState? offerTerms = null
        )
        {
            switch (locationOutcome)
            {
                case AssistantLocationGapOutcome.Gap gap:
                    return new LocationFinish(
                        await FinishGapTurnAsync(
                            conversation,
                            AssistantGapTurn.CreateLocation(
                                gap.Kind,
                                gap.Options,
                                sourceUserMessage,
                                assistantTask,
                                offerTerms is null
                                    ? null
                                    : AssistantOfferPathTerms.Serialize(offerTerms)
                            ),
                            gap.Body,
                            replaceFailure,
                            cancellationToken
                        ),
                        null,
                        null
                    );
                case AssistantLocationGapOutcome.Refusal refusal:
                    conversation.DraftInterviewJson = null;
                    conversation.LastCompareLocationIdsJson = null;
                    return new LocationFinish(
                        await PersistAssistantAsync(
                            conversation,
                            RefusalMessage(DateTime.UtcNow, refusal.Body),
                            replaceFailure,
                            cancellationToken
                        ),
                        null,
                        null
                    );
                case AssistantLocationGapOutcome.Unique unique:
                    return new LocationFinish(null, unique.LocationId, unique.LocationName);
                default:
                    return new LocationFinish(
                        null,
                        conversation.OwnedLocationId,
                        string.IsNullOrWhiteSpace(conversation.OwnedLocationName)
                            ? analysisScopeLocationName
                            : conversation.OwnedLocationName
                    );
            }
        }

        private async Task<AssistantTurnOutcome> PersistCreateAndStoreAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            int locationId,
            string locationName,
            bool updateScope,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            IReadOnlyList<int> ownedLocationIds,
            string assistantTask = AssistantTask.CreateCampaignDraft,
            AssistantOfferPathTermsState? offerTerms = null,
            AssistantCampaignDraftBindChoice? choice = null
        )
        {
            if (updateScope && locationId != conversation.OwnedLocationId)
            {
                var scope = AssistantAnalysisScope.FromConversation(conversation);
                scope.OwnedLocationId = locationId;
                AssistantAnalysisScope.CopyToConversation(
                    conversation,
                    scope,
                    locationName
                );
            }

            conversation.DraftInterviewJson = null;
            conversation.LastCompareLocationIdsJson = null;
            if (string.Equals(
                    assistantTask,
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                ))
            {
                var offerPersist = await PersistCreateOfferDraftAsync(
                    conversation,
                    sourceUserMessage,
                    locationId,
                    locationName,
                    offerTerms,
                    cancellationToken
                );
                if (offerPersist.CreatedOfferId is int createdOfferId)
                {
                    conversation.CreatedOfferId = createdOfferId;
                }

                return await PersistAssistantAsync(
                    conversation,
                    GroundedMessage(
                        DateTime.UtcNow,
                        offerPersist.Title,
                        offerPersist.Body,
                        offerPersist.Actions
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            var persist = await PersistCreateCampaignDraftAsync(
                conversation,
                sourceUserMessage,
                locationId,
                locationName,
                ownedLocationIds,
                cancellationToken,
                choice
            );
            conversation.DraftInterviewJson = persist.GapState is null
                ? null
                : AssistantGapTurn.Serialize(persist.GapState);
            conversation.LastCompareLocationIdsJson = null;
            if (persist.CreatedCampaignId is int createdCampaignId)
            {
                conversation.CreatedCampaignId = createdCampaignId;
            }

            return await PersistAssistantAsync(
                conversation,
                PersistTurnMessage(DateTime.UtcNow, persist),
                replaceFailure,
                cancellationToken
            );
        }

        private async Task<AssistantTurnOutcome> FinishGapTurnAsync(
            AssistantConversation conversation,
            AssistantGapState state,
            string body,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            bool liveAnswerAlreadyCompleted = false,
            AssistantTurnBilling? turnBilling = null
        )
        {
            conversation.DraftInterviewJson = AssistantGapTurn.Serialize(state);
            conversation.LastCompareLocationIdsJson = null;
            return await PersistAssistantAsync(
                conversation,
                GapMessage(DateTime.UtcNow, body),
                replaceFailure,
                cancellationToken,
                liveAnswerAlreadyCompleted: liveAnswerAlreadyCompleted,
                turnBilling: turnBilling
            );
        }

        private sealed record AdvisoryGapResume(
            string? MergedMessage,
            string? ReaskBody
        );

        private async Task<GapResume> ResumeAdvisoryGapAsync(
            AssistantConversation conversation,
            AssistantGapState gapState,
            string userMessage,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            var advisoryResume = TryResumeAdvisoryGap(gapState, userMessage);
            if (advisoryResume.ReaskBody is not null)
            {
                return new GapResume(
                    await FinishGapTurnAsync(
                        conversation,
                        gapState,
                        advisoryResume.ReaskBody,
                        replaceFailure,
                        cancellationToken
                    ),
                    null
                );
            }

            conversation.DraftInterviewJson = null;
            return new GapResume(
                null,
                null,
                MergedUserMessage: advisoryResume.MergedMessage
            );
        }

        private static AdvisoryGapResume TryResumeAdvisoryGap(
            AssistantGapState gapState,
            string userMessage
        )
        {
            var choice = AssistantCampaignDraftBind.ResolveNamedChoice(
                gapState.Options,
                userMessage
            );
            if (choice is not null)
            {
                return new AdvisoryGapResume(
                    $"{gapState.SourceUserMessage}\n{choice}",
                    null
                );
            }

            if (AssistantGapAsk.LooksLikeNewCreateDuringGap(userMessage)
                || AssistantAskIntent.HasReplacingRetrieveAsk(userMessage)
                || AssistantTaskClassification.LooksLikeCreateTurn(userMessage)
                || AssistantTaskClassification.LooksLikeRecoveryPath(userMessage))
            {
                return new AdvisoryGapResume(null, null);
            }

            // Model-requested clarify (and other option-less advisory gaps) must
            // accept a free-form reply the same way a named choice resolves.
            if (gapState.Options.Count == 0
                && !string.IsNullOrWhiteSpace(userMessage))
            {
                return new AdvisoryGapResume(
                    $"{gapState.SourceUserMessage}\n{userMessage.Trim()}",
                    null
                );
            }

            return new AdvisoryGapResume(
                null,
                AssistantGapTurn.AdvisoryGapBody(gapState)
            );
        }

        private async Task<AssistantTurnOutcome?> TryFinishAdvisoryPreCheckAsync(
            AssistantConversation conversation,
            string userMessage,
            IReadOnlyList<OwnedLocationRow> ownedLocations,
            IReadOnlyList<int> compareIds,
            AssistantMessage? replaceFailure,
            int? boundCreateLocationId,
            string? idempotencyKey,
            CancellationToken cancellationToken
        )
        {
            if (_restaurantContextSnapshot is null)
            {
                return null;
            }

            var locationIds = compareIds.Count > 0
                ? compareIds
                : ownedLocations.Select(location => location.Id).ToList();
            var scope = BuildAdvisoryScope(locationIds, ownedLocations.Count);
            var chosenMetric = ExtractChosenMetricNote(userMessage);
            var snapshot = await _restaurantContextSnapshot.BuildAsync(
                conversation.OwnerUserId,
                scope,
                currentOverride: null,
                comparisonOverride: null,
                cancellationToken
            );
            var ownedIdStrings = ownedLocations
                .Select(location =>
                    location.Id.ToString(CultureInfo.InvariantCulture)
                )
                .ToList();
            var outcome = AssistantAdvisoryIntent.Evaluate(
                ownedIdStrings,
                userMessage,
                snapshot,
                _snapshotSettings,
                conversationTurnId: Guid.NewGuid()
                    .ToString("N", CultureInfo.InvariantCulture),
                chosenMetricNote: chosenMetric
            );

            return outcome switch
            {
                AdvisoryPreCheckOutcome.Gap gap => await FinishGapTurnAsync(
                    conversation,
                    AssistantGapTurn.CreateAdvisory(gap.Advisory, userMessage),
                    AssistantAdvisoryIntent.GapQuestionBody(gap.Advisory),
                    replaceFailure,
                    cancellationToken
                ),
                AdvisoryPreCheckOutcome.NoClearDriver noDriver =>
                    await PersistAssistantAsync(
                        conversation,
                        GroundedMessage(
                            DateTime.UtcNow,
                            "No clear driver",
                            noDriver.Body,
                            []
                        ),
                        replaceFailure,
                        cancellationToken,
                        liveAnswerAlreadyCompleted: true
                    ),
                AdvisoryPreCheckOutcome.PureProduct => null,
                AdvisoryPreCheckOutcome.Clear clear when _advisoryReason is not null
                    => await FinishAdvisoryClearAsync(
                        conversation,
                        userMessage,
                        clear,
                        replaceFailure,
                        boundCreateLocationId,
                        idempotencyKey,
                        cancellationToken
                    ),
                // Clear without Reason provider: keep retrieve + live path.
                _ => null,
            };
        }

        private async Task<AssistantTurnOutcome> FinishAdvisoryClearAsync(
            AssistantConversation conversation,
            string userMessage,
            AdvisoryPreCheckOutcome.Clear clear,
            AssistantMessage? replaceFailure,
            int? boundCreateLocationId,
            string? idempotencyKey,
            CancellationToken cancellationToken
        )
        {
            var billingGate = await TryBeginBilledLiveAnswerAsync(
                conversation,
                boundCreateLocationId,
                idempotencyKey,
                cancellationToken
            );
            if (billingGate.Error is not null)
            {
                return billingGate.Error;
            }

            var turnBilling = billingGate.Billing;
            AssistantAdvisoryReasonResult reasonResult;
            try
            {
                await TryPublishProgressAsync(
                    conversation.OwnerUserId,
                    conversation.Id,
                    AssistantTurnProgressSteps.Preparing,
                    cancellationToken
                );
                reasonResult = await _advisoryReason!.CompleteAsync(
                    new AssistantAdvisoryReasonInput(
                        userMessage,
                        clear.Snapshot,
                        BuildLiveAnswerHistory(conversation)
                    ),
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                conversation.LastCompareLocationIdsJson = null;
                await PersistAssistantAsync(
                    conversation,
                    FailureMessage(DateTime.UtcNow),
                    replaceFailure,
                    CancellationToken.None
                );
                throw;
            }

            if (reasonResult is not AssistantAdvisoryReasonResult.Succeeded succeeded)
            {
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    FailureMessage(DateTime.UtcNow),
                    replaceFailure,
                    cancellationToken,
                    liveAnswerAlreadyCompleted: true,
                    turnBilling: turnBilling
                );
            }

            turnBilling?.MarkLiveAnswerSucceeded();
            var validated = AssistantAdvisoryReasonValidate.Validate(
                succeeded.Output,
                clear.Snapshot,
                _logger
            );

            if (validated is AdvisoryReasonValidateResult.Clarify clarify)
            {
                _logger.LogInformation(
                    "AdvisoryReasonClarifyAfterClear conversationId={ConversationId}",
                    conversation.Id
                );
                return await FinishGapTurnAsync(
                    conversation,
                    AssistantGapTurn.CreateAdvisory(
                        clarify.Gap,
                        userMessage,
                        AssistantGapTurn.GapSourceModelRequested
                    ),
                    AssistantAdvisoryIntent.GapQuestionBody(clarify.Gap),
                    replaceFailure,
                    cancellationToken,
                    liveAnswerAlreadyCompleted: true,
                    turnBilling: turnBilling
                );
            }

            if (validated is AdvisoryReasonValidateResult.FallbackNoClearDriver)
            {
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    GroundedMessage(
                        DateTime.UtcNow,
                        "No clear driver",
                        AssistantAdvisoryReasonValidate.NoClearDriverBody,
                        []
                    ),
                    replaceFailure,
                    cancellationToken,
                    liveAnswerAlreadyCompleted: true,
                    turnBilling: turnBilling
                );
            }

            var valid = (AdvisoryReasonValidateResult.Valid)validated;
            _logger.LogInformation(
                "Advisory Reason evidence_used sections={EvidenceUsed} "
                + "conversationId={ConversationId}",
                string.Join(", ", valid.Output.EvidenceUsed),
                conversation.Id
            );
            conversation.LastCompareLocationIdsJson = null;
            return await PersistAssistantAsync(
                conversation,
                GroundedMessage(
                    DateTime.UtcNow,
                    "Advisory",
                    AssistantAdvisoryReasonValidate.RenderBody(valid.Output),
                    []
                ),
                replaceFailure,
                cancellationToken,
                liveAnswerAlreadyCompleted: true,
                turnBilling: turnBilling
            );
        }

        private static LocationScope BuildAdvisoryScope(
            IReadOnlyList<int> locationIds,
            int ownedLocationCount
        )
        {
            var ids = locationIds
                .Select(id => id.ToString(CultureInfo.InvariantCulture))
                .ToArray();
            if (ids.Length == 1)
            {
                return new SingleLocation(ids[0]);
            }

            if (ids.Length == 0)
            {
                return new AllOwnedLocations([]);
            }

            if (ids.Length < ownedLocationCount)
            {
                return new NamedSubset(ids);
            }

            return new AllOwnedLocations(ids);
        }

        private static string? ExtractChosenMetricNote(string userMessage)
        {
            var lower = userMessage.ToLowerInvariant();
            if (lower.Contains("covers", StringComparison.Ordinal))
            {
                return "covers";
            }

            if (lower.Contains("capture", StringComparison.Ordinal)
                || lower.Contains("funnel", StringComparison.Ordinal))
            {
                return "capture";
            }

            if (lower.Contains("sentiment", StringComparison.Ordinal)
                || lower.Contains("feedback score", StringComparison.Ordinal))
            {
                return "sentiment";
            }

            return null;
        }

        private sealed record ScopeAuthorizeResult(
            string LocationName,
            AssistantTurnOutcome? Error
        );

        private async Task<ScopeAuthorizeResult> TryAuthorizeScopeAsync(
            int ownerUserId,
            AssistantAnalysisScopeDto scope,
            CancellationToken cancellationToken
        )
        {
            if (AssistantAnalysisScope.IsAll(scope))
            {
                var owned = await LoadOwnedLocationsAsync(
                    null,
                    ownerUserId,
                    cancellationToken
                );
                if (owned.Count == 0)
                {
                    return new ScopeAuthorizeResult(
                        string.Empty,
                        new AssistantTurnOutcome.LocationDenied(
                            new OwnedLocationResult
                            {
                                Status = OwnedLocationResolveStatus.NotFound
                            }
                        )
                    );
                }

                return new ScopeAuthorizeResult(
                    AssistantAnalysisScope.AllLocationsChromeName,
                    null
                );
            }

            if (scope.OwnedLocationId is not int locationId || locationId <= 0)
            {
                return new ScopeAuthorizeResult(
                    string.Empty,
                    new AssistantTurnOutcome.Invalid("Owned location is required.")
                );
            }

            var locationResult = await _ownedLocation.ResolveAsync(
                ownerUserId,
                locationId
            );
            if (locationResult.Status != OwnedLocationResolveStatus.Found)
            {
                return new ScopeAuthorizeResult(
                    string.Empty,
                    new AssistantTurnOutcome.LocationDenied(locationResult)
                );
            }

            return new ScopeAuthorizeResult(
                locationResult.Location!.LocationName,
                null
            );
        }

        private async Task<IReadOnlyList<OwnedLocationRow>> LoadOwnedLocationsAsync(
            int? savedLocationId,
            int ownerUserId,
            CancellationToken cancellationToken
        )
        {
            RestaurantPermissionDecision access;
            if (savedLocationId is int savedId)
            {
                access = await _permissions.AuthorizeLocationForUserAsync(
                    ownerUserId,
                    OperatorAreaIds.AiAssistant,
                    PermissionLevel.View,
                    savedId
                );
            }
            else
            {
                access = await _permissions.AuthorizeUserAsync(
                    ownerUserId,
                    OperatorAreaIds.AiAssistant,
                    PermissionLevel.View
                );
            }

            if (access.Status != RestaurantPermissionStatus.Allowed)
            {
                return [];
            }

            var scopedIds = access.LocationIds.ToHashSet();
            var query = _context.RestaurantLocations
                .AsNoTracking()
                .Include(location => location.Restaurant)
                .Where(location => scopedIds.Contains(location.Id));
            if (access.RestaurantId > 0)
            {
                query = query.Where(location =>
                    location.RestaurantId == access.RestaurantId
                );
            }

            var rows = await query
                .OrderBy(location => location.LocationName)
                .ToListAsync(cancellationToken);

            return rows
                .Select(location => new OwnedLocationRow(
                    location.Id,
                    location.LocationName,
                    location.Address,
                    location.CaptureLocationStatus,
                    location.Restaurant!.AccountType
                ))
                .ToList();
        }

        private async Task<CompareAllRetrieve> RetrieveCompareAllAsync(
            int ownerUserId,
            IReadOnlyList<int> locationIds,
            IReadOnlyList<AssistantOwnedLocationRef> locationRefs,
            DateTime fromUtc,
            DateTime toUtc,
            bool includeCampaignCopy,
            CancellationToken cancellationToken
        )
        {
            var byId = locationRefs.ToDictionary(location => location.Id);
            var landed = new List<AssistantCompareLocationEvidence>();
            var failedNames = new List<string>();
            var notStartedNames = new List<string>();
            var startedAt = _clock.GetUtcNow();

            foreach (var locationId in locationIds.Distinct())
            {
                if (!byId.TryGetValue(locationId, out var locationRef))
                {
                    continue;
                }

                if (_clock.GetUtcNow() - startedAt >= AssistantCompareAll.RetrieveBudget)
                {
                    notStartedNames.Add(locationRef.Name);
                    continue;
                }

                var evidence = await RetrieveLocationDomainsAsync(
                    ownerUserId,
                    locationId,
                    fromUtc,
                    toUtc,
                    includeCampaignCopy,
                    cancellationToken
                );
                if (evidence is null)
                {
                    failedNames.Add(locationRef.Name);
                    continue;
                }

                landed.Add(
                    new AssistantCompareLocationEvidence(
                        locationId,
                        locationRef.Name,
                        locationRef.CaptureStatus,
                        AssistantCompareAll.Thin(evidence)
                    )
                );
            }

            return new CompareAllRetrieve(landed, failedNames, notStartedNames);
        }

        private async Task<AssistantFeedbackEvidence?> RetrieveRecoveryIdentityUnionAsync(
            int ownerUserId,
            IReadOnlyList<OwnedLocationRow> ownedLocations,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var rows = new List<AssistantFeedbackEvidenceRow>();
            foreach (var location in ownedLocations)
            {
                if (!await CanRetrieveAreaAsync(
                    ownerUserId,
                    OperatorAreaIds.Feedback,
                    location.Id
                ))
                {
                    continue;
                }

                var retrieved = await _feedbackRetrieve.RetrieveIdentityAsync(
                    location.Id,
                    location.Name,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                if (retrieved is AssistantFeedbackRetrieveResult.Failed)
                {
                    return null;
                }

                if (retrieved is AssistantFeedbackRetrieveResult.Ok ok)
                {
                    rows.AddRange(ok.Evidence.Rows);
                }
            }

            return new AssistantFeedbackEvidence(
                rows.Count,
                rows.Count,
                0,
                0,
                0,
                0,
                [],
                rows,
                [],
                [],
                []
            );
        }

        private async Task<AssistantOffersEvidence?> RetrieveOffersAtVenueAsync(
            int ownerUserId,
            int locationId,
            AssistantConversation conversation,
            CancellationToken cancellationToken
        )
        {
            if (!await CanRetrieveAreaAsync(
                ownerUserId,
                OperatorAreaIds.Offers,
                locationId
            ))
            {
                return AssistantOffersEvidence.Empty;
            }

            var scope = AssistantAnalysisScope.FromConversation(conversation);
            var window = AssistantReportingPeriodWindow.Resolve(
                scope.ReportingPeriod,
                DateTime.UtcNow
            );
            var retrieved = await _offersRetrieve.RetrieveAsync(
                locationId,
                window.FromUtc,
                window.ToUtc,
                cancellationToken
            );
            return retrieved is AssistantOffersRetrieveResult.Ok ok
                ? ok.Evidence
                : null;
        }

        private async Task<TurnRetrieve?> RetrieveForTurnAsync(
            int ownerUserId,
            IReadOnlyList<int> locationIds,
            int? savedLocationId,
            IReadOnlyList<AssistantOwnedLocationRef> locationRefs,
            DateTime fromUtc,
            DateTime toUtc,
            bool includeCampaignCopy,
            CancellationToken cancellationToken
        )
        {
            var byId = locationRefs.ToDictionary(location => location.Id);
            var compareRows = new List<AssistantCompareLocationEvidence>();
            AssistantRetrievedEvidence? savedEvidence = null;

            foreach (var locationId in locationIds.Distinct())
            {
                var evidence = await RetrieveLocationDomainsAsync(
                    ownerUserId,
                    locationId,
                    fromUtc,
                    toUtc,
                    includeCampaignCopy,
                    cancellationToken
                );
                if (evidence is null)
                {
                    return null;
                }

                if (locationId == savedLocationId)
                {
                    savedEvidence = evidence;
                }

                if (!byId.TryGetValue(locationId, out var locationRef))
                {
                    continue;
                }

                compareRows.Add(
                    new AssistantCompareLocationEvidence(
                        locationId,
                        locationRef.Name,
                        locationRef.CaptureStatus,
                        evidence
                    )
                );
            }

            if (savedEvidence is null)
            {
                if (savedLocationId is not int savedId)
                {
                    savedEvidence = EmptyEvidence;
                }
                else
                {
                    savedEvidence = await RetrieveLocationDomainsAsync(
                        ownerUserId,
                        savedId,
                        fromUtc,
                        toUtc,
                        includeCampaignCopy,
                        cancellationToken
                    );
                    if (savedEvidence is null)
                    {
                        return null;
                    }
                }
            }

            return new TurnRetrieve(savedEvidence, compareRows);
        }

        private async Task<AssistantRetrievedEvidence?> RetrieveLocationDomainsAsync(
            int ownerUserId,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            bool includeCampaignCopy,
            CancellationToken cancellationToken
        )
        {
            AssistantFeedbackEvidence feedback;
            if (await CanRetrieveAreaAsync(
                ownerUserId,
                OperatorAreaIds.Feedback,
                locationId
            ))
            {
                var feedbackRetrieve = await _feedbackRetrieve.RetrieveAsync(
                    locationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                if (feedbackRetrieve is AssistantFeedbackRetrieveResult.Failed)
                {
                    return null;
                }

                feedback = feedbackRetrieve is AssistantFeedbackRetrieveResult.Ok feedbackOk
                    ? feedbackOk.Evidence
                    : AssistantFeedbackEvidence.Empty;
            }
            else
            {
                feedback = AssistantFeedbackEvidence.Empty;
            }

            return await RetrieveSavedDomainsAsync(
                ownerUserId,
                locationId,
                fromUtc,
                toUtc,
                feedback,
                includeCampaignCopy,
                cancellationToken
            );
        }

        private async Task<AssistantRetrievedEvidence?> RetrieveSavedDomainsAsync(
            int ownerUserId,
            int savedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            AssistantFeedbackEvidence savedFeedback,
            bool includeCampaignCopy,
            CancellationToken cancellationToken
        )
        {
            var offersRetrieve = await RetrieveOffersIfAllowedAsync(
                ownerUserId,
                savedLocationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var campaignsRetrieve = await RetrieveCampaignsIfAllowedAsync(
                ownerUserId,
                savedLocationId,
                fromUtc,
                toUtc,
                includeCampaignCopy,
                cancellationToken
            );
            var captureRetrieve = await RetrieveCaptureIfAllowedAsync(
                ownerUserId,
                savedLocationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var homeRetrieve = await _homeRetrieve.RetrieveAsync(
                savedLocationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var guestsRetrieve = await RetrieveGuestsIfAllowedAsync(
                ownerUserId,
                savedLocationId,
                cancellationToken
            );

            if (offersRetrieve is AssistantOffersRetrieveResult.Failed
                || campaignsRetrieve is AssistantCampaignsRetrieveResult.Failed
                || captureRetrieve is AssistantCaptureRetrieveResult.Failed
                || homeRetrieve is AssistantHomeKpiRetrieveResult.Failed
                || guestsRetrieve is AssistantGuestsRetrieveResult.Failed)
            {
                return null;
            }

            return new AssistantRetrievedEvidence(
                savedFeedback,
                offersRetrieve is AssistantOffersRetrieveResult.Ok offersOk
                    ? offersOk.Evidence
                    : EmptyEvidence.Offers,
                campaignsRetrieve is AssistantCampaignsRetrieveResult.Ok campaignsOk
                    ? campaignsOk.Evidence
                    : EmptyEvidence.Campaigns,
                captureRetrieve is AssistantCaptureRetrieveResult.Ok captureOk
                    ? captureOk.Evidence
                    : EmptyEvidence.Capture,
                homeRetrieve is AssistantHomeKpiRetrieveResult.Ok homeOk
                    ? homeOk.Evidence
                    : EmptyEvidence.Home,
                guestsRetrieve is AssistantGuestsRetrieveResult.Ok guestsOk
                    ? guestsOk.Evidence
                    : EmptyEvidence.Guests
            );
        }

        private async Task<AssistantOffersRetrieveResult> RetrieveOffersIfAllowedAsync(
            int ownerUserId,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            if (!await CanRetrieveAreaAsync(
                ownerUserId,
                OperatorAreaIds.Offers,
                locationId
            ))
            {
                return new AssistantOffersRetrieveResult.Ok(
                    AssistantOffersEvidence.Empty
                );
            }

            return await _offersRetrieve.RetrieveAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
        }

        private async Task<AssistantCampaignsRetrieveResult> RetrieveCampaignsIfAllowedAsync(
            int ownerUserId,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            bool includeCampaignCopy,
            CancellationToken cancellationToken
        )
        {
            if (!await CanRetrieveAreaAsync(
                ownerUserId,
                OperatorAreaIds.Campaigns,
                locationId
            ))
            {
                return new AssistantCampaignsRetrieveResult.Ok(
                    AssistantCampaignsEvidence.Empty
                );
            }

            return await _campaignsRetrieve.RetrieveAsync(
                locationId,
                fromUtc,
                toUtc,
                includeCampaignCopy,
                cancellationToken
            );
        }

        private async Task<AssistantCaptureRetrieveResult> RetrieveCaptureIfAllowedAsync(
            int ownerUserId,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            if (!await CanRetrieveAreaAsync(
                ownerUserId,
                OperatorAreaIds.Capture,
                locationId
            ))
            {
                return new AssistantCaptureRetrieveResult.Ok(
                    AssistantCaptureEvidence.Empty
                );
            }

            return await _captureRetrieve.RetrieveAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
        }

        private async Task<AssistantGuestsRetrieveResult> RetrieveGuestsIfAllowedAsync(
            int ownerUserId,
            int locationId,
            CancellationToken cancellationToken
        )
        {
            if (!await CanRetrieveAreaAsync(
                ownerUserId,
                OperatorAreaIds.Guests,
                locationId
            ))
            {
                return new AssistantGuestsRetrieveResult.Ok(
                    AssistantGuestsEvidence.Empty
                );
            }

            return await _guestsRetrieve.RetrieveAsync(
                locationId,
                cancellationToken
            );
        }

        private async Task<bool> CanRetrieveAreaAsync(
            int userId,
            string areaId,
            int locationId
        )
        {
            var decision = await _permissions.AuthorizeLocationForUserAsync(
                userId,
                areaId,
                PermissionLevel.View,
                locationId
            );
            return decision.Status == RestaurantPermissionStatus.Allowed;
        }

        private async Task<bool> CanPersistDraftAsync(
            int userId,
            string targetAreaId,
            int locationId
        )
        {
            var assistant = await _permissions.AuthorizeUserAsync(
                userId,
                OperatorAreaIds.AiAssistant,
                PermissionLevel.Manage
            );
            if (assistant.Status != RestaurantPermissionStatus.Allowed)
            {
                return false;
            }

            var target = await _permissions.AuthorizeLocationForUserAsync(
                userId,
                targetAreaId,
                PermissionLevel.Manage,
                locationId
            );
            return target.Status == RestaurantPermissionStatus.Allowed;
        }

        private readonly record struct OwnedLocationRow(
            int Id,
            string Name,
            string Address,
            CaptureLocationStatus CaptureStatus,
            string AccountType
        );

        private sealed record CompareAllRetrieve(
            IReadOnlyList<AssistantCompareLocationEvidence> Landed,
            IReadOnlyList<string> FailedNames,
            IReadOnlyList<string> NotStartedNames
        );

        private sealed record TurnRetrieve(
            AssistantRetrievedEvidence SavedEvidence,
            IReadOnlyList<AssistantCompareLocationEvidence> CompareRows
        );

        private async Task TryPublishProgressAsync(
            int userId,
            int conversationId,
            string step,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await _progress.PublishAsync(
                    userId,
                    conversationId,
                    step,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Progress is cosmetic; hub failure must not fail the turn.
            }
        }

        private async Task<AssistantTurnOutcome?> TryFinishSendScheduleRouteAsync(
            AssistantConversation conversation,
            string userMessage,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            if (AssistantSendScheduleAsk.LooksLikeOfferActivate(userMessage))
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.OfferActivateBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            if (!AssistantSendScheduleAsk.LooksLikeSendOrSchedule(userMessage))
            {
                return null;
            }

            var recovery = AssistantRecoveryWork.Parse(conversation.RecoveryWorkJson);
            var hasCampaign = conversation.CreatedCampaignId is int;
            var hasRecovery = recovery is not null;
            if (!hasCampaign && !hasRecovery)
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.NoStoredBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            var namesCampaign = AssistantSendScheduleAsk.LooksLikeCampaignNamed(userMessage);
            var namesRecovery = AssistantSendScheduleAsk.LooksLikeRecoverySend(userMessage);
            var timedSchedule = AssistantSendScheduleAsk.LooksLikeTimedSchedule(userMessage);

            if (hasRecovery && (!hasCampaign || (namesRecovery && !namesCampaign)))
            {
                if (namesCampaign && !namesRecovery)
                {
                    return await PersistSendScheduleTurnAsync(
                        conversation,
                        RefusalMessage(
                            DateTime.UtcNow,
                            AssistantSendScheduleCopy.OtherTypeBody()
                        ),
                        replaceFailure,
                        cancellationToken
                    );
                }

                if (timedSchedule)
                {
                    return await PersistSendScheduleTurnAsync(
                        conversation,
                        RefusalMessage(
                            DateTime.UtcNow,
                            AssistantSendScheduleCopy.RecoveryScheduleBody()
                        ),
                        replaceFailure,
                        cancellationToken
                    );
                }

                return await PersistRecoverySendRouteAsync(
                    conversation,
                    recovery!,
                    replaceFailure,
                    cancellationToken
                );
            }

            if (hasCampaign && namesRecovery && !namesCampaign)
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.OtherTypeBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            var campaignId = conversation.CreatedCampaignId!.Value;
            CampaignDraftDto? draft;
            try
            {
                draft = await _campaignDrafts.GetByIdAsync(campaignId, cancellationToken);
            }
            catch
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.OpenFailureBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            if (draft is null || draft.LocationId != conversation.OwnedLocationId)
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.OpenFailureBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            if (AssistantSendScheduleAsk.IsNamedCampaignMismatch(userMessage, draft.Name))
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    GroundedMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.MismatchTitle,
                        AssistantSendScheduleCopy.NamedMismatchBody(draft.Name),
                        []
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            var landing = AssistantSendScheduleAsk.CampaignLanding(
                userMessage,
                DateTime.UtcNow
            );
            return await PersistSendScheduleTurnAsync(
                conversation,
                GroundedMessage(
                    DateTime.UtcNow,
                    AssistantSendScheduleCopy.Title,
                    AssistantSendScheduleCopy.CampaignBody(landing.Step),
                    []
                ),
                replaceFailure,
                cancellationToken,
                new AssistantSendScheduleRouteDto
                {
                    Kind = AssistantSendScheduleAsk.KindCampaign,
                    CampaignId = draft.Id,
                    Step = landing.Step,
                    ScheduleMode = landing.ScheduleMode,
                    DateLocal = landing.DateLocal,
                    TimeLocal = landing.TimeLocal,
                }
            );
        }

        private async Task<AssistantTurnOutcome> PersistRecoverySendRouteAsync(
            AssistantConversation conversation,
            AssistantRecoveryWorkState recovery,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            Feedback? feedback;
            try
            {
                feedback = await _context.Feedbacks
                    .Include(item => item.LocationGuest)
                    .FirstOrDefaultAsync(
                        item => item.Id == recovery.FeedbackId,
                        cancellationToken
                    );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.OpenFailureBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            if (feedback is null
                || feedback.RestaurantLocationId != conversation.OwnedLocationId)
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.OpenFailureBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            if (AssistantRecoveryEligibility.Evaluate(feedback, recovery.Intent)
                is AssistantRecoveryEligibility.Outcome.Blocked)
            {
                return await PersistSendScheduleTurnAsync(
                    conversation,
                    RefusalMessage(
                        DateTime.UtcNow,
                        AssistantSendScheduleCopy.OpenFailureBody()
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            return await PersistSendScheduleTurnAsync(
                conversation,
                GroundedMessage(
                    DateTime.UtcNow,
                    AssistantSendScheduleCopy.Title,
                    AssistantSendScheduleCopy.RecoveryBody(),
                    []
                ),
                replaceFailure,
                cancellationToken,
                new AssistantSendScheduleRouteDto
                {
                    Kind = AssistantSendScheduleAsk.KindRecovery,
                    FeedbackId = recovery.FeedbackId,
                    Intent = recovery.Intent,
                }
            );
        }

        private async Task<AssistantTurnOutcome> PersistSendScheduleTurnAsync(
            AssistantConversation conversation,
            AssistantMessage assistantMessage,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            AssistantSendScheduleRouteDto? sendScheduleRoute = null
        )
        {
            conversation.LastCompareLocationIdsJson = null;
            return await PersistAssistantAsync(
                conversation,
                assistantMessage,
                replaceFailure,
                cancellationToken,
                sendScheduleRoute: sendScheduleRoute
            );
        }

        private async Task<AssistantTurnOutcome> PersistAssistantAsync(
            AssistantConversation conversation,
            AssistantMessage assistantMessage,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            string? proposedConversationTitle = null,
            bool liveAnswerAlreadyCompleted = false,
            AssistantSendScheduleRouteDto? sendScheduleRoute = null,
            AssistantTurnBilling? turnBilling = null
        )
        {
            if (replaceFailure is not null)
            {
                conversation.Messages.Remove(replaceFailure);
                _context.AssistantMessages.Remove(replaceFailure);
            }

            if (!string.IsNullOrWhiteSpace(_pendingAssistantBodyPrefix))
            {
                assistantMessage.Body =
                    $"{_pendingAssistantBodyPrefix} {assistantMessage.Body}".Trim();
                _pendingAssistantBodyPrefix = null;
            }

            if (!liveAnswerAlreadyCompleted)
            {
                proposedConversationTitle = await TryReadModelConversationTitleAsync(
                    conversation,
                    assistantMessage,
                    cancellationToken
                );
            }

            if (turnBilling is not null && turnBilling.ShouldConsume(assistantMessage))
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var consume = await _aiBilling.ConsumeCompletedAnswerAsync(
                        turnBilling.RestaurantId,
                        turnBilling.LocationId,
                        cancellationToken
                    );
                    if (!consume.Succeeded)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return CreditSpendFromConsumeFail(consume);
                    }

                    AssistantConversationTitle.TryApply(
                        conversation,
                        assistantMessage,
                        proposedConversationTitle
                    );
                    conversation.Messages.Add(assistantMessage);
                    conversation.LastActivityAt = assistantMessage.CreatedAt;
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            else
            {
                AssistantConversationTitle.TryApply(
                    conversation,
                    assistantMessage,
                    proposedConversationTitle
                );
                conversation.Messages.Add(assistantMessage);
                conversation.LastActivityAt = assistantMessage.CreatedAt;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var dto = AssistantAnalysisScope.ToConversationDto(conversation);
            dto.SendScheduleRoute = sendScheduleRoute;

            if (turnBilling is not null
                && !string.IsNullOrWhiteSpace(turnBilling.IdempotencyKey)
                && turnBilling.ShouldConsume(assistantMessage))
            {
                await _aiBilling.StoreOutcomeAsync(
                    turnBilling.RestaurantId,
                    turnBilling.IdempotencyKey,
                    dto,
                    cancellationToken
                );
            }

            return new AssistantTurnOutcome.Ok(dto);
        }

        private sealed record BilledLiveAnswerGate(
            AssistantTurnBilling? Billing,
            AssistantTurnOutcome? Error
        );

        private async Task<AssistantTurnOutcome?> TryReplayIdempotentTurnAsync(
            AssistantAnalysisScopeDto scope,
            string? idempotencyKey,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey)
                || scope.OwnedLocationId is not int locationId)
            {
                return null;
            }

            var restaurantId = await _aiBilling.TryResolveRestaurantIdAsync(
                locationId,
                cancellationToken
            );
            if (restaurantId is not int resolvedRestaurantId)
            {
                return null;
            }

            var cached = await _aiBilling.TryGetCachedOutcomeAsync(
                resolvedRestaurantId,
                idempotencyKey,
                cancellationToken
            );
            return cached is null
                ? null
                : new AssistantTurnOutcome.Ok(cached);
        }

        private async Task<BilledLiveAnswerGate> TryBeginBilledLiveAnswerAsync(
            AssistantConversation conversation,
            int? boundCreateLocationId,
            string? idempotencyKey,
            CancellationToken cancellationToken
        )
        {
            int locationId;
            try
            {
                locationId = CreatePersistLocationId(boundCreateLocationId, conversation);
            }
            catch (InvalidOperationException)
            {
                if (AssistantAnalysisScope.IsAll(conversation))
                {
                    return new BilledLiveAnswerGate(null, null);
                }

                return new BilledLiveAnswerGate(
                    null,
                    new AssistantTurnOutcome.CreditSpendDenied(
                        "location_required",
                        CreditChannels.Ai,
                        0,
                        AssistantAiBillingRules.CompletedAnswerUnits
                    )
                );
            }

            var restaurantId = await _aiBilling.TryResolveRestaurantIdAsync(
                locationId,
                cancellationToken
            );
            if (restaurantId is not int resolvedRestaurantId)
            {
                return new BilledLiveAnswerGate(
                    null,
                    new AssistantTurnOutcome.CreditSpendDenied(
                        "location_not_in_account",
                        CreditChannels.Ai,
                        0,
                        AssistantAiBillingRules.CompletedAnswerUnits
                    )
                );
            }

            var remaining = await _aiBilling.GetAiRemainingAsync(
                resolvedRestaurantId,
                cancellationToken
            );
            if (remaining < AssistantAiBillingRules.CompletedAnswerUnits)
            {
                return new BilledLiveAnswerGate(
                    null,
                    new AssistantTurnOutcome.CreditSpendDenied(
                        "channel_hard_stopped",
                        CreditChannels.Ai,
                        remaining,
                        AssistantAiBillingRules.CompletedAnswerUnits
                    )
                );
            }

            return new BilledLiveAnswerGate(
                new AssistantTurnBilling
                {
                    RestaurantId = resolvedRestaurantId,
                    LocationId = locationId,
                    IdempotencyKey = idempotencyKey,
                },
                null
            );
        }

        private static AssistantTurnOutcome CreditSpendFromConsumeFail(
            CreditLedgerWriteResult consume
        )
        {
            var code = consume.Code switch
            {
                "location_required" => "location_required",
                "location_not_in_account" => "location_not_in_account",
                "channel_hard_stopped" => "channel_hard_stopped",
                _ => "insufficient_credits",
            };

            return new AssistantTurnOutcome.CreditSpendDenied(
                code,
                CreditChannels.Ai,
                0,
                AssistantAiBillingRules.CompletedAnswerUnits
            );
        }

        private async Task<string?> TryReadModelConversationTitleAsync(
            AssistantConversation conversation,
            AssistantMessage assistantMessage,
            CancellationToken cancellationToken
        )
        {
            if (assistantMessage.Class == AssistantMessageClass.Failure)
            {
                return null;
            }

            var userTurns = conversation.Messages.Count(
                message => message.Role == AssistantMessageRole.User
            );
            if (userTurns != 1)
            {
                return null;
            }

            var user = conversation.Messages
                .Where(message => message.Role == AssistantMessageRole.User)
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .Last();
            var scope = AssistantAnalysisScope.FromConversation(conversation);
            try
            {
                var answer = await _liveAnswer.CompleteAsync(
                    new AssistantLiveAnswerInput(
                        user.Body,
                        conversation.OwnedLocationName,
                        AssistantAnalysisScope.PeriodPhrase(scope.ReportingPeriod),
                        EmptyEvidence
                    ),
                    cancellationToken
                );
                return answer is AssistantLiveAnswerResult.Succeeded succeeded
                    ? succeeded.ConversationTitle
                    : null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
            }
        }

        private async Task<AssistantConversation?> LoadOwnedConversationAsync(
            int ownerUserId,
            int conversationId,
            CancellationToken cancellationToken
        )
        {
            return await _context.AssistantConversations
                .Include(conversation => conversation.Messages)
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.Id == conversationId
                        && conversation.OwnerUserId == ownerUserId,
                    cancellationToken
                );
        }

        private async Task<AssistantTurnOutcome?> TryFinishExplainWhyFollowUpAsync(
            AssistantConversation conversation,
            string userMessage,
            AssistantAnalysisScopeDto scope,
            string locationName,
            IReadOnlyList<AssistantOwnedLocationRef> locationRefs,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            var kind = AssistantExplainWhyFollowUp.Detect(userMessage);
            if (kind == AssistantExplainWhyKind.None)
            {
                return null;
            }

            var priorAssistant = conversation.Messages
                .Where(message =>
                    message.Role == AssistantMessageRole.Assistant
                    && message.Class == AssistantMessageClass.Grounded)
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.Id)
                .LastOrDefault();
            var priorUser = priorAssistant is null
                ? null
                : conversation.Messages
                    .Where(message =>
                        message.Role == AssistantMessageRole.User
                        && (message.CreatedAt < priorAssistant.CreatedAt
                            || (message.CreatedAt == priorAssistant.CreatedAt
                                && message.Id < priorAssistant.Id)))
                    .OrderBy(message => message.CreatedAt)
                    .ThenBy(message => message.Id)
                    .LastOrDefault();
            if (priorAssistant is null || priorUser is null)
            {
                return null;
            }

            var path = AssistantExplainWhyFollowUp.InferPriorPath(
                priorUser.Body,
                priorAssistant.Title ?? string.Empty,
                priorAssistant.Body
            );
            if (!AssistantExplainWhyFollowUp.MatchesPrior(
                    kind,
                    path,
                    priorAssistant.Body
                ))
            {
                return null;
            }

            var priorScope = AssistantAnalysisScope.FromUserMessage(priorUser);
            var refetch = priorScope is null
                || !AssistantAnalysisScope.ScopesEqual(priorScope, scope)
                || AssistantExplainWhyFollowUp.NamesNewPeriodOrLocation(
                    userMessage,
                    scope,
                    locationRefs
                );

            var title = priorAssistant.Title ?? string.Empty;
            var body = priorAssistant.Body;
            var actions = AssistantAnalysisScope.ParseActions(priorAssistant.ActionsJson);
            var productTopics = AssistantProductExpertTopics.Detect(priorUser.Body);
            var fetchLocationId = conversation.OwnedLocationId;
            var fetchLocationName = locationName;
            var fetchPeriod = scope.ReportingPeriod;
            var namedLocation = AssistantExplainWhyFollowUp.NamedOtherLocation(
                userMessage,
                conversation.OwnedLocationId,
                locationRefs
            );
            if (namedLocation is AssistantOwnedLocationRef named)
            {
                fetchLocationId = named.Id;
                fetchLocationName = named.Name;
            }

            var namedPreset = AssistantExplainWhyFollowUp.NamedPeriodPreset(userMessage);
            if (namedPreset is not null)
            {
                fetchPeriod = new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = namedPreset,
                };
            }

            if (refetch
                && path is AssistantExplainWhyPriorPath.NeedsAttention
                    or AssistantExplainWhyPriorPath.RecommendedNextStep
                    or AssistantExplainWhyPriorPath.Mix
                    or AssistantExplainWhyPriorPath.WeeklyBrief
                && fetchLocationId is int attentionLocationId)
            {
                var surface = AssistantAttentionAsk.Detect(priorUser.Body);
                if (surface == AssistantAttentionSurface.None)
                {
                    surface = AssistantAttentionSurface.NeedsAttention;
                }

                await TryPublishProgressAsync(
                    conversation.OwnerUserId,
                    conversation.Id,
                    AssistantTurnProgressSteps.Retrieving,
                    cancellationToken
                );
                var presented = await _attentionRetrieve.PresentAsync(
                    surface,
                    conversation.OwnerUserId,
                    attentionLocationId,
                    fetchLocationName,
                    fetchPeriod,
                    cancellationToken
                );
                title = presented.Title;
                body = presented.Body;
                actions = presented.Actions;
            }
            else if (refetch && path == AssistantExplainWhyPriorPath.ProductExpert)
            {
                var canned = AssistantProductExpertTopics.Assemble(productTopics);
                title = canned.Title;
                body = canned.Body;
                actions = [];
            }
            else if (refetch
                && path == AssistantExplainWhyPriorPath.DomainRetrieve
                && fetchLocationId is int domainLocationId)
            {
                var periodPhrase = AssistantAnalysisScope.PeriodPhrase(fetchPeriod);
                var window = AssistantReportingPeriodWindow.Resolve(
                    fetchPeriod,
                    DateTime.UtcNow
                );
                await TryPublishProgressAsync(
                    conversation.OwnerUserId,
                    conversation.Id,
                    AssistantTurnProgressSteps.Retrieving,
                    cancellationToken
                );
                var retrieved = await RetrieveForTurnAsync(
                    conversation.OwnerUserId,
                    [domainLocationId],
                    domainLocationId,
                    locationRefs,
                    window.FromUtc,
                    window.ToUtc,
                    AssistantAskIntent.NeedsCampaignCopy(priorUser.Body),
                    cancellationToken
                );
                if (retrieved is null)
                {
                    conversation.LastCompareLocationIdsJson = null;
                    return await PersistAssistantAsync(
                        conversation,
                        FailureMessage(DateTime.UtcNow),
                        replaceFailure,
                        cancellationToken
                    );
                }

                var grounded = AssistantLiveAnswerCopy.GroundedFromEvidence(
                    priorUser.Body,
                    fetchLocationName,
                    periodPhrase,
                    retrieved.SavedEvidence
                );
                title = grounded.Title ?? string.Empty;
                body = grounded.Body;
                actions = grounded.Actions;
                conversation.LastCompareLocationIdsJson = null;
            }

            var expanded = AssistantExplainWhyCopy.Expand(
                kind,
                path,
                title,
                body,
                actions,
                path == AssistantExplainWhyPriorPath.ProductExpert
                    ? productTopics
                    : null
            );
            return await PersistAssistantAsync(
                conversation,
                GroundedMessage(
                    DateTime.UtcNow,
                    expanded.Title,
                    expanded.Body,
                    expanded.Actions
                ),
                replaceFailure,
                cancellationToken,
                liveAnswerAlreadyCompleted: true
            );
        }

        private async Task<AssistantTurnOutcome> FinishAttentionRetrieveAsync(
            AssistantConversation conversation,
            AssistantAttentionSurface surface,
            int locationId,
            string locationName,
            AssistantReportingPeriodDto reportingPeriod,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            conversation.LastCompareLocationIdsJson = null;
            await TryPublishProgressAsync(
                conversation.OwnerUserId,
                conversation.Id,
                AssistantTurnProgressSteps.Retrieving,
                cancellationToken
            );
            var presented = await _attentionRetrieve.PresentAsync(
                surface,
                conversation.OwnerUserId,
                locationId,
                locationName,
                reportingPeriod,
                cancellationToken
            );
            return await PersistAssistantAsync(
                conversation,
                GroundedMessage(
                    DateTime.UtcNow,
                    presented.Title,
                    presented.Body,
                    presented.Actions
                ),
                replaceFailure,
                cancellationToken,
                liveAnswerAlreadyCompleted: true
            );
        }

        private static AssistantMessage FailureMessage(DateTime createdAt)
            => new()
            {
                Role = AssistantMessageRole.Assistant,
                Class = AssistantMessageClass.Failure,
                Title = null,
                Body = AssistantAnalysisScope.FailureBody,
                ActionsJson = "[]",
                CreatedAt = createdAt,
            };

        private static AssistantMessage ClarifyMessage(DateTime createdAt, string body)
            => new()
            {
                Role = AssistantMessageRole.Assistant,
                Class = AssistantMessageClass.Clarify,
                Title = null,
                Body = body,
                ActionsJson = "[]",
                CreatedAt = createdAt,
            };

        private static AssistantMessage GapMessage(DateTime createdAt, string body)
            => new()
            {
                Role = AssistantMessageRole.Assistant,
                Class = AssistantMessageClass.Gap,
                Title = null,
                Body = body,
                ActionsJson = "[]",
                CreatedAt = createdAt,
            };

        private static AssistantMessage RefusalMessage(DateTime createdAt, string body)
            => new()
            {
                Role = AssistantMessageRole.Assistant,
                Class = AssistantMessageClass.Refusal,
                Title = null,
                Body = body,
                ActionsJson = "[]",
                CreatedAt = createdAt,
            };

        private static AssistantMessage GroundedMessage(
            DateTime createdAt,
            string title,
            string body,
            IReadOnlyList<AssistantActionDto> actions
        )
            => new()
            {
                Role = AssistantMessageRole.Assistant,
                Class = AssistantMessageClass.Grounded,
                Title = title,
                Body = body,
                ActionsJson = AssistantAnalysisScope.SerializeActions(actions),
                CreatedAt = createdAt,
            };

        private static string AppendRefusedOutParts(string body, string userMessage)
        {
            var refused = AssistantLiveAnswerCopy.RefusedOutPartSentences(userMessage);
            return refused.Count == 0
                ? body
                : $"{body}\n\n{string.Join(" ", refused)}";
        }
    }
}
