using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public sealed class ResendWebhooksController : ControllerBase
    {
        private readonly IResendWebhookService _service;

        public ResendWebhooksController(IResendWebhookService service)
        {
            _service = service;
        }

        [HttpPost("/api/webhooks/resend")]
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

            var status = await _service.HandleAsync(
                rawBody,
                Request.Headers["svix-id"].ToString(),
                Request.Headers["svix-timestamp"].ToString(),
                Request.Headers["svix-signature"].ToString(),
                cancellationToken
            );

            return status switch
            {
                ResendWebhookHandleStatus.Accepted => Ok(),
                ResendWebhookHandleStatus.BadSignature => Unauthorized(),
                ResendWebhookHandleStatus.Misconfigured => StatusCode(
                    StatusCodes.Status503ServiceUnavailable
                ),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
            };
        }
    }
}
