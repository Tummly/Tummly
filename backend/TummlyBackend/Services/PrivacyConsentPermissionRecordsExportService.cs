using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Privacy-owned Permission records CSV/XLSX. Soft-lock gate lives on the
    /// controller (paid-write). Soft-max Take/truncate. Location snapshot only —
    /// no Reports date window.
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
            "Basis",
            "Guest form version",
            "Wording version",
            "Privacy notice version",
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
            var rows = await LoadRowsAsync(locationId, cancellationToken);

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

        public async Task<ReportsExportFileResult> ExportXlsxAsync(
            IReadOnlyList<int> locationIds,
            CancellationToken cancellationToken = default
        )
        {
            var distinct = locationIds.Distinct().ToList();
            if (distinct.Count == 0)
            {
                throw new ArgumentException(
                    "At least one locationId is required.",
                    nameof(locationIds)
                );
            }

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => distinct.Contains(row.Id))
                .Select(row => new
                {
                    row.Id,
                    row.LocationName,
                    RestaurantName = row.Restaurant!.Name,
                })
                .ToListAsync(cancellationToken);

            var byId = locations.ToDictionary(row => row.Id);
            var sheets =
                new List<(
                    ReportsStyledXlsxPack.LocationContext Location,
                    IReadOnlyList<string[]> Rows
                )>();

            foreach (var id in distinct)
            {
                if (!byId.TryGetValue(id, out var location))
                {
                    throw new KeyNotFoundException(
                        $"Location {id} was not found."
                    );
                }

                var rows = await LoadRowsAsync(id, cancellationToken);
                sheets.Add(
                    (
                        new ReportsStyledXlsxPack.LocationContext(
                            location.Id,
                            location.LocationName,
                            location.RestaurantName
                        ),
                        rows
                    )
                );
            }

            var (content, fileName) =
                ReportsStyledXlsxPack.RenderGuestConsentXlsx(
                    sheets,
                    Headers,
                    DateTime.UtcNow
                );

            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsStyledXlsxWriter.ContentType,
                Content = content,
            };
        }

        private async Task<string[][]> LoadRowsAsync(
            int locationId,
            CancellationToken cancellationToken
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

            if (latestEntryIds.Count == 0)
            {
                return [];
            }

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
                    Basis = entry.Basis,
                    GuestFormVersion = entry.GuestFormVersion,
                    WordingVersion = entry.WordingVersion,
                    PrivacyNoticeVersion = entry.PrivacyNoticeVersion,
                    OccurredAt = entry.OccurredAt,
                })
                .ToListAsync(cancellationToken);

            return pageRows
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
                        row.Basis ?? string.Empty,
                        row.GuestFormVersion ?? string.Empty,
                        row.WordingVersion ?? string.Empty,
                        row.PrivacyNoticeVersion ?? string.Empty,
                        FormatIsoUtc(row.OccurredAt),
                    }
                )
                .ToArray();
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
