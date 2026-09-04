using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Offers-owned redemption log CSV export (reports-export-extras ticket 03).
    /// Soft lock / Dormant / chargeback deny via paid-write gate; list reads stay open.
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
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            var windowError = ReportsQueryGate.TryValidateLocationAndWindow(
                this,
                locationId,
                from,
                to,
                out var fromUtc,
                out var toUtc
            );
            if (windowError != null)
            {
                return windowError;
            }

            var offers = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.Offers,
                PermissionLevel.View,
                locationId
            );
            var denied = offers.ToHttpResult();
            if (denied != null)
            {
                return denied;
            }

            try
            {
                await OperatorBillingLockGate.EnsurePaidWriteAllowedForLocationAsync(
                    _context,
                    locationId,
                    cancellationToken
                );
            }
            catch (OperatorBillingLockedException ex)
            {
                return OperatorBillingLockGate.Forbidden(ex.Code);
            }

            var result = await _export.ExportCsvAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            return File(result.Content, result.ContentType, result.FileName);
        }
    }
}
