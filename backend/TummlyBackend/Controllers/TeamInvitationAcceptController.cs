using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.TeamPermissions;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/team-invitations")]
    public class TeamInvitationAcceptController : ControllerBase
    {
        private readonly ITeamInvitationAcceptService _accept;

        public TeamInvitationAcceptController(ITeamInvitationAcceptService accept)
        {
            _accept = accept;
        }

        [AllowAnonymous]
        [HttpGet("preview")]
        public async Task<IActionResult> Preview([FromQuery] string? invite)
        {
            int? sessionUserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                OperatorAuth.TryRequireUserId(User, out var userId);
                sessionUserId = userId;
            }

            var result = await _accept.PreviewAsync(invite, sessionUserId);
            if (result.Error != null)
            {
                return BadRequest(new { success = false, message = result.Error });
            }

            return Ok(result.Preview);
        }

        [AllowAnonymous]
        [HttpPost("credentials")]
        public async Task<IActionResult> Credentials(
            [FromBody] TeamInvitationCredentialsRequest request
        )
        {
            var error = await _accept.StoreCredentialsAndSendOtpAsync(request);
            return Map(error);
        }

        [AllowAnonymous]
        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn(
            [FromBody] TeamInvitationSignInRequest request
        )
        {
            var error = await _accept.SignInAndSendOtpAsync(request);
            return Map(error);
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] TeamInvitationVerifyOtpRequest request
        )
        {
            var result = await _accept.VerifyOtpAndAcceptAsync(request);
            if (result.Error != null)
            {
                return BadRequest(new { success = false, message = result.Error });
            }

            return Ok(result.Session);
        }

        [Authorize]
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInPlace([FromQuery] string? invite)
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var result = await _accept.AcceptInPlaceAsync(userId, invite);
            if (result.Error != null)
            {
                return BadRequest(new { success = false, message = result.Error });
            }

            return Ok(result.Session);
        }

        private IActionResult Map(string? error)
        {
            if (error == null)
            {
                return Ok(new { success = true });
            }

            return BadRequest(new { success = false, message = error });
        }
    }
}
