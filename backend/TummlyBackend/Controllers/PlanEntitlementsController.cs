using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/plan-entitlements")]
    public sealed class PlanEntitlementsController : ControllerBase
    {
        private readonly IPlanEntitlementsSnapshot _entitlements;
        private readonly IRestaurantPermissionHelper _permissions;

        public PlanEntitlementsController(
            IPlanEntitlementsSnapshot entitlements,
            IRestaurantPermissionHelper permissions
        )
        {
            _entitlements = entitlements;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int? locationId,
            CancellationToken cancellationToken
        )
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

            if (locationId != null)
            {
                var locationDecision = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Capture,
                    PermissionLevel.View,
                    locationId.Value
                );
                var locationDenied = locationDecision.ToHttpResult();
                if (locationDenied != null)
                {
                    return locationDenied;
                }
            }

            var snapshot = await _entitlements.GetAsync(
                decision.RestaurantId,
                locationId,
                cancellationToken
            );

            return Ok(new { success = true, entitlements = snapshot });
        }
    }
}
