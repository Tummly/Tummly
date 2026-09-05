using System.Globalization;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offers-owned redemption log CSV (reports-export-extras ticket 03). Soft-lock
    /// gate lives on the controller (paid-write). Soft-max Take/truncate.
    /// </summary>
    public sealed class OffersRedemptionsExportService
        : IOffersRedemptionsExportService
    {
        public const int ExportSoftMaxRows = 10_000;

        public const string CsvContentType = "text/csv";

        /// <summary>
        /// Matches <c>OFFERS_REDEMPTION_LOG_COPY.columns</c> (no Actions).
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

        private readonly IOfferLifecycleService _lifecycle;

        public OffersRedemptionsExportService(IOfferLifecycleService lifecycle)
        {
            _lifecycle = lifecycle;
        }

        public async Task<ReportsExportFileResult> ExportCsvAsync(
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

            var rows = list.Items
                .Where(row =>
                    row.DateTimeUtc >= fromUtc && row.DateTimeUtc < toUtc
                )
                .OrderByDescending(row => row.DateTimeUtc)
                .Take(ExportSoftMaxRows)
                .Select(ToCsvRow)
                .ToList();

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
