using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/admin/shop-orders")]
    [Authorize(Roles = "Admin")]
    public class AdminShopOrdersController : ControllerBase
    {
        private readonly IAdminShopOrderFulfilmentService _fulfilment;
        private readonly IPrintReadyQrMaterialsService _printReadyQrMaterials;

        public AdminShopOrdersController(
            IAdminShopOrderFulfilmentService fulfilment,
            IPrintReadyQrMaterialsService printReadyQrMaterials
        )
        {
            _fulfilment = fulfilment;
            _printReadyQrMaterials = printReadyQrMaterials;
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
                    BuildListQuery(page, pageSize, q, restaurantId, fulfilmentStatus),
                    cancellationToken
                );
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return QueryBadRequest(ex);
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
                // page/pageSize accepted on the wire (Guests export pattern) but
                // ignored for warehouse dump — ExportCsvAsync returns all matches.
                var result = await _fulfilment.ExportCsvAsync(
                    BuildListQuery(page, pageSize, q, restaurantId, fulfilmentStatus),
                    cancellationToken
                );
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (ArgumentException ex)
            {
                return QueryBadRequest(ex);
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

        [HttpGet("{id:guid}/print-assets/{qrType}/download")]
        public async Task<IActionResult> DownloadPrintAsset(
            Guid id,
            string qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!TryParsePhysicalQrType(qrType, out var parsed))
            {
                return NotFound();
            }

            try
            {
                var download =
                    await _printReadyQrMaterials.DownloadShopOrderAsync(
                        id,
                        parsed,
                        cancellationToken
                    );
                return download == null
                    ? NotFound()
                    : File(
                        download.Content,
                        download.ContentType,
                        download.FileName
                    );
            }
            catch (PrintReadyQrNotReadyException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                    status = ex.Status.ToString(),
                });
            }
        }

        [HttpPost("{id:guid}/print-assets/{qrType}/retry")]
        public async Task<IActionResult> RetryPrintAsset(
            Guid id,
            string qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!TryParsePhysicalQrType(qrType, out var parsed))
            {
                return NotFound();
            }

            try
            {
                var asset =
                    await _printReadyQrMaterials.RetryShopOrderAsync(
                        id,
                        parsed,
                        cancellationToken
                    );
                return asset == null ? NotFound() : Ok(asset);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        private static AdminShopOrdersListQuery BuildListQuery(
            int page,
            int pageSize,
            string? q,
            int? restaurantId,
            string[]? fulfilmentStatus
        )
        {
            return new AdminShopOrdersListQuery
            {
                Page = page,
                PageSize = pageSize,
                Q = q,
                RestaurantId = restaurantId,
                FulfilmentStatus = fulfilmentStatus ?? Array.Empty<string>(),
            };
        }

        private static bool TryParsePhysicalQrType(
            string value,
            out QrType qrType
        )
        {
            return Enum.TryParse(value, ignoreCase: true, out qrType)
                && qrType
                    is QrType.TableTent
                    or QrType.WindowSticker
                    or QrType.OfferCard;
        }

        private BadRequestObjectResult QueryBadRequest(ArgumentException ex)
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
}
