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
        private readonly ApplicationDbContext _context;
        private readonly IOwnedLocationService _ownedLocation;
        private readonly IAssistantLiveAnswerProvider _liveAnswer;

        public AssistantConversationService(
            ApplicationDbContext context,
            IOwnedLocationService ownedLocation,
            IAssistantLiveAnswerProvider liveAnswer
        )
        {
            _context = context;
            _ownedLocation = ownedLocation;
            _liveAnswer = liveAnswer;
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

            AssistantLiveAnswerResult answer;
            try
            {
                answer = await _liveAnswer.CompleteAsync(
                    new AssistantLiveAnswerInput(
                        message,
                        locationName,
                        AssistantAnalysisScope.PeriodPhrase(request.AnalysisScope.ReportingPeriod)
                    ),
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                await PersistFailureAsync(conversation, CancellationToken.None);
                throw;
            }

            var assistantNow = DateTime.UtcNow;
            if (answer is AssistantLiveAnswerResult.Succeeded succeeded)
            {
                conversation.Messages.Add(
                    new AssistantMessage
                    {
                        Role = AssistantMessageRole.Assistant,
                        Class = succeeded.Class,
                        Title = succeeded.Class == AssistantMessageClass.Grounded
                            ? succeeded.Title
                            : null,
                        Body = succeeded.Body,
                        CreatedAt = assistantNow,
                    }
                );
            }
            else
            {
                conversation.Messages.Add(CreateFailureMessage(assistantNow));
            }

            conversation.LastActivityAt = assistantNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new AssistantTurnOutcome.Ok(
                AssistantAnalysisScope.ToConversationDto(conversation)
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

        private async Task PersistFailureAsync(
            AssistantConversation conversation,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;
            conversation.Messages.Add(CreateFailureMessage(now));
            conversation.LastActivityAt = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static AssistantMessage CreateFailureMessage(DateTime createdAt)
            => new()
            {
                Role = AssistantMessageRole.Assistant,
                Class = AssistantMessageClass.Failure,
                Title = null,
                Body = AssistantAnalysisScope.FailureBody,
                CreatedAt = createdAt,
            };
    }
}
