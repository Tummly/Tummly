using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/trust-indicators")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminTrustIndicatorsController : ControllerBase
    {
        private readonly ITrustIndicatorService _trust;

        public AdminTrustIndicatorsController(ITrustIndicatorService trust)
        {
            _trust = trust;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var item = await _trust.GetAdminViewAsync(cancellationToken);
            return Ok(new { success = true, item });
        }

        [HttpPut("items/{itemNumber:int}")]
        public async Task<IActionResult> UpdateItem(
            int itemNumber,
            [FromBody] TrustIndicatorEditableValues request,
            CancellationToken cancellationToken
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var outcome = await _trust.UpdateDraftItemAsync(
                adminId.Value,
                itemNumber,
                request,
                cancellationToken
            );
            return outcome.Succeeded
                ? Ok(new { success = true, item = outcome.Value })
                : Failure(outcome);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(CancellationToken cancellationToken)
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var outcome = await _trust.SubmitDraftAsync(adminId.Value, cancellationToken);
            return outcome.Succeeded
                ? Ok(new { success = true, item = outcome.Value })
                : Failure(outcome);
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish(
            [FromBody] AdminTrustIndicatorPublishRequest? request,
            CancellationToken cancellationToken
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            var outcome = await _trust.PublishAsync(
                adminId.Value,
                idempotencyKey,
                request?.Reason,
                cancellationToken
            );
            return outcome.Succeeded
                ? Ok(new { success = true, item = outcome.Value })
                : Failure(outcome);
        }

        private IActionResult Failure<T>(TrustIndicatorOutcome<T> outcome)
        {
            var body = new
            {
                success = false,
                code = outcome.Code,
                message = outcome.Message,
                details = outcome.Details ?? Array.Empty<string>(),
            };

            return outcome.Code switch
            {
                "not_found" => NotFound(body),
                "not_submitted" or "already_submitted" or "self_approval_denied" or "no_changes" or "publish_conflict" => Conflict(body),
                _ => BadRequest(body),
            };
        }

        private int? ResolveAdminId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    [ApiController]
    [Route("api/public/trust-indicators")]
    public sealed class PublicTrustIndicatorsController : ControllerBase
    {
        private readonly ITrustIndicatorService _trust;

        public PublicTrustIndicatorsController(ITrustIndicatorService trust)
        {
            _trust = trust;
        }

        /// <summary>Latest published, enabled, unexpired indicators in item order. Empty array when nothing is published.</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var items = await _trust.GetPublicAsync(cancellationToken);
            return Ok(items);
        }
    }
}
