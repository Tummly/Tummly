using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Admin queue for Locations saved as Draft for a possible duplicate
    /// premises. Admin only: the matched Locations may be in other Workspaces.
    /// </summary>
    [ApiController]
    [Route("api/admin/premises-reviews")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminPremisesReviewController : ControllerBase
    {
        private readonly IAdminPremisesReviewService _reviews;

        public AdminPremisesReviewController(IAdminPremisesReviewService reviews)
        {
            _reviews = reviews;
        }

        [HttpGet]
        public async Task<IActionResult> ListPending(CancellationToken cancellationToken)
        {
            var items = await _reviews.ListPendingAsync(cancellationToken);
            return Ok(new { success = true, items });
        }

        [HttpPost("{locationId:int}/clear")]
        public Task<IActionResult> Clear(
            int locationId,
            [FromBody] AdminPremisesReviewResolveRequest request,
            CancellationToken cancellationToken
        ) => ResolveAsync(locationId, clear: true, request, cancellationToken);

        [HttpPost("{locationId:int}/reject")]
        public Task<IActionResult> Reject(
            int locationId,
            [FromBody] AdminPremisesReviewResolveRequest request,
            CancellationToken cancellationToken
        ) => ResolveAsync(locationId, clear: false, request, cancellationToken);

        private async Task<IActionResult> ResolveAsync(
            int locationId,
            bool clear,
            AdminPremisesReviewResolveRequest? request,
            CancellationToken cancellationToken
        )
        {
            if (
                !int.TryParse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out var adminId
                )
            )
            {
                return Unauthorized(new { success = false, message = "Invalid token." });
            }

            var adminIdentity =
                User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(adminIdentity))
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message = "Admin identity could not be resolved from the current session.",
                    }
                );
            }

            var result = await _reviews.ResolveAsync(
                locationId,
                clear,
                request?.Reason,
                adminId,
                adminIdentity,
                cancellationToken
            );

            return result switch
            {
                AdminPremisesReviewResolveResult.Resolved => Ok(new { success = true }),
                AdminPremisesReviewResolveResult.NotFound => NotFound(
                    new { success = false, message = "Location not found." }
                ),
                AdminPremisesReviewResolveResult.ReasonRequired => BadRequest(
                    new
                    {
                        success = false,
                        code = "reason_required",
                        message = "A reason is required.",
                    }
                ),
                AdminPremisesReviewResolveResult.NotPending => Conflict(
                    new
                    {
                        success = false,
                        code = "not_pending_review",
                        message = "This location is not waiting for review.",
                    }
                ),
                _ => StatusCode(StatusCodes.Status500InternalServerError),
            };
        }
    }
}
