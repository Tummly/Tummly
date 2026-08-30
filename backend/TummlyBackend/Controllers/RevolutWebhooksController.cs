using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public sealed class RevolutWebhooksController : ControllerBase
    {
        private readonly IRevolutWebhookService _webhooks;

        public RevolutWebhooksController(IRevolutWebhookService webhooks)
        {
            _webhooks = webhooks;
        }

        [HttpPost("/api/webhooks/revolut")]
        public async Task<IActionResult> Receive(
            CancellationToken cancellationToken
        )
        {
            Request.EnableBuffering();
            string rawBody;
            using (
                var reader = new StreamReader(
                    Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true
                )
            )
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
            }

            var signature = Request.Headers["Revolut-Signature"].ToString();
            var timestamp = Request
                .Headers["Revolut-Request-Timestamp"]
                .ToString();

            var result = await _webhooks.HandleAsync(
                rawBody,
                signature,
                timestamp,
                cancellationToken
            );

            return result.Status switch
            {
                RevolutWebhookHandleStatus.Accepted
                or RevolutWebhookHandleStatus.Replay => NoContent(),
                RevolutWebhookHandleStatus.BadSignature => Unauthorized(),
                RevolutWebhookHandleStatus.RetryLater => StatusCode(
                    StatusCodes.Status503ServiceUnavailable
                ),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
            };
        }
    }
}
