using System.Text.Json;
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

        public const string GuestDataExportOwnerOnlyMessage =
            "Only the account owner can export guest data.";

        private readonly ApplicationDbContext _context;
        private readonly IAdminAuditService _audit;

        public GuestDataExportService(
            ApplicationDbContext context,
            IAdminAuditService audit
        )
        {
            _context = context;
            _audit = audit;
        }

        public async Task<(
            GuestDataExportResult? Result,
            string? Error,
            int StatusCode
        )>
            ExportAsync(int restaurantId, int actorUserId, string? format)
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

            if (restaurant.OwnerUserId != actorUserId)
            {
                return (
                    null,
                    GuestDataExportOwnerOnlyMessage,
                    StatusCodes.Status403Forbidden
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
                    GuestExportColumns.ToRow(
                        lg.Name,
                        lg.MasterGuest?.Email,
                        lg.MasterGuest?.Mobile,
                        lg.RestaurantLocation?.LocationName ?? string.Empty,
                        lg.MarketingPreference,
                        lg.CreatedAt
                    )
                )
                .ToList();

            var utcNow = DateTime.UtcNow;
            var stamp = utcNow.ToString("yyyyMMdd-HHmmss");
            var extension = normalizedFormat == "csv" ? "csv" : "xlsx";
            var fileName =
                $"tummly-guest-data-{restaurant.Id}-{stamp}Z.{extension}";

            var result = normalizedFormat == "csv"
                ? new GuestDataExportResult
                {
                    FileName = fileName,
                    ContentType = "text/csv",
                    Content = Rfc4180Csv.WriteUtf8(
                        GuestExportColumns.Headers,
                        rows
                    ),
                    RowCount = rows.Count,
                }
                : new GuestDataExportResult
                {
                    FileName = fileName,
                    ContentType = OpenXmlSpreadsheet.ContentType,
                    Content = OpenXmlSpreadsheet.Write(
                        GuestExportColumns.Headers,
                        rows
                    ),
                    RowCount = rows.Count,
                };

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.GuestDataExport,
                    ActorIdentity: $"user:{actorUserId}",
                    TargetType: AdminAuditTargetTypes.Restaurant,
                    TargetId: restaurant.Id.ToString(),
                    RestaurantId: restaurant.Id,
                    DetailJson: JsonSerializer.Serialize(
                        new
                        {
                            format = normalizedFormat,
                            rows = rows.Count,
                        }
                    )
                )
            );
            await _context.SaveChangesAsync();

            return (result, null, StatusCodes.Status200OK);
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
    }
}
