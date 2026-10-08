using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offers-owned redemption log CSV/XLSX. Soft-lock gate lives on the
    /// controller (paid-write). Soft-max Take/truncate.
    /// </summary>
    public sealed class OffersRedemptionsExportService
        : IOffersRedemptionsExportService
    {
        public const int ExportSoftMaxRows = 10_000;

        public const string CsvContentType = "text/csv";

        /// <summary>
        /// Matches <c>OFFERS_REDEMPTION_LOG_COPY.columns</c> (no Actions).
        /// PDF pack uses the same row facts with template labels in the writer.
        /// </summary>
        private static readonly string[] Headers =
        [
            "Date/time",
            "Guest",
            "Pass reference",
            "Location",
            "Staff member",
            "Outcome",
            "Reason",
            "Offer version",
            "Offer",
        ];

        private readonly ApplicationDbContext _context;
        private readonly IOfferLifecycleService _lifecycle;

        public OffersRedemptionsExportService(
            ApplicationDbContext context,
            IOfferLifecycleService lifecycle
        )
        {
            _context = context;
            _lifecycle = lifecycle;
        }

        public async Task<ReportsExportFileResult> ExportCsvAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var items = await ListRowsAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var rows = items.Select(ToCsvRow).ToList();

            var stamp = DateTime.UtcNow.ToString(
                "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture
            );
            var fileName =
                $"tummly-offers-redemptions-{locationId}-{stamp}Z.csv";

            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = CsvContentType,
                Content = Rfc4180Csv.WriteUtf8(Headers, rows),
            };
        }

        public async Task<ReportsExportFileResult> ExportXlsxAsync(
            IReadOnlyList<int> locationIds,
            DateTime fromUtc,
            DateTime toUtc,
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

                var items = await ListRowsAsync(
                    id,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
                sheets.Add(
                    (
                        new ReportsStyledXlsxPack.LocationContext(
                            location.Id,
                            location.LocationName,
                            location.RestaurantName
                        ),
                        items.Select(ToCsvRow).ToList()
                    )
                );
            }

            var (content, fileName) =
                ReportsStyledXlsxPack.RenderOffersRedemptionsXlsx(
                    sheets,
                    Headers,
                    fromUtc,
                    toUtc,
                    DateTime.UtcNow
                );

            return new ReportsExportFileResult
            {
                FileName = fileName,
                ContentType = ReportsStyledXlsxWriter.ContentType,
                Content = content,
            };
        }

        public async Task<
            IReadOnlyList<OfferDetailsRedemptionListItemDto>
        > ListRowsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var list = await _lifecycle.ListLocationRedemptionsAsync(
                locationId,
                cancellationToken
            );

            return list.Items
                .Where(row =>
                    row.DateTimeUtc >= fromUtc && row.DateTimeUtc < toUtc
                )
                .OrderByDescending(row => row.DateTimeUtc)
                .Take(ExportSoftMaxRows)
                .ToList();
        }

        private static string[] ToCsvRow(OfferDetailsRedemptionListItemDto row)
        {
            return
            [
                FormatIsoUtc(row.DateTimeUtc),
                row.GuestName,
                row.PassReferenceText,
                row.LocationName,
                row.StaffMemberText ?? string.Empty,
                row.OutcomeLabel,
                row.ReasonLabel ?? string.Empty,
                row.OfferVersionLabel,
                row.OfferTitle,
            ];
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
