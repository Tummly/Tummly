using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Privacy-owned Permission records CSV export (reports-export-extras ticket 04).
    /// Soft lock / Dormant / chargeback deny via paid-write gate; list reads stay open.
    /// </summary>
    [ApiController]
    [Route("api/privacy-consent/permission-records")]
    [Authorize]
    public class PrivacyConsentPermissionRecordsExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IPrivacyConsentPermissionRecordsExportService _export;

        public PrivacyConsentPermissionRecordsExportController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IPrivacyConsentPermissionRecordsExportService export
        )
        {
            _context = context;
            _permissions = permissions;
            _export = export;
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (locationId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            var privacy = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.PrivacyConsent,
                PermissionLevel.View,
                locationId
            );
            var denied = privacy.ToHttpResult();
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
                cancellationToken
            );
            return File(result.Content, result.ContentType, result.FileName);
        }
    }
}
