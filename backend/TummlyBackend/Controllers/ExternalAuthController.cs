using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/auth/external")]
    public class ExternalAuthController : ControllerBase
    {
        private readonly IExternalAuthService _externalAuth;

        public ExternalAuthController(IExternalAuthService externalAuth)
        {
            _externalAuth = externalAuth;
        }

        [HttpGet("{provider}/start")]
        public async Task<IActionResult> Start(
            string provider,
            [FromQuery] string? returnPath,
            CancellationToken ct
        )
        {
            var url = await _externalAuth.BuildStartRedirectAsync(
                provider,
                returnPath,
                ct
            );
            return Redirect(url);
        }

        [HttpGet("{provider}/callback")]
        public async Task<IActionResult> Callback(
            string provider,
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            CancellationToken ct
        )
        {
            var url = await _externalAuth.HandleCallbackAsync(
                provider,
                code,
                state,
                error,
                ct
            );
            return Redirect(url);
        }

        [HttpPost("exchange")]
        public async Task<IActionResult> Exchange(
            [FromBody] ExternalAuthExchangeDto dto,
            CancellationToken ct
        )
        {
            try
            {
                var result = await _externalAuth.ExchangeSignInAsync(dto, ct);
                return Ok(result);
            }
            catch (ActivationExpiredException ex)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        activationExpired = true,
                        message = ex.Message,
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("accept-terms")]
        public async Task<IActionResult> AcceptTerms(
            [FromBody] ExternalAuthAcceptTermsDto dto,
            CancellationToken ct
        )
        {
            try
            {
                var session = await _externalAuth.AcceptTermsAsync(dto, ct);
                return Ok(new { success = true, data = session });
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { success = false, message = ex.Message }
                );
            }
        }
    }
}
