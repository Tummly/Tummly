using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/shop")]
    [Authorize]
    public class ShopOrdersController : ControllerBase
    {
        private readonly IShopOrderPlaceService _orders;
        private readonly IShopMaterialsOrderPaySession _paySessions;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly ApplicationDbContext _context;

        public ShopOrdersController(
            IShopOrderPlaceService orders,
            IShopMaterialsOrderPaySession paySessions,
            IRestaurantPermissionHelper permissions,
            ApplicationDbContext context
        )
        {
            _orders = orders;
            _paySessions = paySessions;
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

            var paidWriteDeny = await OperatorBillingLockGate.EvaluatePaidWriteDenyAsync(
                _context,
                gate.RestaurantId,
                cancellationToken
            );
            if (paidWriteDeny != null)
            {
                return OperatorBillingLockGate.Forbidden(paidWriteDeny);
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

        [HttpGet("orders/{orderId:guid}")]
        public async Task<IActionResult> GetOrder(
            Guid orderId,
            [FromQuery] int? locationId,
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

            var order = await _context.ShopOrders
                .AsNoTracking()
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == orderId
                        && row.RestaurantId == gate.RestaurantId,
                    cancellationToken
                );
            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Shop order was not found.",
                });
            }

            if (locationId is > 0 && order.LocationId != locationId.Value)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Shop order was not found.",
                });
            }

            return Ok(ShopOrderDtoMapper.Map(order));
        }

        [HttpPost("orders/{orderId:guid}/pay")]
        public async Task<IActionResult> PayOrder(
            Guid orderId,
            [FromQuery] int? locationId,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeShopAsync(
                locationId,
                PermissionLevel.Scoped
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var paidWriteDeny = await OperatorBillingLockGate.EvaluatePaidWriteDenyAsync(
                _context,
                gate.RestaurantId,
                cancellationToken
            );
            if (paidWriteDeny != null)
            {
                return OperatorBillingLockGate.Forbidden(paidWriteDeny);
            }

            if (
                !Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader)
                || string.IsNullOrWhiteSpace(keyHeader)
            )
            {
                return BadRequest(new
                {
                    success = false,
                    code = "idempotency_key_required",
                    message = "Idempotency-Key header is required.",
                });
            }

            var order = await _context.ShopOrders
                .Include(row => row.Lines)
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == orderId
                        && row.RestaurantId == gate.RestaurantId,
                    cancellationToken
                );
            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Shop order was not found.",
                });
            }

            if (locationId is > 0 && order.LocationId != locationId.Value)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Shop order was not found.",
                });
            }

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == gate.RestaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Billing account was not found.",
                });
            }

            if (string.IsNullOrWhiteSpace(billingAccount.RevolutCustomerId))
            {
                return BadRequest(new
                {
                    success = false,
                    code = "revolut_customer_required",
                    message = "revolut_customer_required",
                });
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == gate.RestaurantId,
                    cancellationToken
                );
            if (restaurant == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Restaurant was not found.",
                });
            }

            try
            {
                var checkoutUrl = await _paySessions.StartAsync(
                    billingAccount,
                    restaurant.AccountType,
                    order,
                    keyHeader.ToString().Trim(),
                    cancellationToken
                );

                return Ok(new ShopOrderPayResponseDto
                {
                    Outcome = "pay",
                    RedirectUrl = checkoutUrl,
                });
            }
            catch (RevolutMerchantNotReadyException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    code = ex.Code,
                    message = ex.Code,
                });
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "idempotency_target_mismatch"
            )
            {
                return Conflict(new
                {
                    success = false,
                    code = "idempotency_target_mismatch",
                    message =
                        "Idempotency-Key was already used for a different shop order.",
                });
            }
            catch (InvalidOperationException ex) when (
                ex.Message
                    is "revolut_customer_required"
                        or "shop_order_not_payable"
                        or RevolutHostedCheckoutRedirectUrls.InvalidHostErrorCode
            )
            {
                return BadRequest(new
                {
                    success = false,
                    code = ex.Message,
                    message = ex.Message,
                });
            }
            catch (InvalidOperationException ex) when (
                ex.Message is RevolutMerchantCreateGate.VatNotReady
            )
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    code = ex.Message,
                    message = ex.Message,
                });
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "revolut_http_error"
                    || ex.Message == "Frontend:BaseUrl is not configured."
            )
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    success = false,
                    code = ex.Message,
                    message = ex.Message,
                });
            }
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
