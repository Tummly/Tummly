using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/assistant")]
    [Authorize]
    public class AssistantController : ControllerBase
    {
        private readonly IAssistantConversationService _conversations;
        private readonly ISpeechToTextProvider _speechToText;

        public AssistantController(
            IAssistantConversationService conversations,
            ISpeechToTextProvider speechToText
        )
        {
            _conversations = conversations;
            _speechToText = speechToText;
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

        /*
         =========================================
         EPHEMERAL OPERATOR SPEECH-TO-TEXT
         Audio is transcribed in-memory and discarded — never stored.
         =========================================
        */

        [HttpPost("stt")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> TranscribeSpeech(
            IFormFile? audio,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Audio is required.",
                });
            }

            await using var audioStream = audio.OpenReadStream();
            var result = await _speechToText.TranscribeAsync(
                audioStream,
                audio.ContentType ?? "application/octet-stream",
                cancellationToken
            );

            return result switch
            {
                SpeechToTextResult.Succeeded succeeded => Ok(new
                {
                    success = true,
                    text = succeeded.Text.Trim(),
                }),
                SpeechToTextResult.EmptySpeech => UnprocessableEntity(new
                {
                    success = false,
                    code = "empty_speech",
                    message =
                        "We didn't catch any speech. Try again or type your question.",
                }),
                _ => StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        code = "stt_failure",
                        message =
                            "We couldn't transcribe that recording. Try again or type your question.",
                    }
                ),
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
