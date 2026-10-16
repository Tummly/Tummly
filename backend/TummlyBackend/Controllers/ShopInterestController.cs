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
    [Route("api/shop/interest")]
    [Authorize]
    public sealed class ShopInterestController : ControllerBase
    {
        private readonly IShopInterestService _interest;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly ApplicationDbContext _context;

        public ShopInterestController(
            IShopInterestService interest,
            IRestaurantPermissionHelper permissions,
            ApplicationDbContext context
        )
        {
            _interest = interest;
            _permissions = permissions;
            _context = context;
        }

        [HttpGet("products")]
        public async Task<IActionResult> ListProducts(
            [FromQuery] int? locationId,
            CancellationToken cancellationToken
        )
        {
            var denied = await AuthorizeShopViewAsync(locationId);
            if (denied != null)
            {
                return denied;
            }

            var items = await _interest.ListActiveProductsAsync(cancellationToken);
            return Ok(new { success = true, items });
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            [FromBody] RegisterShopInterestRequest request,
            CancellationToken cancellationToken
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (request.LocationId is not > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            var gate = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.TummlyShop,
                PermissionLevel.View,
                request.LocationId.Value
            );
            var gateResult = gate.ToHttpResult();
            if (gateResult != null)
            {
                return gateResult;
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == gate.RestaurantId,
                    cancellationToken
                );
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == request.LocationId.Value,
                    cancellationToken
                );
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == userId, cancellationToken);

            if (restaurant == null || user == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Operator context was not found.",
                });
            }

            var (ok, error, message) = await _interest.RegisterAsync(
                userId,
                restaurant.Id,
                restaurant.Name,
                request,
                location?.LocationName,
                user.FullName,
                user.Email,
                cancellationToken
            );

            if (!ok)
            {
                if (
                    error != null
                    && error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return NotFound(new { success = false, message = error });
                }

                return BadRequest(new { success = false, message = error });
            }

            return Ok(new { success = true, message });
        }

        private async Task<IActionResult?> AuthorizeShopViewAsync(int? locationId)
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (locationId is not > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            var gate = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.TummlyShop,
                PermissionLevel.View,
                locationId.Value
            );
            return gate.ToHttpResult();
        }
    }
}
