using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/staff")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminStaffController : ControllerBase
    {
        private readonly IAdminStaffService _staff;

        public AdminStaffController(IAdminStaffService staff)
        {
            _staff = staff;
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var items = await _staff.ListAsync(cancellationToken);
            return Ok(new { success = true, items });
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] AdminStaffCreateRequest? request,
            CancellationToken cancellationToken
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var outcome = await _staff.CreateAsync(
                adminId.Value,
                request ?? new AdminStaffCreateRequest(null, null, null, null, null),
                cancellationToken
            );
            return outcome.Succeeded
                ? Ok(new { success = true, item = outcome.Value })
                : Failure(outcome);
        }

        [HttpPost("{id:int}/disable")]
        public Task<IActionResult> Disable(
            int id,
            [FromBody] AdminStaffReasonRequest? request,
            CancellationToken cancellationToken
        ) => Act(id, request?.Reason, (adminId, targetId, reason) =>
            _staff.DisableAsync(adminId, targetId, reason, cancellationToken));

        [HttpPost("{id:int}/enable")]
        public Task<IActionResult> Enable(
            int id,
            [FromBody] AdminStaffReasonRequest? request,
            CancellationToken cancellationToken
        ) => Act(id, request?.Reason, (adminId, targetId, reason) =>
            _staff.EnableAsync(adminId, targetId, reason, cancellationToken));

        [HttpPost("{id:int}/unlock")]
        public Task<IActionResult> Unlock(
            int id,
            [FromBody] AdminStaffReasonRequest? request,
            CancellationToken cancellationToken
        ) => Act(id, request?.Reason, (adminId, targetId, reason) =>
            _staff.UnlockAsync(adminId, targetId, reason, cancellationToken));

        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(
            int id,
            [FromBody] AdminStaffResetPasswordRequest? request,
            CancellationToken cancellationToken
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var outcome = await _staff.ResetPasswordAsync(
                adminId.Value,
                id,
                request?.NewPassword,
                request?.Reason,
                cancellationToken
            );
            return outcome.Succeeded
                ? Ok(new { success = true, item = outcome.Value })
                : Failure(outcome);
        }

        private async Task<IActionResult> Act(
            int targetId,
            string? reason,
            Func<int, int, string?, Task<StaffOutcome<AdminStaffMemberDto>>> action
        )
        {
            var adminId = ResolveAdminId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Admin required." });
            }

            var outcome = await action(adminId.Value, targetId, reason);
            return outcome.Succeeded
                ? Ok(new { success = true, item = outcome.Value })
                : Failure(outcome);
        }

        private IActionResult Failure<T>(StaffOutcome<T> outcome)
        {
            var body = new
            {
                success = false,
                code = outcome.Code,
                message = outcome.Message,
            };

            return outcome.Code switch
            {
                "staff_not_found" => NotFound(body),
                "email_in_use" or "cannot_disable_self" or "last_active_admin"
                    or "cannot_reset_own_password" => Conflict(body),
                _ => BadRequest(body),
            };
        }

        private int? ResolveAdminId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
