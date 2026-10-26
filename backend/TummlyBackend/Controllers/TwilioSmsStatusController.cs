using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public sealed class TwilioSmsStatusController : ControllerBase
    {
        private readonly TwilioSmsStatusService _status;
        private readonly IConfiguration _configuration;

        public TwilioSmsStatusController(
            TwilioSmsStatusService status,
            IConfiguration configuration
        )
        {
            _status = status;
            _configuration = configuration;
        }

        [HttpPost(TwilioWebhookUrl.SmsStatusPath)]
        public async Task<IActionResult> Status(CancellationToken cancellationToken)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            var parameters = form.Keys.ToDictionary(
                key => key,
                key => form[key].ToString(),
                StringComparer.Ordinal
            );

            var validationUrl = TwilioWebhookUrl.ValidationUrl(
                _configuration,
                TwilioWebhookUrl.SmsStatusPath,
                Request
            );
            var signature = Request.Headers["X-Twilio-Signature"].ToString();

            var status = await _status.HandleAsync(
                validationUrl,
                parameters,
                signature,
                cancellationToken
            );

            if (status == TwilioSmsStatusHandleStatus.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            return NoContent();
        }
    }
}
