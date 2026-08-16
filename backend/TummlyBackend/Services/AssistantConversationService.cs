using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Assistant;
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
            IAssistantProgressPublisher progress
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
            var campaignDraftState = AssistantCampaignDraftInterview.Parse(
                conversation.DraftInterviewJson
            );
            var offerDraftState = AssistantOfferDraftInterview.Parse(
                conversation.DraftInterviewJson
            );
            var recoveryDraftState = AssistantRecoveryDraftInterview.Parse(
                conversation.DraftInterviewJson
            );
            var cancelDraft =
                (campaignDraftState is not null
                    || offerDraftState is not null
                    || recoveryDraftState is not null)
                && AssistantCampaignDraftInterview.IsClearCancel(userMessage);
            if (cancelDraft)
            {
                conversation.DraftInterviewJson = null;
                campaignDraftState = null;
                offerDraftState = null;
                recoveryDraftState = null;
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
            await _progress.PublishAsync(
                conversation.OwnerUserId,
                conversation.Id,
                AssistantTurnProgressSteps.Checking,
                cancellationToken
            );

            if (compareOutcome is AssistantCompareOutcome.Clarify clarify)
            {
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    ClarifyMessage(DateTime.UtcNow, clarify.Body),
                    replaceFailure,
                    cancellationToken
                );
            }

            var hasRetrieveAsk = AssistantAskIntent.HasRetrieveAsk(userMessage);
            if (cancelDraft && !hasRetrieveAsk)
            {
                conversation.LastCompareLocationIdsJson = null;
                return await PersistAssistantAsync(
                    conversation,
                    GroundedMessage(
                        DateTime.UtcNow,
                        "Draft interview cancelled",
                        "I cancelled the incomplete draft interview.",
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
                await _progress.PublishAsync(
                    conversation.OwnerUserId,
                    conversation.Id,
                    AssistantTurnProgressSteps.Retrieving,
                    cancellationToken
                );
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

            var draftTargets = AssistantCampaignDraftInterview.DetectDraftTargets(
                userMessage
            );
            var askKind = AssistantAskIntent.Classify(userMessage);
            var isCampaignDraftAsk =
                AssistantCampaignDraftInterview.IsCampaignDraftAsk(userMessage)
                && askKind != AssistantAskKind.Mixed;
            var isOfferDraftAsk =
                AssistantOfferDraftInterview.IsOfferDraftAsk(userMessage)
                && askKind != AssistantAskKind.Mixed;
            var isRecoveryDraftAsk =
                AssistantRecoveryDraftInterview.IsRecoveryDraftAsk(userMessage)
                && askKind != AssistantAskKind.Mixed;

            string? draftTargetChoiceBody = null;
            string? draftInterviewTitle = null;
            string? draftInterviewBody = null;
            bool draftInterviewReady = false;
            IReadOnlyList<AssistantActionDto> draftReadyActions = [];
            var draftComposed = false;

            if (!cancelDraft && draftTargets.Count > 1)
            {
                draftTargetChoiceBody =
                    $"Which one target should I draft: {string.Join(" or ", draftTargets)}? "
                    + AssistantLiveAnswerCopy.OneDraftTargetSentence;
                draftComposed = true;
                if (!hasRetrieveAsk)
                {
                    conversation.LastCompareLocationIdsJson = null;
                    return await PersistAssistantAsync(
                        conversation,
                        GroundedMessage(
                            DateTime.UtcNow,
                            "Choose one draft target",
                            AppendRefusedOutParts(
                                draftTargetChoiceBody,
                                userMessage
                            ),
                            []
                        ),
                        replaceFailure,
                        cancellationToken
                    );
                }
            }
            else if (!cancelDraft)
            {
                var namedCampaign =
                    draftTargets.Contains("Campaign", StringComparer.Ordinal)
                    || isCampaignDraftAsk;
                var namedOffer =
                    draftTargets.Contains("Offer", StringComparer.Ordinal)
                    || isOfferDraftAsk;
                var namedRecovery =
                    draftTargets.Contains("Feedback recovery", StringComparer.Ordinal)
                    || isRecoveryDraftAsk;

                if (namedRecovery
                    || (recoveryDraftState is not null
                        && !namedCampaign
                        && !namedOffer))
                {
                    var current = namedRecovery ? null : recoveryDraftState;
                    var draftTurn = AssistantRecoveryDraftInterview.Apply(
                        current,
                        userMessage,
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
                else if (namedOffer
                    || (offerDraftState is not null
                        && !namedCampaign
                        && !namedRecovery))
                {
                    var current = namedOffer ? null : offerDraftState;
                    var draftTurn = AssistantOfferDraftInterview.Apply(
                        current,
                        userMessage
                    );
                    conversation.DraftInterviewJson =
                        AssistantOfferDraftInterview.Serialize(draftTurn.State);
                    draftInterviewTitle = draftTurn.Title;
                    draftInterviewBody = draftTurn.Body;
                    draftInterviewReady = draftTurn.IsReady;
                    draftReadyActions = draftTurn.IsReady
                        ? AssistantActionCatalog.ValidateOfferDraft(
                            [new AssistantActionDto { Type = "draft-offer" }],
                            AssistantMessageClass.Grounded
                        )
                        : [];
                    draftComposed = true;
                }
                else if (namedCampaign
                    || (campaignDraftState is not null
                        && !namedOffer
                        && !namedRecovery))
                {
                    var current = namedCampaign ? null : campaignDraftState;
                    var draftTurn = AssistantCampaignDraftInterview.Apply(
                        current,
                        userMessage,
                        savedEvidence.Offers
                    );
                    conversation.DraftInterviewJson =
                        AssistantCampaignDraftInterview.Serialize(draftTurn.State);
                    draftInterviewTitle = draftTurn.Title;
                    draftInterviewBody = draftTurn.Body;
                    draftInterviewReady = draftTurn.IsReady;
                    draftReadyActions = draftTurn.IsReady
                        ? AssistantActionCatalog.ValidateCampaignDraft(
                            [new AssistantActionDto { Type = "draft-campaign" }],
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
                await _progress.PublishAsync(
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
            if (answer is AssistantLiveAnswerResult.Succeeded succeeded)
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
            else
            {
                conversation.LastCompareLocationIdsJson = null;
                assistantMessage = FailureMessage(assistantNow);
            }

            return await PersistAssistantAsync(
                conversation,
                assistantMessage,
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

        private async Task<AssistantTurnOutcome> PersistAssistantAsync(
            AssistantConversation conversation,
            AssistantMessage assistantMessage,
            AssistantMessage? replaceFailure,
            CancellationToken cancellationToken
        )
        {
            if (replaceFailure is not null)
            {
                conversation.Messages.Remove(replaceFailure);
                _context.AssistantMessages.Remove(replaceFailure);
            }

            conversation.Messages.Add(assistantMessage);
            conversation.LastActivityAt = assistantMessage.CreatedAt;
            await _context.SaveChangesAsync(cancellationToken);

            return new AssistantTurnOutcome.Ok(
                AssistantAnalysisScope.ToConversationDto(conversation)
            );
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
