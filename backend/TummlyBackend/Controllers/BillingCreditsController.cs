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

        [HttpPut("billing-contacts")]
        public async Task<IActionResult> UpdateBillingContacts(
            [FromBody] UpdateBillingContactsRequest request
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

            var (response, error, statusCode) =
                await _billingCredits.UpdateBillingContactsAsync(
                    userId,
                    manage.RestaurantId,
                    request
                );

            if (response == null)
            {
                return StatusCode(statusCode, new
                {
                    success = false,
                    message = error,
                });
            }

            return Ok(response);
        }
    }
}
