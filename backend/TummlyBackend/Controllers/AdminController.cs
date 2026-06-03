using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(
            IAdminService adminService
        )
        {
            _adminService = adminService;
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
         UPDATE STATUS
         =========================================
        */

        [HttpPut("update-status")]
        public async Task<IActionResult>
            UpdateStatus(
                UpdateTrialStatusDto dto
            )
        {
            var result =
                await _adminService
                    .UpdateTrialStatusAsync(dto);

            if (!result)
            {
                return BadRequest(new
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
                    "Status updated successfully."
            });
        }

        /*
         =========================================
         APPROVE TRIAL REQUEST
         =========================================
        */

        [HttpPost("approve/{trialRequestId}")]
        public async Task<IActionResult>
            ApproveTrialRequest(
                int trialRequestId
            )
        {
            var result =
                await _adminService
                    .ApproveTrialRequestAsync(
                        trialRequestId
                    );

            return Ok(result);
        }

        /*
         =========================================
         RESEND INVITE
         =========================================
        */

        [HttpPost("resend-invite/{id}")]
        public async Task<IActionResult>
            ResendInvite(
                int id
            )
        {
            var result =
                await _adminService
                    .ResendInviteAsync(id);

            return Ok(result);
        }
        /*
 =========================================
 DECLINE REQUEST
 =========================================
*/

        [HttpPost("decline/{id}")]
        public async Task<IActionResult>
            DeclineRequest(
                int id
            )
        {
            var result =
                await _adminService
                    .DeclineRequestAsync(id);

            return Ok(result);
        }

        /*
         =========================================
         REQUEST MORE INFO
         =========================================
        */

        [HttpPost("request-more-info/{id}")]
        public async Task<IActionResult>
            RequestMoreInfo(
                int id
            )
        {
            var result =
                await _adminService
                    .RequestMoreInfoAsync(id);

            return Ok(result);
        }
    }
}