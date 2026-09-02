using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/shop-orders")]
    [Authorize(Roles = "Admin")]
    public class AdminShopOrdersController : ControllerBase
    {
        private readonly IAdminShopOrderFulfilmentService _fulfilment;

        public AdminShopOrdersController(
            IAdminShopOrderFulfilmentService fulfilment
        )
        {
            _fulfilment = fulfilment;
        }

        [HttpGet]
        public async Task<IActionResult> ListOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? q = null,
            [FromQuery] int? restaurantId = null,
            [FromQuery] string[]? fulfilmentStatus = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var result = await _fulfilment.GetListAsync(
                    new AdminShopOrdersListQuery
                    {
                        Page = page,
                        PageSize = pageSize,
                        Q = q,
                        RestaurantId = restaurantId,
                        FulfilmentStatus =
                            fulfilmentStatus ?? Array.Empty<string>(),
                    },
                    cancellationToken
                );
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message = ex.Message,
                    }
                );
            }
        }

        [HttpGet("export.csv")]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? q = null,
            [FromQuery] int? restaurantId = null,
            [FromQuery] string[]? fulfilmentStatus = null,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var result = await _fulfilment.ExportCsvAsync(
                    new AdminShopOrdersListQuery
                    {
                        Page = page,
                        PageSize = pageSize,
                        Q = q,
                        RestaurantId = restaurantId,
                        FulfilmentStatus =
                            fulfilmentStatus ?? Array.Empty<string>(),
                    },
                    cancellationToken
                );
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message = ex.Message,
                    }
                );
            }
        }

        [HttpPatch("{id:guid}/fulfilment")]
        public async Task<IActionResult> PatchFulfilment(
            Guid id,
            [FromBody] AdminShopOrderFulfilmentPatchDto body,
            CancellationToken cancellationToken = default
        )
        {
            var result = await _fulfilment.UpdateFulfilmentAsync(
                id,
                body,
                cancellationToken
            );

            if (result.Succeeded)
            {
                return Ok(result.Order);
            }

            return result.ErrorCode switch
            {
                "order_not_found" => NotFound(
                    new
                    {
                        success = false,
                        code = result.ErrorCode,
                        message = result.ErrorMessage,
                    }
                ),
                "illegal_fulfilment_transition" => Conflict(
                    new
                    {
                        success = false,
                        code = result.ErrorCode,
                        message = result.ErrorMessage,
                    }
                ),
                _ => BadRequest(
                    new
                    {
                        success = false,
                        code = result.ErrorCode,
                        message = result.ErrorMessage,
                    }
                ),
            };
        }
    }
}
