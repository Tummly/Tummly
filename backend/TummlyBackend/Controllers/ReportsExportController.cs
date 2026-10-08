using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Controllers
{
    /// <summary>
    /// Sync Reports export pack. Soft lock / Dormant / chargeback deny via
    /// paid-write gate; KPI reads stay open. PDF/CSV retained; XLSX additive.
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
            [FromQuery] int? locationId,
            [FromQuery] int[]? locationIds,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? format,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                locationIds,
                from,
                to,
                format,
                allowedFormats: ["pdf", "xlsx"],
                defaultFormat: "pdf",
                build: async (ids, fromUtc, toUtc, fmt, ct) =>
                {
                    if (fmt == "xlsx")
                    {
                        return await _export.ExportOverviewXlsxAsync(
                            ids,
                            fromUtc,
                            toUtc,
                            ct
                        );
                    }

                    var includeOffers = true;
                    var includeConsent = true;
                    foreach (var id in ids)
                    {
                        var offers = await _permissions.AuthorizeLocationAsync(
                            User,
                            OperatorAreaIds.Offers,
                            PermissionLevel.View,
                            id
                        );
                        if (offers.ToHttpResult() != null)
                        {
                            includeOffers = false;
                        }

                        var privacy = await _permissions.AuthorizeLocationAsync(
                            User,
                            OperatorAreaIds.PrivacyConsent,
                            PermissionLevel.View,
                            id
                        );
                        if (privacy.ToHttpResult() != null)
                        {
                            includeConsent = false;
                        }
                    }

                    return await _export.ExportOverviewPdfAsync(
                        ids,
                        fromUtc,
                        toUtc,
                        includeOffers,
                        includeConsent,
                        ct
                    );
                },
                cancellationToken
            );

        [HttpGet("capture")]
        public Task<IActionResult> ExportCapture(
            [FromQuery] int? locationId,
            [FromQuery] int[]? locationIds,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? format,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                locationIds,
                from,
                to,
                format,
                allowedFormats: ["csv", "xlsx"],
                defaultFormat: "csv",
                build: async (ids, fromUtc, toUtc, fmt, ct) =>
                {
                    if (fmt == "xlsx")
                    {
                        return await _export.ExportCaptureXlsxAsync(
                            ids,
                            fromUtc,
                            toUtc,
                            ct
                        );
                    }

                    return await _export.ExportCaptureCsvAsync(
                        ids[0],
                        fromUtc,
                        toUtc,
                        ct
                    );
                },
                cancellationToken
            );

        [HttpGet("feedback")]
        public Task<IActionResult> ExportFeedback(
            [FromQuery] int? locationId,
            [FromQuery] int[]? locationIds,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? format,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                locationIds,
                from,
                to,
                format,
                allowedFormats: ["csv", "xlsx"],
                defaultFormat: "csv",
                build: async (ids, fromUtc, toUtc, fmt, ct) =>
                {
                    if (fmt == "xlsx")
                    {
                        return await _export.ExportFeedbackXlsxAsync(
                            ids,
                            fromUtc,
                            toUtc,
                            ct
                        );
                    }

                    return await _export.ExportFeedbackCsvAsync(
                        ids[0],
                        fromUtc,
                        toUtc,
                        ct
                    );
                },
                cancellationToken
            );

        [HttpGet("campaigns")]
        public Task<IActionResult> ExportCampaigns(
            [FromQuery] int? locationId,
            [FromQuery] int[]? locationIds,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? format,
            CancellationToken cancellationToken = default
        )
            => ExportAsync(
                locationId,
                locationIds,
                from,
                to,
                format,
                allowedFormats: ["csv", "xlsx"],
                defaultFormat: "csv",
                build: async (ids, fromUtc, toUtc, fmt, ct) =>
                {
                    if (fmt == "xlsx")
                    {
                        return await _export.ExportCampaignsXlsxAsync(
                            ids,
                            fromUtc,
                            toUtc,
                            ct
                        );
                    }

                    return await _export.ExportCampaignsCsvAsync(
                        ids[0],
                        fromUtc,
                        toUtc,
                        ct
                    );
                },
                cancellationToken
            );

        private async Task<IActionResult> ExportAsync(
            int? locationId,
            int[]? locationIds,
            DateTime? from,
            DateTime? to,
            string? format,
            string[] allowedFormats,
            string defaultFormat,
            Func<
                IReadOnlyList<int>,
                DateTime,
                DateTime,
                string,
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

            var formatError = ReportsExportRequestGate.TryParseFormat(
                this,
                format,
                allowedFormats,
                defaultFormat,
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

            // CSV stays single-location; PDF/XLSX may be multi (REP-03).
            if (
                normalizedFormat == "csv"
                && resolvedIds.Count > 1
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Multiple locationIds are only supported for format=pdf or format=xlsx.",
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
                var reports = await ReportsQueryGate.AuthorizeReportsViewAsync(
                    _permissions,
                    User,
                    id
                );
                var denied = reports.ToHttpResult();
                if (denied != null)
                {
                    return denied;
                }

                restaurantId ??= reports.RestaurantId;
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

            var result = await build(
                resolvedIds,
                fromUtc,
                toUtc,
                normalizedFormat,
                cancellationToken
            );
            return File(result.Content, result.ContentType, result.FileName);
        }
    }
}
