using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Privacy-owned Permission records CSV (reports-export-extras ticket 04).
    /// Soft-lock gate lives on the controller (paid-write). Soft-max Take/truncate.
    /// Location snapshot only — no Reports date window.
    /// </summary>
    public sealed class PrivacyConsentPermissionRecordsExportService
        : IPrivacyConsentPermissionRecordsExportService
    {
        public const int ExportSoftMaxRows = 10_000;

        public const string CsvContentType = "text/csv";

        /// <summary>
        /// Matches Permission records table columns (no Action / View).
        /// </summary>
        private static readonly string[] Headers =
        [
            "Guest",
            "Permission",
            "Current state",
            "Location",
            "Source",
            "Recorded",
        ];

        private readonly ApplicationDbContext _context;

        public PrivacyConsentPermissionRecordsExportService(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        public async Task<ReportsExportFileResult> ExportCsvAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var baseQuery = _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(entry => entry.RestaurantLocationId == locationId);

            var latestEntryIds = await baseQuery
                .GroupBy(entry => new
                {
                    entry.LocationGuestId,
                    entry.PermissionKind,
                    entry.RestaurantLocationId,
                })
                .Select(group =>
                    group
                        .OrderByDescending(entry => entry.OccurredAt)
                        .ThenByDescending(entry => entry.Id)
                        .Select(entry => entry.Id)
                        .First()
                )
                .ToListAsync(cancellationToken);

            var rows = Array.Empty<string[]>();
            if (latestEntryIds.Count > 0)
            {
                var pageRows = await _context.LocationGuestPermissionLedgerEntries
                    .AsNoTracking()
                    .Where(entry => latestEntryIds.Contains(entry.Id))
                    .OrderByDescending(entry => entry.OccurredAt)
                    .ThenByDescending(entry => entry.Id)
                    .Take(ExportSoftMaxRows)
                    .Select(entry => new
                    {
                        GuestName = entry.LocationGuest.Name,
                        PermissionKind = entry.PermissionKind,
                        EventKind = entry.EventKind,
                        LocationName = entry.RestaurantLocation.LocationName,
                        Source = entry.Source,
                        OccurredAt = entry.OccurredAt,
                    })
                    .ToListAsync(cancellationToken);

                rows = pageRows
                    .Select(row =>
                        new[]
                        {
                            row.GuestName,
                            LocationGuestPermissionPresentation.PermissionLabel(
                                row.PermissionKind
                            ),
                            row.EventKind
                            == LocationGuestPermissionLedgerEventKinds.Grant
                                ? "Granted"
                                : "Withdrawn",
                            row.LocationName,
                            LocationGuestPermissionPresentation.SourceLabel(
                                row.Source
                            ),
                            FormatIsoUtc(row.OccurredAt),
                        }
                    )
                    .ToArray();
            }

            var stamp = DateTime.UtcNow.ToString(
                "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture
            );
            var fileName =
                $"tummly-consent-permission-records-{locationId}-{stamp}Z.csv";

            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = CsvContentType,
                Content = Rfc4180Csv.WriteUtf8(Headers, rows),
            };
        }

        private static string FormatIsoUtc(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
            return utc.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
