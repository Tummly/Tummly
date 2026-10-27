using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Resolves the QR placement for a (location, QR type) key (spec 4.3).
    /// Archived codes are never chosen.
    /// </summary>
    public class QrPlacementResolver : IQrPlacementResolver
    {
        private readonly ApplicationDbContext _context;

        public QrPlacementResolver(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<QrPlacementResolution> ResolveAsync(
            int locationId,
            QrType qrType,
            CancellationToken ct
        )
        {
            var activeId = await OldestIdAsync(
                locationId,
                qrType,
                QrCodeStatus.Active,
                ct
            );
            if (activeId.HasValue)
            {
                return new Reuse(activeId.Value);
            }

            var pausedId = await OldestIdAsync(
                locationId,
                qrType,
                QrCodeStatus.Paused,
                ct
            );
            return new NeedsSlot(pausedId);
        }

        private Task<int?> OldestIdAsync(
            int locationId,
            QrType qrType,
            QrCodeStatus status,
            CancellationToken ct
        ) =>
            _context.QrCodes
                .Where(qr =>
                    qr.RestaurantLocationId == locationId
                    && qr.QrType == qrType
                    && qr.Status == status
                    && qr.ArchivedAt == null
                )
                .OrderBy(qr => qr.CreatedAt)
                .ThenBy(qr => qr.Id)
                .Select(qr => (int?)qr.Id)
                .FirstOrDefaultAsync(ct);
    }
}
