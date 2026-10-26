using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
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
        private readonly IConfiguration _configuration;

        public TwilioSmsInboundController(
            TwilioSmsInboundService inbound,
            IConfiguration configuration
        )
        {
            _inbound = inbound;
            _configuration = configuration;
        }

        [HttpPost(TwilioWebhookUrl.SmsInboundPath)]
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

            var validationUrl = TwilioWebhookUrl.ValidationUrl(
                _configuration,
                TwilioWebhookUrl.SmsInboundPath,
                Request
            );
            var signature = Request.Headers["X-Twilio-Signature"].ToString();

            var status = await _inbound.HandleAsync(
                validationUrl,
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
