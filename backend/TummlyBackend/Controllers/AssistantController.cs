using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/assistant")]
    [Authorize]
    public class AssistantController : ControllerBase
    {
        private readonly IAssistantConversationService _conversations;

        public AssistantController(IAssistantConversationService conversations)
        {
            _conversations = conversations;
        }

        [HttpPost("turns")]
        public async Task<IActionResult> SendTurn(
            [FromBody] SendAssistantTurnRequest body,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            try
            {
                var outcome = await _conversations.SendTurnAsync(
                    userId,
                    body,
                    cancellationToken
                );
                return ToActionResult(outcome);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> ListConversations(
            [FromQuery] bool archived = false,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var outcome = await _conversations.ListAsync(
                userId,
                archived,
                cancellationToken
            );
            return outcome switch
            {
                AssistantListOutcome.Ok ok => Ok(new
                {
                    success = true,
                    conversations = ok.Conversations,
                }),
                _ => StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected Assistant list result.",
                }),
            };
        }

        [HttpGet("conversations/{conversationId:int}")]
        public async Task<IActionResult> GetConversation(
            int conversationId,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var outcome = await _conversations.GetAsync(
                userId,
                conversationId,
                cancellationToken
            );
            return ToActionResult(outcome);
        }

        [HttpPost("conversations/{conversationId:int}/retry")]
        public async Task<IActionResult> RetryTurn(
            int conversationId,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            try
            {
                var outcome = await _conversations.RetryTurnAsync(
                    userId,
                    conversationId,
                    cancellationToken
                );
                return ToActionResult(outcome);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }
        }

        [HttpPatch("conversations/{conversationId:int}/scope")]
        public async Task<IActionResult> ApplyScope(
            int conversationId,
            [FromBody] ApplyAssistantScopeRequest body,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var outcome = await _conversations.ApplyScopeAsync(
                userId,
                conversationId,
                body,
                cancellationToken
            );
            return ToActionResult(outcome);
        }

        [HttpPatch("conversations/{conversationId:int}/archive")]
        public async Task<IActionResult> ArchiveConversation(
            int conversationId,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var outcome = await _conversations.SetArchivedAsync(
                userId,
                conversationId,
                archived: true,
                cancellationToken
            );
            return ToActionResult(outcome);
        }

        [HttpPatch("conversations/{conversationId:int}/unarchive")]
        public async Task<IActionResult> UnarchiveConversation(
            int conversationId,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var outcome = await _conversations.SetArchivedAsync(
                userId,
                conversationId,
                archived: false,
                cancellationToken
            );
            return ToActionResult(outcome);
        }

        [HttpDelete("conversations/{conversationId:int}")]
        public async Task<IActionResult> DeleteConversation(
            int conversationId,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var outcome = await _conversations.DeleteAsync(
                userId,
                conversationId,
                cancellationToken
            );
            return outcome switch
            {
                AssistantDeleteOutcome.Ok => Ok(new { success = true }),
                AssistantDeleteOutcome.NotFound => NotFound(new
                {
                    success = false,
                    message = "Conversation not found.",
                }),
                _ => StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected Assistant delete result.",
                }),
            };
        }

        private IActionResult ToActionResult(AssistantTurnOutcome outcome)
            => outcome switch
            {
                AssistantTurnOutcome.Ok ok => Ok(new
                {
                    success = true,
                    conversation = ok.Conversation,
                }),
                AssistantTurnOutcome.Invalid invalid => BadRequest(new
                {
                    success = false,
                    message = invalid.Message,
                }),
                AssistantTurnOutcome.NotFound => NotFound(new
                {
                    success = false,
                    message = "Conversation not found.",
                }),
                AssistantTurnOutcome.LocationDenied denied =>
                    OwnedLocationResponses.FromResult(denied.Location)
                    ?? StatusCode(500, new
                    {
                        success = false,
                        message = "Unexpected owned-location resolve status.",
                    }),
                _ => StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected Assistant turn result.",
                }),
            };
    }
}
