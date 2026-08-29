using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.AccountWorkspace;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestDataExportService : IGuestDataExportService
    {
        public const int ExportSoftMaxRows = 10_000;

        public const string ExportSoftMaxMessage =
            "Export exceeds 10,000 guest rows for this workspace.";

        private static readonly string[] ExportHeaders =
        [
            "Name",
            "Email",
            "Mobile",
            "Location",
            "Marketing preference",
            "First captured",
        ];

        private readonly ApplicationDbContext _context;

        public GuestDataExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(
            GuestDataExportResult? Result,
            string? Error,
            int StatusCode
        )>             ExportAsync(int restaurantId, string? format)
        {
            string normalizedFormat;
            try
            {
                normalizedFormat = NormalizeFormat(format);
            }
            catch (ArgumentException ex)
            {
                return (
                    null,
                    ex.Message,
                    StatusCodes.Status400BadRequest
                );
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant == null)
            {
                return (
                    null,
                    "Restaurant not found.",
                    StatusCodes.Status404NotFound
                );
            }

            var billingDeny = await OperatorBillingLockGate.EvaluatePaidWriteDenyAsync(
                _context,
                restaurantId
            );
            if (billingDeny != null)
            {
                return (
                    null,
                    billingDeny,
                    StatusCodes.Status403Forbidden
                );
            }

            var guests = await _context.LocationGuests
                .AsNoTracking()
                .Include(lg => lg.MasterGuest)
                .Include(lg => lg.RestaurantLocation)
                .Where(lg =>
                    lg.RestaurantLocation!.RestaurantId == restaurant.Id
                )
                .OrderBy(lg => lg.RestaurantLocation!.LocationName)
                .ThenBy(lg => lg.Name)
                .ThenBy(lg => lg.Id)
                .ToListAsync();

            if (guests.Count > ExportSoftMaxRows)
            {
                return (
                    null,
                    ExportSoftMaxMessage,
                    StatusCodes.Status400BadRequest
                );
            }

            var rows = guests
                .Select(lg =>
                    (IReadOnlyList<string>)
                    [
                        lg.Name,
                        lg.MasterGuest?.Email ?? string.Empty,
                        lg.MasterGuest?.Mobile ?? string.Empty,
                        lg.RestaurantLocation?.LocationName ?? string.Empty,
                        FormatMarketingPreference(lg.MarketingPreference),
                        FormatIsoUtc(lg.CreatedAt),
                    ]
                )
                .ToList();

            var utcNow = DateTime.UtcNow;
            var stamp = utcNow.ToString("yyyyMMdd-HHmmss");
            var extension = normalizedFormat == "csv" ? "csv" : "xlsx";
            var fileName =
                $"tummly-guest-data-{restaurant.Id}-{stamp}Z.{extension}";

            if (normalizedFormat == "csv")
            {
                return (
                    new GuestDataExportResult
                    {
                        FileName = fileName,
                        ContentType = "text/csv",
                        Content = Rfc4180Csv.WriteUtf8(ExportHeaders, rows),
                    },
                    null,
                    StatusCodes.Status200OK
                );
            }

            return (
                new GuestDataExportResult
                {
                    FileName = fileName,
                    ContentType = OpenXmlSpreadsheet.ContentType,
                    Content = OpenXmlSpreadsheet.Write(ExportHeaders, rows),
                },
                null,
                StatusCodes.Status200OK
            );
        }

        private static string NormalizeFormat(string? format)
        {
            var key = (format ?? "xlsx").Trim().ToLowerInvariant();
            if (key is not ("xlsx" or "csv"))
            {
                throw new ArgumentException("format must be xlsx or csv.");
            }

            return key;
        }

        private static string FormatMarketingPreference(
            LocationGuestMarketingPreference preference
        ) =>
            preference switch
            {
                LocationGuestMarketingPreference.Allowed => "Allowed",
                LocationGuestMarketingPreference.OptedOut => "Opted out",
                LocationGuestMarketingPreference.NotRecorded => "Not recorded",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(preference),
                    preference,
                    "Unknown Location Guest marketing preference."
                ),
            };

        private static string FormatIsoUtc(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

            return utc.ToString("O");
        }
    }
}
