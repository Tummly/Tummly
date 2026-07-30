using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class CapturePreviewOptionsService : ICapturePreviewOptionsService
    {
        private readonly ApplicationDbContext _context;

        public CapturePreviewOptionsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetPreviewOptionsAsync(
            CapturePreviewOptionsQuery query
        )
        {
            var items = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == query.LocationId
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .OrderBy(q => q.QrType)
                .Select(q => new
                {
                    qrCodeId = q.Id,
                    qrType = q.QrType.ToString(),
                    status = q.Status.ToString(),
                    linkName = q.LinkName,
                })
                .ToListAsync();

            return new { items };
        }
    }
}
