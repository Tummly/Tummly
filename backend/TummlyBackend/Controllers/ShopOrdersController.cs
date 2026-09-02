using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/shop")]
    [Authorize]
    public class ShopOrdersController : ControllerBase
    {
        private readonly IShopOrderPlaceService _orders;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly ApplicationDbContext _context;

        public ShopOrdersController(
            IShopOrderPlaceService orders,
            IRestaurantPermissionHelper permissions,
            ApplicationDbContext context
        )
        {
            _orders = orders;
            _permissions = permissions;
            _context = context;
        }

        [HttpPost("orders")]
        public async Task<IActionResult> PlaceOrder(
            [FromBody] PlaceShopOrderRequest body,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeShopAsync(
                body.LocationId,
                PermissionLevel.Scoped
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var placedByName = await ResolveDisplayNameAsync(
                gate.UserId,
                cancellationToken
            );

            var result = await _orders.PlaceAsync(
                gate.RestaurantId,
                gate.UserId,
                placedByName,
                body,
                cancellationToken
            );

            if (result.Order == null)
            {
                if (result.ErrorCode == "location_not_found")
                {
                    return NotFound(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    code = result.ErrorCode,
                    message = result.ErrorMessage,
                });
            }

            return Ok(result.Order);
        }

        [HttpGet("locations/{locationId:int}/delivery-defaults")]
        public async Task<IActionResult> GetDeliveryDefaults(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeShopAsync(
                locationId,
                PermissionLevel.View
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var placedByName = await ResolveDisplayNameAsync(
                gate.UserId,
                cancellationToken
            );

            var defaults = await _orders.GetDeliveryDefaultsAsync(
                gate.RestaurantId,
                locationId,
                placedByName,
                cancellationToken
            );
            if (defaults == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Owned location was not found.",
                });
            }

            return Ok(defaults);
        }

        private async Task<string> ResolveDisplayNameAsync(
            int userId,
            CancellationToken cancellationToken
        )
        {
            var name = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.FullName)
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(name) ? "Operator" : name.Trim();
        }

        private async Task<(
            IActionResult? Denied,
            int RestaurantId,
            int UserId
        )> AuthorizeShopAsync(int? locationId, PermissionLevel minimum)
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
