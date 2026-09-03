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
        private readonly IShopOrdersListService _ordersList;
        private readonly IGuestsEffectiveLocationService _effectiveLocations;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly ApplicationDbContext _context;
        private readonly IShopOrderCancelReorderService _cancelReorder;

        public ShopOrdersController(
            IShopOrderPlaceService orders,
            IShopMaterialsOrderPaySession paySessions,
            IShopOrdersListService ordersList,
            IGuestsEffectiveLocationService effectiveLocations,
            IRestaurantPermissionHelper permissions,
            ApplicationDbContext context,
            IShopOrderCancelReorderService cancelReorder
        )
        {
            _orders = orders;
            _paySessions = paySessions;
            _ordersList = ordersList;
            _effectiveLocations = effectiveLocations;
            _permissions = permissions;
            _context = context;
            _cancelReorder = cancelReorder;
        }

        [HttpGet("orders")]
        public async Task<IActionResult> ListOrders(
            [FromQuery] int locationId,
            [FromQuery] string? q = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string sort = "newest",
            [FromQuery] string[]? fulfilmentStatus = null,
            [FromQuery] string[]? paymentStatus = null,
            [FromQuery] string[]? materialType = null,
            [FromQuery] string? orderDatePreset = null,
            [FromQuery] DateTime? orderDateFrom = null,
            [FromQuery] DateTime? orderDateTo = null,
            [FromQuery] string? locationScope = "all",
            [FromQuery] int[]? locationIds = null,
            [FromQuery] int utcOffsetMinutes = 0,
            CancellationToken cancellationToken = default
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

            try
            {
                var effectiveLocations = await _effectiveLocations.ResolveAsync(
                    gate.LocationIds,
                    gate.ShellLocation!,
                    locationScope,
                    locationIds
                );
                var effectiveDenied = effectiveLocations.ToHttpResult();
                if (effectiveDenied != null)
                {
                    return effectiveDenied;
                }

                var result = await _ordersList.GetListAsync(
                    new ShopOrdersListQuery
                    {
                        RestaurantId = gate.RestaurantId,
                        ShellLocationId = locationId,
                        LocationIds = effectiveLocations.LocationIds!,
                        Q = q,
                        Page = page,
                        PageSize = pageSize,
                        Sort = sort,
                        FulfilmentStatus = fulfilmentStatus ?? Array.Empty<string>(),
                        PaymentStatus = paymentStatus ?? Array.Empty<string>(),
                        MaterialType = materialType ?? Array.Empty<string>(),
                        OrderDatePreset = orderDatePreset,
                        OrderDateFrom = orderDateFrom,
                        OrderDateTo = orderDateTo,
                        UtcOffsetMinutes = utcOffsetMinutes,
                    },
                    cancellationToken
                );

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
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

            if (!gate.LocationIds.Contains(order.LocationId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have access to this location.",
                });
            }

            return Ok(
                await ShopOrderDtoMapper.MapOperatorDetailAsync(
                    _context,
                    order,
                    cancellationToken
                )
            );
        }

        [HttpPost("orders/{orderId:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(
            Guid orderId,
            [FromBody] CancelShopOrderRequest body,
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

            if (!gate.LocationIds.Contains(body.LocationId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have access to this location.",
                });
            }

            var result = await _cancelReorder.CancelAsync(
                gate.RestaurantId,
                gate.UserId,
                orderId,
                body.LocationId,
                body.Reason,
                cancellationToken
            );

            if (result.Order == null)
            {
                if (result.ErrorCode == "order_not_found")
                {
                    return NotFound(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                    });
                }

                if (result.ErrorCode == "shop_order_not_cancellable")
                {
                    return Conflict(new
                    {
                        success = false,
                        code = result.ErrorCode,
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

        [HttpPost("orders/{orderId:guid}/reorder")]
        public async Task<IActionResult> ReorderOrder(
            Guid orderId,
            [FromQuery] int locationId,
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

            if (!gate.LocationIds.Contains(locationId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have access to this location.",
                });
            }

            var result = await _cancelReorder.BuildReorderPrefillAsync(
                gate.RestaurantId,
                orderId,
                locationId,
                cancellationToken
            );

            if (result.Prefill == null)
            {
                if (result.ErrorCode == "order_not_found")
                {
                    return NotFound(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                    });
                }

                if (result.ErrorCode == "catalog_sku_unavailable")
                {
                    return Conflict(new
                    {
                        success = false,
                        code = result.ErrorCode,
                        message = result.ErrorMessage,
                        unavailableSkuIds = result.UnavailableSkuIds,
                    });
                }

                if (result.ErrorCode == "shop_order_not_reorderable")
                {
                    return Conflict(new
                    {
                        success = false,
                        code = result.ErrorCode,
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

            return Ok(result.Prefill);
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

            if (!gate.LocationIds.Contains(order.LocationId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have access to this location.",
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

        private async Task<ShopGate> AuthorizeShopAsync(
            int? locationId,
            PermissionLevel minimum
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out var userId);
            if (unauthorized != null)
            {
                return ShopGate.FromDenied(unauthorized);
            }

            if (locationId is not > 0)
            {
                return ShopGate.FromDenied(
                    BadRequest(new
                    {
                        success = false,
                        message = "locationId is required.",
                    })
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
                return ShopGate.FromDenied(denied);
            }

            return ShopGate.FromAllowed(
                decision.RestaurantId,
                userId,
                decision.LocationIds,
                decision.Location!
            );
        }

        private sealed record ShopGate(
            IActionResult? Denied,
            int RestaurantId,
            int UserId,
            IReadOnlyList<int> LocationIds,
            RestaurantLocation? ShellLocation
        )
        {
            public static ShopGate FromDenied(IActionResult denied)
            {
                return new ShopGate(denied, 0, 0, [], null);
            }

            public static ShopGate FromAllowed(
                int restaurantId,
                int userId,
                IReadOnlyList<int> locationIds,
                RestaurantLocation shellLocation
            )
            {
                return new ShopGate(null, restaurantId, userId, locationIds, shellLocation);
            }
        }
    }
}
