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
        private static readonly AssistantFeedbackEvidence EmptyEvidence = new(
            0,
            0,
            0,
            0,
            0,
            0,
            [],
            []
        );

        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IAssistantLiveAnswerProvider _liveAnswer;
        private readonly IAssistantFeedbackRetrieve _feedbackRetrieve;

        public AssistantConversationService(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            IAssistantLiveAnswerProvider liveAnswer,
            IAssistantFeedbackRetrieve feedbackRetrieve
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _liveAnswer = liveAnswer;
            _feedbackRetrieve = feedbackRetrieve;
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
            var compareOutcome = AssistantCompareTurn.Resolve(
                userMessage,
                conversation.OwnedLocationId,
                locationRefs,
                AssistantCompareTurn.ParseLocationIds(
                    conversation.LastCompareLocationIdsJson
                ),
                isSingleMode
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
            AssistantFeedbackEvidence savedEvidence;
            try
            {
                var retrieved = await RetrieveForTurnAsync(
                    compareIds,
                    conversation.OwnedLocationId,
                    locationRefs,
                    window.FromUtc,
                    window.ToUtc,
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

            AssistantLiveAnswerResult answer;
            try
            {
                answer = await _liveAnswer.CompleteAsync(
                    new AssistantLiveAnswerInput(
                        userMessage,
                        locationName,
                        periodPhrase,
                        savedEvidence,
                        compareEvidence,
                        caveat,
                        droppedUnknown
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
                var actions = AssistantActionCatalog.Validate(
                    succeeded.Actions,
                    succeeded.Class,
                    savedEvidence
                );
                assistantMessage = new AssistantMessage
                {
                    Role = AssistantMessageRole.Assistant,
                    Class = succeeded.Class,
                    Title = succeeded.Class == AssistantMessageClass.Grounded
                        ? succeeded.Title
                        : null,
                    Body = succeeded.Body,
                    ActionsJson = AssistantAnalysisScope.SerializeActions(actions),
                    CreatedAt = assistantNow,
                };
                conversation.LastCompareLocationIdsJson =
                    compareOutcome is AssistantCompareOutcome.Compare compareOk
                    && succeeded.Class == AssistantMessageClass.Grounded
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
            CancellationToken cancellationToken
        )
        {
            var byId = locationRefs.ToDictionary(location => location.Id);
            var compareRows = new List<AssistantCompareLocationEvidence>();
            AssistantFeedbackEvidence savedEvidence = EmptyEvidence;

            foreach (var locationId in locationIds.Distinct())
            {
                var retrieve = await _feedbackRetrieve.RetrieveAsync(
                    locationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                if (retrieve is AssistantFeedbackRetrieveResult.Failed)
                {
                    return null;
                }

                var evidence = retrieve is AssistantFeedbackRetrieveResult.Ok ok
                    ? ok.Evidence
                    : EmptyEvidence;
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

            return new TurnRetrieve(savedEvidence, compareRows);
        }

        private readonly record struct OwnedLocationRow(
            int Id,
            string Name,
            string Address,
            CaptureLocationStatus CaptureStatus,
            string AccountType
        );

        private sealed record TurnRetrieve(
            AssistantFeedbackEvidence SavedEvidence,
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
    }
}
