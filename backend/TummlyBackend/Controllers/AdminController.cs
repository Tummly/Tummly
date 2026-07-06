using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ITrialReviewTransition _trialReviewTransition;

        public AdminController(
            IAdminService adminService,
            ITrialReviewTransition trialReviewTransition
        )
        {
            _adminService = adminService;
            _trialReviewTransition = trialReviewTransition;
        }

        /*
         =========================================
         GET ALL TRIAL REQUESTS
         =========================================
        */

        [HttpGet("trial-requests")]
        public async Task<IActionResult>
            GetAllTrialRequests()
        {
            var requests =
                await _adminService
                    .GetAllTrialRequestsAsync();

            return Ok(new
            {
                success = true,
                data = requests
            });
        }

        /*
         =========================================
         APPROVE TRIAL REQUEST
         =========================================
        */

        [HttpPost("approve/{trialRequestId}")]
        public Task<IActionResult> ApproveTrialRequest(int trialRequestId) =>
            ExecuteTransitionAsync(
                trialRequestId,
                TrialReviewDecision.Approve,
                BuildContext(null, null),
                "Trial request approved successfully."
            );

        /*
         =========================================
         RESEND INVITE
         =========================================
        */

        [HttpPost("resend-invite/{id}")]
        public Task<IActionResult> ResendInvite(int id) =>
            ExecuteTransitionAsync(
                id,
                TrialReviewDecision.ResendInvite,
                BuildContext(null, null),
                "Invite resent successfully."
            );

        /*
         =========================================
         DECLINE REQUEST
         =========================================
        */

        [HttpPost("decline/{id}")]
        public Task<IActionResult> DeclineRequest(
            int id,
            [FromBody] DeclineTrialRequestDto dto
        ) =>
            ExecuteTransitionAsync(
                id,
                TrialReviewDecision.Decline,
                BuildContext(dto.DeclineReason, null),
                "Request declined successfully"
            );

        /*
         =========================================
         REQUEST MORE INFO
         =========================================
        */

        [HttpPost("request-more-info/{id}")]
        public Task<IActionResult> RequestMoreInfo(
            int id,
            [FromBody] RequestMoreInfoDto dto
        ) =>
            ExecuteTransitionAsync(
                id,
                TrialReviewDecision.RequestMoreInfo,
                BuildContext(dto.MoreInfoMessage, null),
                "More info email sent successfully"
            );

        /*
         =========================================
         PURGE TRIAL REQUEST (QA ONLY)
         =========================================
        */

        [HttpDelete("trial-requests/{id}")]
        public async Task<IActionResult>
            PurgeTrialRequest(
                int id
            )
        {
            if (!_adminService.IsTrialPurgeEnabled())
            {
                return StatusCode(403, new
                {
                    success = false,
                    message =
                        "Trial purge is not enabled on this environment."
                });
            }

            var deleted =
                await _adminService
                    .PurgeTrialRequestAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Trial request not found."
                });
            }

            return Ok(new
            {
                success = true,
                message =
                    "Trial request and related data deleted."
            });
        }

        [HttpPost("operators/{userId}/extend-activation")]
        public async Task<IActionResult> ExtendActivation(
            int userId,
            [FromBody] ExtendActivationDto dto
        )
        {
            try
            {
                var result =
                    await _adminService.ExtendActivationAsync(userId, dto);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Operator account not found.",
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = result,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [HttpGet("operators/{userId}/activation-download")]
        public async Task<IActionResult> DownloadActivationAsset(int userId)
        {
            try
            {
                var result =
                    await _adminService.GetActivationDownloadAsync(userId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Operator account not found.",
                    });
                }

                return File(
                    result.Value.Content,
                    result.Value.ContentType,
                    result.Value.FileName
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        private async Task<IActionResult> ExecuteTransitionAsync(
            int trialRequestId,
            TrialReviewDecision decision,
            TrialReviewContext context,
            string successMessage
        )
        {
            try
            {
                var result = await _trialReviewTransition.ApplyTransitionAsync(
                    trialRequestId,
                    decision,
                    context
                );

                return Ok(BuildTransitionResponse(
                    successMessage,
                    result,
                    emailDispatched: true
                ));
            }
            catch (TrialReviewEmailDispatchException ex)
            {
                return Ok(BuildTransitionResponse(
                    successMessage,
                    ex.Result,
                    emailDispatched: false,
                    emailWarning: TrialReviewEmailDispatchException.DefaultMessage
                ));
            }
            catch (IllegalTrialTransitionException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (TrialReviewConcurrentModificationException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        private static object BuildTransitionResponse(
            string successMessage,
            TrialReviewResult result,
            bool emailDispatched,
            string? emailWarning = null
        ) =>
            new
            {
                success = true,
                message = successMessage,
                emailDispatched,
                emailWarning = emailDispatched ? null : emailWarning,
                newStatus = result.NewStatus.ToWireString(),
                setupLink = result.SetupLink,
                expiresAt = result.InviteExpiresAt,
            };

        private TrialReviewContext BuildContext(
            string? reason,
            string? adminNotes
        )
        {
            var adminIdentity =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(adminIdentity))
            {
                throw new ArgumentException(
                    "Admin identity could not be resolved from the current session."
                );
            }

            return new TrialReviewContext(
                adminIdentity,
                reason,
                adminNotes
            );
        }
    }
}
