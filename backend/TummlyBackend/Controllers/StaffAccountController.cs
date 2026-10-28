using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/staff/me")]
    [Authorize(Roles = "Admin,Support")]
    public sealed class StaffAccountController : ControllerBase
    {
        private readonly IStaffAccountService _accounts;

        public StaffAccountController(IStaffAccountService accounts)
        {
            _accounts = accounts;
        }

        [HttpPost("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] StaffChangePasswordRequest? request,
            CancellationToken cancellationToken
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Staff sign-in required." });
            }

            var outcome = await _accounts.ChangeOwnPasswordAsync(
                adminId.Value,
                request?.CurrentPassword,
                request?.NewPassword,
                cancellationToken
            );

            if (outcome.Succeeded)
            {
                return Ok(new { success = true });
            }

            var body = new
            {
                success = false,
                code = outcome.Code,
                message = outcome.Message,
            };

            return outcome.Code == "staff_not_found" ? NotFound(body) : BadRequest(body);
        }

        private int? ResolveAdminId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
