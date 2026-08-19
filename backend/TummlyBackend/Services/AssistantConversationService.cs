using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.DTOs.Campaigns;
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
            IOffersCatalogService offersCatalog
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
        }

        public async Task<AssistantTurnOutcome> SendTurnAsync(
            int ownerUserId,
            SendAssistantTurnRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var message = request.Message?.Trim() ?? string.Empty;
            if (message.Length == 0)
            {
                return new AssistantTurnOutcome.Invalid("Message is required.");
            }

            var locationResult = await _ownedLocation.ResolveAsync(
                ownerUserId,
                request.AnalysisScope.OwnedLocationId
            );

            if (locationResult.Status != OwnedLocationResolveStatus.Found)
            {
                return new AssistantTurnOutcome.LocationDenied(locationResult);
            }

            var locationName = locationResult.Location!.LocationName;
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
                cancellationToken
            );
        }

        public async Task<AssistantTurnOutcome> RetryTurnAsync(
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

            var locationResult = await _ownedLocation.ResolveAsync(
                ownerUserId,
                sendScope.OwnedLocationId
            );

            if (locationResult.Status != OwnedLocationResolveStatus.Found)
            {
                return new AssistantTurnOutcome.LocationDenied(locationResult);
            }

            return await CompleteTurnAsync(
                conversation,
                lastUser.Body,
                sendScope,
                locationResult.Location!.LocationName,
                lastAssistant,
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

            var locationResult = await _ownedLocation.ResolveAsync(
                ownerUserId,
                conversation.OwnedLocationId
            );

            if (locationResult.Status != OwnedLocationResolveStatus.Found)
            {
                return new AssistantTurnOutcome.LocationDenied(locationResult);
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

            var locationResult = await _ownedLocation.ResolveAsync(
                ownerUserId,
                request.AnalysisScope.OwnedLocationId
            );

            if (locationResult.Status != OwnedLocationResolveStatus.Found)
            {
                return new AssistantTurnOutcome.LocationDenied(locationResult);
            }

            var lastActivity = conversation.LastActivityAt;
            AssistantAnalysisScope.CopyToConversation(
                conversation,
                request.AnalysisScope,
                locationResult.Location!.LocationName
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

        private async Task<AssistantTurnOutcome> CompleteTurnAsync(
            AssistantConversation conversation,
            string userMessage,
            AssistantAnalysisScopeDto scope,
            string locationName,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
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
            if (AssistantCampaignDraftInterview.Parse(conversation.DraftInterviewJson)
                is not null)
            {
                conversation.DraftInterviewJson = null;
            }

            if (AssistantOfferDraftInterview.Parse(conversation.DraftInterviewJson)
                is not null)
            {
                conversation.DraftInterviewJson = null;
            }
            var offerDraftState = AssistantOfferDraftInterview.Parse(
                conversation.DraftInterviewJson
            );
            var recoveryDraftState = AssistantRecoveryDraftInterview.Parse(
                conversation.DraftInterviewJson
            );
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
            var cancelDraft =
                (offerDraftState is not null
                    || recoveryDraftState is not null
                    || draftTargetChoiceState is not null
                    || gapState is not null)
                && AssistantCampaignDraftInterview.IsClearCancel(userMessage);
            var cancelledGap = cancelDraft && gapState is not null;
            if (cancelDraft)
            {
                conversation.DraftInterviewJson = null;
                offerDraftState = null;
                recoveryDraftState = null;
                draftTargetChoiceState = null;
                gapState = null;
            }
            else if (gapState is not null
                && AssistantTaskClassification.LooksLikeReplacingTask(userMessage))
            {
                conversation.DraftInterviewJson = null;
                gapState = null;
            }

            var compareOutcome = AssistantCompareTurn.Resolve(
                userMessage,
                conversation.OwnedLocationId,
                locationRefs,
                AssistantCompareTurn.ParseLocationIds(
                    conversation.LastCompareLocationIdsJson
                ),
                isSingleMode
            );
            await TryPublishProgressAsync(
                conversation.OwnerUserId,
                conversation.Id,
                AssistantTurnProgressSteps.Checking,
                cancellationToken
            );

            var isCreateTurn = gapState is not null
                || AssistantTaskClassification.LooksLikeCreateTurn(userMessage);
            if (compareOutcome is AssistantCompareOutcome.Clarify clarify
                && !isCreateTurn)
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
            if (gapState is not null)
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
                )
                && !AssistantAskIntent.IsHelpCentreAsk(userMessage)
            )
            {
                var createTask = AssistantTaskClassification.LooksLikeCreateCampaignDraft(
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
                if (createTask == AssistantTask.CreateCampaignDraft)
                {
                    preparedCampaignBind = await BindCampaignAsync(
                        userMessage,
                        boundCreateLocationId ?? conversation.OwnedLocationId,
                        boundCreateLocationName ?? locationName,
                        ownedLocationIds,
                        cancellationToken
                    );
                    var bindAbort = await TryFinishBindOutcomeAsync(
                        conversation,
                        userMessage,
                        preparedCampaignBind,
                        replaceFailure,
                        cancellationToken
                    );
                    if (bindAbort is not null)
                    {
                        return bindAbort;
                    }
                }
                else if (offerTerms is not null)
                {
                    var termsGap = await TryFinishOfferTermsGapAsync(
                        conversation,
                        userMessage,
                        offerTerms,
                        replaceFailure,
                        cancellationToken
                    );
                    if (termsGap is not null)
                    {
                        return termsGap;
                    }
                }
            }
            var hasActiveDraftInterview =
                offerDraftState is not null
                || recoveryDraftState is not null
                || draftTargetChoiceState is not null;
            var hasExplicitRetrieveAsk =
                AssistantAskIntent.HasExplicitRetrieveAsk(userMessage);
            var hasRetrieveAsk =
                AssistantAskIntent.HasRetrieveAsk(userMessage)
                && (
                    hasExplicitRetrieveAsk
                    || (!hasActiveDraftInterview && draftTargets.Count == 0)
                );
            if (cancelDraft && !hasRetrieveAsk)
            {
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    GroundedMessage(
                        DateTime.UtcNow,
                        cancelledGap ? "Question cancelled" : "Draft interview cancelled",
                        cancelledGap
                            ? "I cancelled that question."
                            : "I cancelled the incomplete draft interview.",
                        []
                    ),
                    replaceFailure,
                    cancellationToken
                );
            }

            var periodPhrase = AssistantAnalysisScope.PeriodPhrase(scope.ReportingPeriod);
            var window = AssistantReportingPeriodWindow.Resolve(
                scope.ReportingPeriod,
                DateTime.UtcNow
            );
            var compareIds = compareOutcome is AssistantCompareOutcome.Compare compare
                ? compare.LocationIds
                : (IReadOnlyList<int>)[conversation.OwnedLocationId];
            var droppedUnknown = compareOutcome is AssistantCompareOutcome.Compare compareDrop
                ? compareDrop.DroppedUnknownSentence
                : null;
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
            AssistantRetrievedEvidence savedEvidence;
            try
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

                compareEvidence = compareOutcome is AssistantCompareOutcome.Compare
                    ? retrieved.CompareRows
                    : null;
                savedEvidence = retrieved.SavedEvidence;
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
            var isRecoveryDraftAsk =
                AssistantRecoveryDraftInterview.IsRecoveryDraftAsk(userMessage)
                && askKind != AssistantAskKind.Mixed;

            string? draftTargetChoiceBody = null;
            string? draftInterviewTitle = null;
            string? draftInterviewBody = null;
            bool draftInterviewReady = false;
            IReadOnlyList<AssistantActionDto> draftReadyActions = [];
            var draftComposed = false;

            if (!cancelDraft)
            {
                var namedRecovery =
                    draftTargets.Contains("Feedback recovery", StringComparer.Ordinal)
                    || isRecoveryDraftAsk;
                var continuingInterview =
                    recoveryDraftState is not null
                    && !namedRecovery;
                // Skip field Apply only when the send is retrieve-only after stripping retrieve clauses.
                var applyMessage = continuingInterview
                    && hasRetrieveAsk
                    && string.IsNullOrWhiteSpace(
                        AssistantCampaignDraftInterview.InterviewAnswerPortion(
                            userMessage
                        )
                    )
                        ? string.Empty
                        : userMessage;

                if (namedRecovery
                    || (recoveryDraftState is not null))
                {
                    var current = namedRecovery ? null : recoveryDraftState;
                    var draftTurn = AssistantRecoveryDraftInterview.Apply(
                        current,
                        applyMessage,
                        savedEvidence.Feedback,
                        savedEvidence.Offers
                    );
                    conversation.DraftInterviewJson =
                        AssistantRecoveryDraftInterview.Serialize(draftTurn.State);
                    draftInterviewTitle = draftTurn.Title;
                    draftInterviewBody = draftTurn.Body;
                    draftInterviewReady = draftTurn.IsReady;
                    draftReadyActions = draftTurn.IsReady
                        ? AssistantActionCatalog.ValidateOpenRecovery(
                            [
                                new AssistantActionDto
                                {
                                    Type = "open-recovery",
                                    FeedbackId = draftTurn.State.FeedbackId,
                                    Intent = draftTurn.State.Intent,
                                },
                            ],
                            AssistantMessageClass.Grounded
                        )
                        : [];
                    draftComposed = true;
                }

                if (draftComposed && !hasRetrieveAsk && draftTargetChoiceBody is null)
                {
                    conversation.LastCompareLocationIdsJson = null;
                    return await PersistAssistantAsync(
                        conversation,
                        GroundedMessage(
                            DateTime.UtcNow,
                            draftInterviewTitle!,
                            AppendRefusedOutParts(draftInterviewBody!, userMessage),
                            draftReadyActions
                        ),
                        replaceFailure,
                        cancellationToken
                    );
                }
            }

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
                        SuppressMixedRefusal: draftComposed
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
                        AssistantTask.CreateCampaignDraft,
                        StringComparison.Ordinal
                    )
                    && !draftComposed
                    && !AssistantAskIntent.IsHelpCentreAsk(userMessage))
                {
                    var persistLocationId = boundCreateLocationId ?? conversation.OwnedLocationId;
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
                    && !draftComposed
                    && !AssistantAskIntent.IsHelpCentreAsk(userMessage))
                {
                    var persistLocationId = boundCreateLocationId ?? conversation.OwnedLocationId;
                    var persistLocationName = boundCreateLocationName ?? locationName;
                    var persist = await PersistCreateOfferDraftAsync(
                        conversation,
                        userMessage,
                        persistLocationId,
                        persistLocationName,
                        priorTerms: null,
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
                else
                {
                    var composed =
                        draftComposed
                        && succeeded.Class != AssistantMessageClass.Grounded
                            ? AssistantLiveAnswerCopy.WithSentences(
                                AssistantLiveAnswerCopy.GroundedFromEvidence(
                                    userMessage,
                                    locationName,
                                    periodPhrase,
                                    savedEvidence,
                                    suppressMixedRefusal: true
                                ),
                                caveat,
                                droppedUnknown
                            )
                            : succeeded;
                if (draftComposed)
                {
                    var interviewBody = draftInterviewBody ?? draftTargetChoiceBody!;
                    composed = composed with
                    {
                        Body = AppendRefusedOutParts(
                            $"{composed.Body}\n\n{interviewBody}",
                            userMessage
                        ),
                    };
                    conversation.LastCompareLocationIdsJson = null;
                }
                var actions = draftInterviewReady
                    ? draftReadyActions
                    : draftTargetChoiceBody is not null
                        ? []
                        : AssistantActionCatalog.Validate(
                            composed.Actions,
                            composed.Class,
                            savedEvidence,
                            AssistantAskIntent.ClassifyGrounded(userMessage)
                        );
                var redactionTokens = savedEvidence.Feedback.ContactRedactionTokens
                    .Concat(savedEvidence.Guests.ContactRedactionTokens)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var title = composed.Class == AssistantMessageClass.Grounded
                    ? AssistantContactRedaction.RedactTitle(
                        composed.Title,
                        redactionTokens
                    )
                    : null;
                var body = AssistantContactRedaction.RedactBody(
                    composed.Body,
                    redactionTokens
                );
                assistantMessage = new AssistantMessage
                {
                    Role = AssistantMessageRole.Assistant,
                    Class = composed.Class,
                    Title = title,
                    Body = body,
                    ActionsJson = AssistantAnalysisScope.SerializeActions(actions),
                    CreatedAt = assistantNow,
                };
                conversation.LastCompareLocationIdsJson =
                    compareOutcome is AssistantCompareOutcome.Compare compareOk
                    && composed.Class == AssistantMessageClass.Grounded
                    && !draftComposed
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
                liveAnswerAlreadyCompleted: true
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

        private sealed record CreateOfferDraftPersistTurn(
            string Title,
            string Body,
            IReadOnlyList<AssistantActionDto> Actions,
            int? CreatedOfferId
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
            var terms = AssistantOfferPathTerms.Merge(priorTerms, userMessage);
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
            catch
            {
                return new CreateOfferDraftPersistTurn(
                    AssistantOfferPathPersistCopy.FailureTitle,
                    AssistantOfferPathPersistCopy.FailureBody("Offer create"),
                    [],
                    null
                );
            }

            return new CreateOfferDraftPersistTurn(
                AssistantOfferPathPersistCopy.SuccessTitle,
                AssistantOfferPathPersistCopy.SuccessBody(
                    locationName,
                    AssistantOfferPathTerms.TypeLabel(terms.OfferType),
                    AssistantOfferPathTerms.ValueLabel(terms),
                    AssistantOfferPathTerms.ValidityLabel(terms),
                    created.Title,
                    terms.WantsActivate
                ),
                AssistantActionCatalog.ValidateReviewOffer(
                    created.Id,
                    AssistantMessageClass.Grounded
                ),
                created.Id
            );
        }

        private async Task<AssistantTurnOutcome?> TryFinishOfferTermsGapAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            AssistantOfferPathTermsState terms,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            if (AssistantOfferPathTerms.IsComplete(terms))
            {
                return null;
            }

            return await FinishGapTurnAsync(
                conversation,
                AssistantGapTurn.CreateOfferTerms(
                    sourceUserMessage,
                    AssistantOfferPathTerms.Serialize(terms)
                ),
                AssistantOfferPathTerms.GapBody(terms),
                replaceFailure,
                cancellationToken
            );
        }

        private sealed record GapResume(
            AssistantTurnOutcome? Outcome,
            IReadOnlyList<string>? DraftTargets
        );

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
            AssistantCampaignDraftBindChoice? choice = null
        )
        {
            var (locationOffers, otherOffers) = await LoadBindOffersAsync(
                locationId,
                ownedLocationIds,
                cancellationToken
            );
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
            CancellationToken cancellationToken
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
                => new(
                    offer.Id,
                    offer.Title,
                    offer.Status,
                    CatalogOfferStatus.IsAttachableActive(
                        offer.Status,
                        offer.Validity,
                        offer.CustomExpiryDate,
                        today
                    ),
                    offer.DiscountPercentage,
                    offer.DiscountAmount,
                    offer.FreeItemText
                );

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

        private async Task<AssistantTurnOutcome?> TryFinishBindOutcomeAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            AssistantCampaignDraftBindOutcome bind,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
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
                            AssistantTask.CreateCampaignDraft
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
            var detected = AssistantCreateTargets.Detect(userMessage);
            if (detected.Count > 1)
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

            var gapTarget = CreateTargetForTask(gapState.AssistantTask);
            if (detected.Count == 1
                && gapTarget is not null
                && !string.Equals(detected[0], gapTarget, StringComparison.Ordinal))
            {
                if (detected[0] == AssistantCreateTargets.Offer)
                {
                    return await ResumeOfferPathAsync(
                        conversation,
                        userMessage,
                        userMessage,
                        analysisScopeLocationName,
                        ownedLocations,
                        AssistantOfferPathTerms.Parse(userMessage),
                        uniqueNameIsChoice: false,
                        updateScope: false,
                        replaceFailure,
                        cancellationToken
                    );
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
                    if (AssistantAskIntent.HasExplicitRetrieveAsk(userMessage)
                        && detected.Count == 0)
                    {
                        conversation.DraftInterviewJson = null;
                        return new GapResume(null, null);
                    }

                    return new GapResume(
                        await FinishGapTurnAsync(
                            conversation,
                            gapState,
                            AssistantGapTurn.CreateTargetBody(gapState.Options),
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
                        finished.LocationId ?? conversation.OwnedLocationId,
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
                    return await ResumeOfferPathAsync(
                        conversation,
                        gapState.SourceUserMessage,
                        userMessage,
                        analysisScopeLocationName,
                        ownedLocations,
                        AssistantOfferPathTerms.FromJson(gapState.OfferTermsJson)
                            ?? AssistantOfferPathTerms.Parse(gapState.SourceUserMessage),
                        uniqueNameIsChoice: false,
                        updateScope: false,
                        replaceFailure,
                        cancellationToken
                    );
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
                    boundFinish.LocationId ?? conversation.OwnedLocationId,
                    boundFinish.LocationName ?? analysisScopeLocationName,
                    updateScope: false,
                    replaceFailure,
                    cancellationToken,
                    ownedLocations.Select(location => location.Id).ToList(),
                    choice: AssistantCampaignDraftBindChoice.FromGapKind(gapState.Kind, choice)
                );
                return new GapResume(resumed, null);
            }

            if (gapState.Kind == AssistantGapTurn.KindOfferTerms)
            {
                var prior = AssistantOfferPathTerms.FromJson(gapState.OfferTermsJson)
                    ?? AssistantOfferPathTerms.Parse(gapState.SourceUserMessage);
                return await ResumeOfferPathAsync(
                    conversation,
                    gapState.SourceUserMessage,
                    userMessage,
                    analysisScopeLocationName,
                    ownedLocations,
                    prior,
                    uniqueNameIsChoice: true,
                    updateScope: false,
                    replaceFailure,
                    cancellationToken
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
                conversation.DraftInterviewJson = null;
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
                            AssistantGapTurn.RepeatLocationBody(gapState),
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
                    AssistantTask.OfferPath,
                    StringComparison.Ordinal
                )
                && rememberedTerms is not null)
            {
                var termsGap = await TryFinishOfferTermsGapAsync(
                    conversation,
                    gapState.SourceUserMessage,
                    rememberedTerms,
                    replaceFailure,
                    cancellationToken
                );
                if (termsGap is not null)
                {
                    return new GapResume(termsGap, null);
                }
            }

            conversation.DraftInterviewJson = null;
            var persisted = await PersistCreateAndStoreAsync(
                conversation,
                gapState.SourceUserMessage,
                locationFinish.LocationId ?? conversation.OwnedLocationId,
                locationFinish.LocationName ?? analysisScopeLocationName,
                updateScope: true,
                replaceFailure,
                cancellationToken,
                ownedLocations.Select(location => location.Id).ToList(),
                gapState.AssistantTask,
                rememberedTerms
            );
            return new GapResume(persisted, null);
        }

        private async Task<GapResume> ResumeOfferPathAsync(
            AssistantConversation conversation,
            string sourceUserMessage,
            string userMessage,
            string analysisScopeLocationName,
            IReadOnlyList<OwnedLocationRow> ownedLocations,
            AssistantOfferPathTermsState priorTerms,
            bool uniqueNameIsChoice,
            bool updateScope,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            var terms = AssistantOfferPathTerms.Merge(priorTerms, userMessage);
            var locationMessage = uniqueNameIsChoice ? userMessage : sourceUserMessage;
            var locationOutcome = ResolveCreateLocation(
                locationMessage,
                conversation,
                analysisScopeLocationName,
                ownedLocations,
                uniqueNameIsChoice,
                AssistantTask.OfferPath
            );
            if (locationOutcome is AssistantLocationGapOutcome.Unnamed
                && uniqueNameIsChoice)
            {
                locationOutcome = ResolveCreateLocation(
                    sourceUserMessage,
                    conversation,
                    analysisScopeLocationName,
                    ownedLocations,
                    uniqueNameIsChoice: false,
                    AssistantTask.OfferPath
                );
            }

            var finished = await TryFinishLocationOutcomeAsync(
                conversation,
                sourceUserMessage,
                analysisScopeLocationName,
                locationOutcome,
                replaceFailure,
                cancellationToken,
                AssistantTask.OfferPath,
                terms
            );
            if (finished.Outcome is not null)
            {
                return new GapResume(finished.Outcome, null);
            }

            var termsGap = await TryFinishOfferTermsGapAsync(
                conversation,
                sourceUserMessage,
                terms,
                replaceFailure,
                cancellationToken
            );
            if (termsGap is not null)
            {
                return new GapResume(termsGap, null);
            }

            conversation.DraftInterviewJson = null;
            var persist = await PersistCreateAndStoreAsync(
                conversation,
                sourceUserMessage,
                finished.LocationId ?? conversation.OwnedLocationId,
                finished.LocationName ?? analysisScopeLocationName,
                updateScope,
                replaceFailure,
                cancellationToken,
                ownedLocations.Select(location => location.Id).ToList(),
                AssistantTask.OfferPath,
                terms
            );
            return new GapResume(persist, null);
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
            CancellationToken cancellationToken
        )
        {
            conversation.DraftInterviewJson = AssistantGapTurn.Serialize(state);
            conversation.LastCompareLocationIdsJson = null;
            return await PersistAssistantAsync(
                conversation,
                GapMessage(DateTime.UtcNow, body),
                replaceFailure,
                cancellationToken
            );
        }

        private async Task<IReadOnlyList<OwnedLocationRow>> LoadOwnedLocationsAsync(
            int savedLocationId,
            int ownerUserId,
            CancellationToken cancellationToken
        )
        {
            var saved = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(location => location.Restaurant)
                .FirstOrDefaultAsync(
                    location => location.Id == savedLocationId,
                    cancellationToken
                );
            if (saved?.Restaurant is null || saved.Restaurant.OwnerUserId != ownerUserId)
            {
                return [];
            }

            var rows = await _context.RestaurantLocations
                .AsNoTracking()
                .Include(location => location.Restaurant)
                .Where(location =>
                    location.RestaurantId == saved.RestaurantId
                    && location.Restaurant!.OwnerUserId == ownerUserId
                )
                .OrderBy(location => location.Id)
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

        private async Task<TurnRetrieve?> RetrieveForTurnAsync(
            IReadOnlyList<int> locationIds,
            int savedLocationId,
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
                savedEvidence = await RetrieveLocationDomainsAsync(
                    savedLocationId,
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

            return new TurnRetrieve(savedEvidence, compareRows);
        }

        private async Task<AssistantRetrievedEvidence?> RetrieveLocationDomainsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            bool includeCampaignCopy,
            CancellationToken cancellationToken
        )
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

            var feedback = feedbackRetrieve is AssistantFeedbackRetrieveResult.Ok feedbackOk
                ? feedbackOk.Evidence
                : AssistantFeedbackEvidence.Empty;

            return await RetrieveSavedDomainsAsync(
                locationId,
                fromUtc,
                toUtc,
                feedback,
                includeCampaignCopy,
                cancellationToken
            );
        }

        private async Task<AssistantRetrievedEvidence?> RetrieveSavedDomainsAsync(
            int savedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            AssistantFeedbackEvidence savedFeedback,
            bool includeCampaignCopy,
            CancellationToken cancellationToken
        )
        {
            var offersRetrieve = await _offersRetrieve.RetrieveAsync(
                savedLocationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var campaignsRetrieve = await _campaignsRetrieve.RetrieveAsync(
                savedLocationId,
                fromUtc,
                toUtc,
                includeCampaignCopy,
                cancellationToken
            );
            var captureRetrieve = await _captureRetrieve.RetrieveAsync(
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
            var guestsRetrieve = await _guestsRetrieve.RetrieveAsync(
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

        private readonly record struct OwnedLocationRow(
            int Id,
            string Name,
            string Address,
            CaptureLocationStatus CaptureStatus,
            string AccountType
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

        private async Task<AssistantTurnOutcome> PersistAssistantAsync(
            AssistantConversation conversation,
            AssistantMessage assistantMessage,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken,
            string? proposedConversationTitle = null,
            bool liveAnswerAlreadyCompleted = false
        )
        {
            if (replaceFailure is not null)
            {
                conversation.Messages.Remove(replaceFailure);
                _context.AssistantMessages.Remove(replaceFailure);
            }

            if (!liveAnswerAlreadyCompleted)
            {
                proposedConversationTitle = await TryReadModelConversationTitleAsync(
                    conversation,
                    assistantMessage,
                    cancellationToken
                );
            }

            AssistantConversationTitle.TryApply(
                conversation,
                assistantMessage,
                proposedConversationTitle
            );
            conversation.Messages.Add(assistantMessage);
            conversation.LastActivityAt = assistantMessage.CreatedAt;
            await _context.SaveChangesAsync(cancellationToken);

            return new AssistantTurnOutcome.Ok(
                AssistantAnalysisScope.ToConversationDto(conversation)
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
