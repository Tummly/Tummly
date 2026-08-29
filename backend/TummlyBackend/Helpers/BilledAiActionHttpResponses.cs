using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class BilledAiActionHttpResponses
    {
        public static IActionResult ToActionResult(this BilledAiActionResult result)
        {
            return result switch
            {
                BilledAiActionResult.Cached cached => OkDraft(cached.Payload),
                BilledAiActionResult.Succeeded succeeded => OkDraft(succeeded.Payload),
                BilledAiActionResult.HardStopped stopped =>
                    HardStopped(stopped.Remaining),
                BilledAiActionResult.ConsumeFailed failed =>
                    CreditRefusal(failed.Code, failed.Remaining),
                BilledAiActionResult.ProviderFailed failed => new ObjectResult(
                    new
                    {
                        success = false,
                        message = failed.Message,
                        retryable = failed.Retryable,
                    }
                )
                {
                    StatusCode = StatusCodes.Status502BadGateway,
                },
                BilledAiActionResult.ResourceNotFound notFound => new NotFoundObjectResult(
                    new
                    {
                        success = false,
                        message = notFound.Message,
                    }
                ),
                BilledAiActionResult.IdempotencyKeyRequired => new BadRequestObjectResult(
                    new
                    {
                        success = false,
                        code = "idempotency_key_required",
                        message = "Idempotency-Key header is required.",
                        retryable = false,
                    }
                ),
                _ => new ObjectResult(
                    new
                    {
                        success = false,
                        message = "Unexpected billed AI action result.",
                        retryable = true,
                    }
                )
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                },
            };
        }

        private static OkObjectResult OkDraft(BilledAiDraftPayload payload)
        {
            return new OkObjectResult(new
            {
                success = true,
                body = payload.Body,
                subject = payload.Subject,
                channel = payload.Channel,
            });
        }

        private static ObjectResult HardStopped(int remaining)
        {
            return CreditRefusal("channel_hard_stopped", remaining);
        }

        private static ObjectResult CreditRefusal(string code, int remaining)
        {
            return new ObjectResult(new
            {
                success = false,
                code,
                message = CreditRefusalMessage(code),
                channel = CreditChannels.Ai,
                remaining,
                requested = 1,
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }

        private static string CreditRefusalMessage(string code)
            => code switch
            {
                "channel_hard_stopped" =>
                    "You have no AI credits remaining for this action.",
                "insufficient_credits" =>
                    "You do not have enough AI credits for this action.",
                "location_required" =>
                    "A location is required for this AI action.",
                "location_not_in_account" =>
                    "That location is not part of your account.",
                _ => "This AI action could not be billed.",
            };
    }
}
