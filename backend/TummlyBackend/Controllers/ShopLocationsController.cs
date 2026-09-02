using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/shop/locations")]
    [Authorize]
    public class ShopLocationsController : ControllerBase
    {
        private readonly IShopLocationRecommendationsService _recommendations;
        private readonly IRestaurantPermissionHelper _permissions;

        public ShopLocationsController(
            IShopLocationRecommendationsService recommendations,
            IRestaurantPermissionHelper permissions
        )
        {
            _recommendations = recommendations;
            _permissions = permissions;
        }

        [HttpPut("{locationId:int}/details")]
        public async Task<IActionResult> SaveDetails(
            int locationId,
            [FromBody] SaveShopLocationDetailsRequest body,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeLocationAsync(
                locationId,
                PermissionLevel.Scoped
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var saved = await _recommendations.SaveDetailsAsync(
                locationId,
                body,
                cancellationToken
            );
            if (saved == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Location was not found.",
                });
            }

            return Ok(new
            {
                success = true,
                locationId,
                basedOn = saved,
            });
        }

        [HttpGet("{locationId:int}/recommendations")]
        public async Task<IActionResult> GetRecommendations(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeLocationAsync(
                locationId,
                PermissionLevel.View
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var payload = await _recommendations.GetRecommendationsAsync(
                locationId,
                cancellationToken
            );
            return Ok(payload);
        }

        private async Task<(IActionResult? Denied, int RestaurantId)> AuthorizeLocationAsync(
            int locationId,
            PermissionLevel minimum
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return (unauthorized, 0);
            }

            if (locationId <= 0)
            {
                return (
                    BadRequest(new
                    {
                        success = false,
                        message = "locationId is required.",
                    }),
                    0
                );
            }

            var decision = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.TummlyShop,
                minimum,
                locationId
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return (denied, 0);
            }

            return (null, decision.RestaurantId);
        }
    }
}
