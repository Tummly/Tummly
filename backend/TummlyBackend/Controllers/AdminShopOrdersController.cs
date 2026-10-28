using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
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
        private readonly IAdminAuditService _adminAudit;

        public AdminShopOrdersController(
            IAdminShopOrderFulfilmentService fulfilment,
            IPrintReadyQrMaterialsService printReadyQrMaterials,
            IAdminAuditService adminAudit
        )
        {
            _fulfilment = fulfilment;
            _printReadyQrMaterials = printReadyQrMaterials;
            _adminAudit = adminAudit;
        }

        [HttpGet]
        public async Task<IActionResult> ListOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? q = null,
            [FromQuery] int? restaurantId = null,
            [FromQuery] string[]? fulfilmentStatus = null,
            [FromQuery] bool dispatchEvidenceMissing = false,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var result = await _fulfilment.GetListAsync(
                    BuildListQuery(
                        page,
                        pageSize,
                        q,
                        restaurantId,
                        fulfilmentStatus,
                        dispatchEvidenceMissing
                    ),
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
            [FromQuery] bool dispatchEvidenceMissing = false,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                // page/pageSize accepted on the wire (Guests export pattern) but
                // ignored for warehouse dump — ExportCsvAsync returns all matches.
                var result = await _fulfilment.ExportCsvAsync(
                    BuildListQuery(
                        page,
                        pageSize,
                        q,
                        restaurantId,
                        fulfilmentStatus,
                        dispatchEvidenceMissing
                    ),
                    cancellationToken
                );
                await _adminAudit.AppendAndSaveAsync(
                    StaffReadAudit.Build(
                        User,
                        AdminAuditActions.ShopOrdersExport,
                        AdminAuditTargetTypes.ShopOrder,
                        "export",
                        System.Text.Json.JsonSerializer.Serialize(
                            new { restaurantId, fulfilmentStatus, dispatchEvidenceMissing }
                        )
                    ),
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
            return FulfilmentResult(result);
        }

        [HttpPost("{id:guid}/preparation-started")]
        public async Task<IActionResult> MarkPreparationStarted(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            var denied = ResolveAdminActor(out var actorId, out var actorIdentity);
            if (denied != null)
            {
                return denied;
            }

            var result = await _fulfilment.MarkPreparationStartedAsync(
                id,
                actorId,
                actorIdentity,
                cancellationToken
            );
            return FulfilmentResult(result);
        }

        [HttpPost("{id:guid}/production-started")]
        public async Task<IActionResult> MarkProductionStarted(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            var denied = ResolveAdminActor(out var actorId, out var actorIdentity);
            if (denied != null)
            {
                return denied;
            }

            var result = await _fulfilment.MarkProductionStartedAsync(
                id,
                actorId,
                actorIdentity,
                cancellationToken
            );
            return FulfilmentResult(result);
        }

        [HttpPost("{id:guid}/dispatched")]
        public async Task<IActionResult> MarkDispatched(
            Guid id,
            [FromBody] AdminShopDispatchRequest? body,
            CancellationToken cancellationToken = default
        )
        {
            var denied = ResolveAdminActor(out var actorId, out var actorIdentity);
            if (denied != null)
            {
                return denied;
            }

            var result = await _fulfilment.MarkDispatchedAsync(
                id,
                body ?? new AdminShopDispatchRequest(),
                actorId,
                actorIdentity,
                cancellationToken
            );
            return FulfilmentResult(result);
        }

        [HttpPost("{id:guid}/delivered")]
        public async Task<IActionResult> MarkDelivered(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            var denied = ResolveAdminActor(out var actorId, out var actorIdentity);
            if (denied != null)
            {
                return denied;
            }

            var result = await _fulfilment.MarkDeliveredAsync(
                id,
                actorId,
                actorIdentity,
                cancellationToken
            );
            return FulfilmentResult(result);
        }

        [HttpPost("{id:guid}/dispatch-evidence")]
        public async Task<IActionResult> RecordDispatchEvidence(
            Guid id,
            [FromBody] AdminShopDispatchEvidenceRequest? body,
            CancellationToken cancellationToken = default
        )
        {
            var denied = ResolveAdminActor(out var actorId, out var actorIdentity);
            if (denied != null)
            {
                return denied;
            }

            var result = await _fulfilment.RecordDispatchEvidenceAsync(
                id,
                body ?? new AdminShopDispatchEvidenceRequest(),
                actorId,
                actorIdentity,
                cancellationToken
            );
            return FulfilmentResult(result);
        }

        [HttpPost("{id:guid}/status-correction")]
        public async Task<IActionResult> CorrectStatus(
            Guid id,
            [FromBody] AdminShopStatusCorrectionRequest? body,
            CancellationToken cancellationToken = default
        )
        {
            var denied = ResolveAdminActor(out var actorId, out var actorIdentity);
            if (denied != null)
            {
                return denied;
            }

            var result = await _fulfilment.CorrectStatusAsync(
                id,
                body ?? new AdminShopStatusCorrectionRequest(),
                actorId,
                actorIdentity,
                cancellationToken
            );
            return FulfilmentResult(result);
        }

        [HttpPost("{id:guid}/force-cancel")]
        public async Task<IActionResult> ForceCancel(
            Guid id,
            [FromBody] AdminShopForceCancelRequest body,
            CancellationToken cancellationToken = default
        )
        {
            var staffId = GetStaffId();
            if (staffId == null)
            {
                return Unauthorized(
                    new
                    {
                        success = false,
                        message = "Invalid token.",
                    }
                );
            }

            var adminIdentity =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(adminIdentity))
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "Admin identity could not be resolved from the current session.",
                    }
                );
            }

            var result = await _fulfilment.ForceCancelAsync(
                id,
                body,
                staffId.Value,
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

        private IActionResult? ResolveAdminActor(
            out int actorId,
            out string actorIdentity
        )
        {
            actorId = 0;
            actorIdentity = string.Empty;

            var staffId = GetStaffId();
            if (staffId == null)
            {
                return Unauthorized(
                    new
                    {
                        success = false,
                        message = "Invalid token.",
                    }
                );
            }

            var identity =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(identity))
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "Admin identity could not be resolved from the current session.",
                    }
                );
            }

            actorId = staffId.Value;
            actorIdentity = identity;
            return null;
        }

        private IActionResult FulfilmentResult(AdminShopOrderFulfilmentResult result)
        {
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

            var staffId = GetStaffId();
            if (staffId == null)
            {
                return Unauthorized(
                    new
                    {
                        success = false,
                        message = "Invalid token.",
                    }
                );
            }

            var adminIdentity =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(adminIdentity))
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "Admin identity could not be resolved from the current session.",
                    }
                );
            }

            var asset = await _printReadyQrMaterials.ApproveShopOrderAsync(
                id,
                parsed,
                staffId.Value,
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
            string[]? fulfilmentStatus,
            bool dispatchEvidenceMissing
        )
        {
            return new AdminShopOrdersListQuery
            {
                Page = page,
                PageSize = pageSize,
                Q = q,
                RestaurantId = restaurantId,
                FulfilmentStatus = fulfilmentStatus ?? Array.Empty<string>(),
                DispatchEvidenceMissing = dispatchEvidenceMissing,
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
