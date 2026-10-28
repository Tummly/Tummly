using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;
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
            if (!TryResolveStaff(out var staffId, out var adminIdentity, out var actorError))
            {
                return actorError;
            }

            var result = await _fulfilment.UpdateFulfilmentAsync(
                id,
                body,
                staffId,
                adminIdentity,
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

        [HttpPost("{id:guid}/production-started")]
        public async Task<IActionResult> MarkProductionStarted(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            if (!TryResolveStaff(out var staffId, out var adminIdentity, out var actorError))
            {
                return actorError;
            }

            var result = await _fulfilment.MarkProductionStartedAsync(
                id,
                staffId,
                adminIdentity,
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

        [HttpPost("{id:guid}/force-cancel")]
        public async Task<IActionResult> ForceCancel(
            Guid id,
            [FromBody] AdminShopForceCancelRequest body,
            CancellationToken cancellationToken = default
        )
        {
            if (!TryResolveStaff(out var staffId, out var adminIdentity, out var actorError))
            {
                return actorError;
            }

            var result = await _fulfilment.ForceCancelAsync(
                id,
                body,
                staffId,
                adminIdentity,
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
                "in_transit" or "delivered" or "shop_order_not_cancellable" =>
                    Conflict(
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
                    code = "print_asset_not_ready",
                    message = ex.Message,
                    status = ex.Status.ToString(),
                });
            }
            catch (PrintReadyQrNotApprovedException ex)
            {
                return Conflict(new
                {
                    success = false,
                    code = "print_asset_not_approved",
                    message = ex.Message,
                });
            }
        }

        [HttpGet("{id:guid}/print-assets/{qrType}/preview")]
        public async Task<IActionResult> PreviewPrintAsset(
            Guid id,
            string qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!TryParsePhysicalQrType(qrType, out var parsed))
            {
                return NotFound();
            }

            var preview =
                await _printReadyQrMaterials.GetShopOrderPreviewAsync(
                    id,
                    parsed,
                    cancellationToken
                );
            return preview == null ? NotFound() : Ok(preview);
        }

        [HttpPost("{id:guid}/print-assets/{qrType}/approve")]
        public async Task<IActionResult> ApprovePrintAsset(
            Guid id,
            string qrType,
            CancellationToken cancellationToken = default
        )
        {
            if (!TryParsePhysicalQrType(qrType, out var parsed))
            {
                return NotFound();
            }

            if (!TryResolveStaff(out var staffId, out var adminIdentity, out var actorError))
            {
                return actorError;
            }

            var asset = await _printReadyQrMaterials.ApproveShopOrderAsync(
                id,
                parsed,
                staffId,
                adminIdentity,
                cancellationToken
            );
            return asset == null ? NotFound() : Ok(asset);
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

            if (!TryResolveStaff(out var staffId, out var adminIdentity, out var actorError))
            {
                return actorError;
            }

            try
            {
                var asset =
                    await _printReadyQrMaterials.RetryShopOrderAsync(
                        id,
                        parsed,
                        staffId,
                        adminIdentity,
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
                && PhysicalShopPrintQrTypes.Contains(qrType);
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

        /// <summary>
        /// Staff id and identity for audit. 401 without a staff id; 400 when the
        /// session has no email or name.
        /// </summary>
        private bool TryResolveStaff(
            out int staffId,
            out string adminIdentity,
            out IActionResult actorError
        )
        {
            adminIdentity = string.Empty;
            actorError = new EmptyResult();
            var resolved = GetStaffId();
            if (resolved == null)
            {
                staffId = 0;
                actorError = Unauthorized(new { success = false, message = "Invalid token." });
                return false;
            }

            staffId = resolved.Value;
            var identity =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(identity))
            {
                actorError = BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "Admin identity could not be resolved from the current session.",
                    }
                );
                return false;
            }

            adminIdentity = identity;
            return true;
        }

        private int? GetStaffId()
        {
            var staffIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrEmpty(staffIdClaim)
                || !int.TryParse(staffIdClaim, out var staffId)
            )
            {
                return null;
            }

            return staffId;
        }
    }
}
