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
