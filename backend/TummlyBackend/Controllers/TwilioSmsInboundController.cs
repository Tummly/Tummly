using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public sealed class TwilioSmsInboundController : ControllerBase
    {
        private const string EmptyTwiml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response></Response>";

        private readonly TwilioSmsInboundService _inbound;

        public TwilioSmsInboundController(TwilioSmsInboundService inbound)
        {
            _inbound = inbound;
        }

        [HttpPost("/api/webhooks/twilio/sms-inbound")]
        public async Task<IActionResult> Inbound(
            CancellationToken cancellationToken
        )
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            var parameters = form.Keys.ToDictionary(
                key => key,
                key => form[key].ToString(),
                StringComparer.Ordinal
            );

            var requestUrl =
                $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";
            var signature = Request.Headers["X-Twilio-Signature"].ToString();

            var status = await _inbound.HandleAsync(
                requestUrl,
                parameters,
                signature,
                cancellationToken
            );

            if (status == TwilioSmsInboundStatus.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            return Content(EmptyTwiml, "text/xml");
        }
    }
}
