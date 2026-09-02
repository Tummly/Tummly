using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/shop/cart")]
    [Authorize]
    public class ShopCartController : ControllerBase
    {
        private readonly IShopCartService _carts;
        private readonly IRestaurantPermissionHelper _permissions;

        public ShopCartController(
            IShopCartService carts,
            IRestaurantPermissionHelper permissions
        )
        {
            _carts = carts;
            _permissions = permissions;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart(
            [FromQuery] int? locationId,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeCartAsync(
                locationId,
                PermissionLevel.View
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var cart = await _carts.GetCartAsync(
                gate.RestaurantId,
                locationId!.Value,
                gate.UserId,
                cancellationToken
            );
            return Ok(cart);
        }

        [HttpPut("lines")]
        public async Task<IActionResult> UpsertLine(
            [FromBody] UpsertShopCartLineRequest body,
            CancellationToken cancellationToken
        )
        {
            // Scoped minimum: Manage and Scoped cells may mutate (lock 10).
            // View does not meet Scoped via DefaultPermissionMatrix.Meets.
            var gate = await AuthorizeCartAsync(
                body.LocationId,
                PermissionLevel.Scoped
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            if (string.IsNullOrWhiteSpace(body.SkuId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "skuId is required.",
                });
            }

            var cart = await _carts.UpsertLineAsync(
                gate.RestaurantId,
                body.LocationId,
                gate.UserId,
                body.SkuId.Trim(),
                body.Quantity,
                cancellationToken
            );
            if (cart == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Catalog skuId is invalid or quantity is below minOrderQty.",
                });
            }

            return Ok(cart);
        }

        [HttpDelete("lines/{skuId}")]
        public async Task<IActionResult> RemoveLine(
            string skuId,
            [FromQuery] int? locationId,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeCartAsync(
                locationId,
                PermissionLevel.Scoped
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var cart = await _carts.RemoveLineAsync(
                gate.RestaurantId,
                locationId!.Value,
                gate.UserId,
                skuId,
                cancellationToken
            );
            return Ok(cart);
        }

        private async Task<(
            IActionResult? Denied,
            int RestaurantId,
            int UserId
        )> AuthorizeCartAsync(int? locationId, PermissionLevel minimum)
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return (unauthorized, 0, 0);
            }

            if (locationId is not > 0)
            {
                return (
                    BadRequest(new
                    {
                        success = false,
                        message = "locationId is required.",
                    }),
                    0,
                    0
                );
            }

            var decision = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.TummlyShop,
                minimum,
                locationId.Value
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return (denied, 0, 0);
            }

            return (null, decision.RestaurantId, userId);
        }
    }
}
