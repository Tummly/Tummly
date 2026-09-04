using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Sync Reports export pack (ticket 17 / lock 09). Soft lock / Dormant /
    /// chargeback deny via paid-write gate; KPI reads stay open.
    /// </summary>
    [ApiController]
    [Route("api/reports/export")]
    [Authorize]
    public class ReportsExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IReportsExportService _export;

        public ReportsExportController(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IReportsExportService export
        )
        {
            _context = context;
            _permissions = permissions;
            _export = export;
        }

        [HttpGet("overview")]
        public Task<IActionResult> ExportOverview(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                from,
                to,
                (id, fromUtc, toUtc, ct) =>
                    _export.ExportOverviewPdfAsync(id, fromUtc, toUtc, ct),
                cancellationToken
            );

        [HttpGet("capture")]
        public Task<IActionResult> ExportCapture(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                from,
                to,
                (id, fromUtc, toUtc, ct) =>
                    _export.ExportCaptureCsvAsync(id, fromUtc, toUtc, ct),
                cancellationToken
            );

        [HttpGet("feedback")]
        public Task<IActionResult> ExportFeedback(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                from,
                to,
                (id, fromUtc, toUtc, ct) =>
                    _export.ExportFeedbackCsvAsync(id, fromUtc, toUtc, ct),
                cancellationToken
            );

        [HttpGet("campaigns")]
        public Task<IActionResult> ExportCampaigns(
            [FromQuery] int locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                from,
                to,
                (id, fromUtc, toUtc, ct) =>
                    _export.ExportCampaignsCsvAsync(id, fromUtc, toUtc, ct),
                cancellationToken
            );

        private async Task<IActionResult> ExportAsync(
            int locationId,
            DateTime? from,
            DateTime? to,
            Func<
                int,
                DateTime,
                DateTime,
                CancellationToken,
                Task<ReportsExportFileResult>
            > build,
            CancellationToken cancellationToken
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

            var reports = await ReportsQueryGate.AuthorizeReportsViewAsync(
                _permissions,
                User,
                locationId
            );
            var denied = reports.ToHttpResult();
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

            var result = await build(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            return File(result.Content, result.ContentType, result.FileName);
        }
    }
}
