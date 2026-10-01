using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Offers-owned redemption log CSV/XLSX export. Soft lock / Dormant /
    /// chargeback deny via paid-write gate; list reads stay open.
    /// </summary>
    [ApiController]
    [Route("api/offers/redemptions")]
    [Authorize]
    public class OffersRedemptionsExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IOffersRedemptionsExportService _export;

        public OffersRedemptionsExportController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IOffersRedemptionsExportService export
        )
        {
            _context = context;
            _permissions = permissions;
            _export = export;
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] int? locationId,
            [FromQuery] int[]? locationIds,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? format,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var formatError = ReportsExportRequestGate.TryParseFormat(
                this,
                format,
                ["csv", "xlsx"],
                "csv",
                out var normalizedFormat
            );
            if (formatError != null)
            {
                return formatError;
            }

            var idsError = ReportsExportRequestGate.TryResolveLocationIds(
                this,
                locationId,
                locationIds,
                locationIdsCsv: null,
                out var resolvedIds
            );
            if (idsError != null)
            {
                return idsError;
            }

            if (normalizedFormat != "xlsx" && resolvedIds.Count > 1)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Multiple locationIds are only supported for format=xlsx.",
                });
            }

            var windowError = ReportsQueryGate.TryValidateLocationAndWindow(
                this,
                resolvedIds[0],
                from,
                to,
                out var fromUtc,
                out var toUtc
            );
            if (windowError != null)
            {
                return windowError;
            }

            int? restaurantId = null;
            foreach (var id in resolvedIds)
            {
                var offers = await _permissions.AuthorizeLocationAsync(
                    User,
                    OperatorAreaIds.Offers,
                    PermissionLevel.View,
                    id
                );
                var denied = offers.ToHttpResult();
                if (denied != null)
                {
                    return denied;
                }

                restaurantId ??= offers.RestaurantId;
            }

            try
            {
                if (restaurantId is int rid)
                {
                    await OperatorBillingLockGate.EnsurePaidWriteAllowedAsync(
                        _context,
                        rid,
                        cancellationToken
                    );
                }
                else
                {
                    await OperatorBillingLockGate.EnsurePaidWriteAllowedForLocationAsync(
                        _context,
                        resolvedIds[0],
                        cancellationToken
                    );
                }
            }
            catch (OperatorBillingLockedException ex)
            {
                return OperatorBillingLockGate.Forbidden(ex.Code);
            }

            var result =
                normalizedFormat == "xlsx"
                    ? await _export.ExportXlsxAsync(
                        resolvedIds,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    )
                    : await _export.ExportCsvAsync(
                        resolvedIds[0],
                        fromUtc,
                        toUtc,
                        cancellationToken
                    );
            return File(result.Content, result.ContentType, result.FileName);
        }
    }
}
