using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Billing;
using TummlyBackend.DTOs.TeamPermissions;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/team-permissions")]
    [Authorize]
    public class TeamPermissionsController : ControllerBase
    {
        private readonly ITeamPermissionsService _teamPermissions;
        private readonly IRestaurantPermissionHelper _permissions;

        public TeamPermissionsController(
            ITeamPermissionsService teamPermissions,
            IRestaurantPermissionHelper permissions
        )
        {
            _teamPermissions = teamPermissions;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.View
            );
            var forbidden = decision.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            var actorCanManage =
                manage.Status == RestaurantPermissionStatus.Allowed;

            var page = await _teamPermissions.GetPageAsync(
                userId,
                decision.RestaurantId,
                actorCanManage
            );
            if (page == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                });
            }

            return Ok(page);
        }

        [HttpGet("access-activity")]
        public async Task<IActionResult> GetAccessActivity(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var team = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.View
            );
            var teamDenied = team.ToForbiddenResult();
            if (teamDenied != null)
            {
                return teamDenied;
            }

            var privacy = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.PrivacyConsent,
                PermissionLevel.View
            );
            var privacyDenied = privacy.ToForbiddenResult();
            if (privacyDenied != null)
            {
                return privacyDenied;
            }

            var list = await _teamPermissions.GetAccessActivityAsync(
                team.RestaurantId,
                page,
                pageSize
            );
            return Ok(list);
        }

        [HttpPatch("members/{membershipId:int}/role")]
        public async Task<IActionResult> UpdateRole(
            int membershipId,
            [FromBody] UpdateMemberRoleRequest request
        )
        {
            return await MutateAsync(
                membershipId,
                (userId, restaurantId, canManage) =>
                    _teamPermissions.UpdateRoleAsync(
                        userId,
                        restaurantId,
                        canManage,
                        membershipId,
                        request.PermissionRole
                    )
            );
        }

        [HttpPatch("members/{membershipId:int}/location-scope")]
        public async Task<IActionResult> UpdateLocationScope(
            int membershipId,
            [FromBody] UpdateMemberLocationScopeRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var error = await _teamPermissions.UpdateLocationScopeAsync(
                userId,
                decision.RestaurantId,
                true,
                membershipId,
                decision.LocationIds,
                request.LocationScope,
                request.NamedLocationIds
            );
            return MapWrite(error);
        }

        [HttpPost("members/{membershipId:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int membershipId)
        {
            return await MutateAsync(
                membershipId,
                (userId, restaurantId, canManage) =>
                    _teamPermissions.DeactivateAsync(
                        userId,
                        restaurantId,
                        canManage,
                        membershipId
                    )
            );
        }

        [HttpPost("members/{membershipId:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int membershipId)
        {
            return await MutateWriteResultAsync(
                (userId, restaurantId, canManage) =>
                    _teamPermissions.ReactivateAsync(
                        userId,
                        restaurantId,
                        canManage,
                        membershipId
                    )
            );
        }

        [HttpDelete("members/{membershipId:int}")]
        public async Task<IActionResult> Remove(int membershipId)
        {
            return await MutateAsync(
                membershipId,
                (userId, restaurantId, canManage) =>
                    _teamPermissions.RemoveAsync(
                        userId,
                        restaurantId,
                        canManage,
                        membershipId
                    )
            );
        }

        [HttpPut("matrix")]
        public async Task<IActionResult> UpdateMatrix(
            [FromBody] UpdateAdminMatrixRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var error = await _teamPermissions.UpdateAdminMatrixAsync(
                userId,
                decision.RestaurantId,
                request.AdminCells
            );
            return MapWrite(error);
        }

        [HttpPost("invitations")]
        public async Task<IActionResult> SendInvite(
            [FromBody] SendTeamInvitationRequest request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await _teamPermissions.SendInviteAsync(
                userId,
                decision.RestaurantId,
                true,
                decision.LocationIds,
                request
            );
            return MapWrite(result);
        }

        [HttpPost("invitations/{invitationId:int}/resend")]
        public async Task<IActionResult> ResendInvite(int invitationId)
        {
            return await MutateInvitationAsync(
                invitationId,
                (userId, restaurantId, canManage) =>
                    _teamPermissions.ResendInviteAsync(
                        userId,
                        restaurantId,
                        canManage,
                        invitationId
                    )
            );
        }

        [HttpDelete("invitations/{invitationId:int}")]
        public async Task<IActionResult> RevokeInvite(int invitationId)
        {
            return await MutateInvitationAsync(
                invitationId,
                (userId, restaurantId, canManage) =>
                    _teamPermissions.RevokeInviteAsync(
                        userId,
                        restaurantId,
                        canManage,
                        invitationId
                    )
            );
        }

        private async Task<IActionResult> MutateInvitationAsync(
            int invitationId,
            Func<int, int, bool, Task<string?>> write
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var error = await write(userId, decision.RestaurantId, true);
            return MapWrite(error);
        }

        private async Task<IActionResult> MutateWriteResultAsync(
            Func<int, int, bool, Task<TeamPermissionsWriteResult>> write
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var result = await write(userId, decision.RestaurantId, true);
            return MapWrite(result);
        }

        private async Task<IActionResult> MutateAsync(
            int membershipId,
            Func<int, int, bool, Task<string?>> write
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.TeamPermissions,
                PermissionLevel.Manage
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            var error = await write(userId, decision.RestaurantId, true);
            return MapWrite(error);
        }

        private IActionResult MapWrite(TeamPermissionsWriteResult result)
        {
            if (result.Code == TeamMemberCapGate.CapReachedCode)
            {
                return Conflict(new
                {
                    success = false,
                    code = result.Code,
                    message = result.Error,
                    cap = result.Cap,
                    current = result.Current,
                });
            }

            return MapWrite(result.Error);
        }

        private IActionResult MapWrite(string? error)
        {
            if (error == null)
            {
                return NoContent();
            }

            if (error == TeamMemberCapGate.UnavailableMessage)
            {
                return Conflict(new
                {
                    success = false,
                    message = error,
                });
            }

            if (error == "forbidden")
            {
                return new ObjectResult(new
                {
                    success = false,
                    message = "You do not have access to this restaurant.",
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            }

            if (error == "matrix-edit-disabled")
            {
                return new ObjectResult(new
                {
                    success = false,
                    message = "Permission matrix editing is disabled.",
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            }

            if (error is "Restaurant not found." or "Member not found." or "Invitation not found.")
            {
                return NotFound(new
                {
                    success = false,
                    message = error,
                });
            }

            return BadRequest(new
            {
                success = false,
                message = error,
            });
        }
    }
}
