using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/billing-credits")]
    [Authorize]
    public class BillingCreditsController : ControllerBase
    {
        private readonly IBillingCreditsService _billingCredits;
        private readonly IRestaurantPermissionHelper _permissions;

        public BillingCreditsController(
            IBillingCreditsService billingCredits,
            IRestaurantPermissionHelper permissions
        )
        {
            _billingCredits = billingCredits;
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
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = decision.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var actorCanManage =
                manage.Status == RestaurantPermissionStatus.Allowed;

            var page = await _billingCredits.GetPageAsync(
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

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var decision = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.View
            );
            var forbidden = decision.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            var usage = await _billingCredits.GetUsageAsync(decision.RestaurantId);
            if (usage == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant not found.",
                });
            }

            return Ok(usage);
        }

        [HttpPost("plan-change")]
        public async Task<IActionResult> PostPlanChange(
            [FromBody] PlanChangeRequestDto request
        )
        {
            var unauthorized =
                OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var manage = await _permissions.AuthorizeAsync(
                User,
                OperatorAreaIds.BillingCredits,
                PermissionLevel.Manage
            );
            var forbidden = manage.ToForbiddenResult();
            if (forbidden != null)
            {
                return forbidden;
            }

            try
            {
                var result = await _billingCredits.SubmitPlanChangeAsync(
                    userId,
                    manage.RestaurantId,
                    request
                );
                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Restaurant not found.",
                    });
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "invalid-target")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Pilot is not a valid plan-change target.",
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "forbidden")
            {
                return Forbid();
            }
        }
    }
}
