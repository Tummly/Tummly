using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;

namespace TummlyBackend.Services
{
    public class QrCodeProvisioningService : IQrCodeProvisioningService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartGuestLinkService _smartGuestLink;

        public QrCodeProvisioningService(
            ApplicationDbContext context,
            ISmartGuestLinkService smartGuestLink
        )
        {
            _context = context;
            _smartGuestLink = smartGuestLink;
        }

        public async Task<IReadOnlyList<QrCode>> MintDefaultQrCodesAsync(
            RestaurantLocation location
        )
        {
            var starterTypes = await StarterQrMaterialTypes.LoadAsync(_context);
            if (starterTypes.Count == 0)
            {
                return Array.Empty<QrCode>();
            }

            var existingTypes = await LoadExistingPlacementTypesAsync(location);
            var qrCodes = new List<QrCode>();

            foreach (var qrType in starterTypes)
            {
                if (existingTypes.Contains(qrType))
                {
                    continue;
                }

                // Generated + added one at a time so each subsequent
                // GenerateTokenAsync's change-tracker uniqueness check sees
                // every token already minted in this batch.
                var token = await _smartGuestLink.GenerateTokenAsync();

                var qrCode = new QrCode
                {
                    RestaurantLocation = location,
                    QrType = qrType,
                    Token = token,
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                _context.QrCodes.Add(qrCode);
                qrCodes.Add(qrCode);
                existingTypes.Add(qrType);
            }

            return qrCodes;
        }

        private async Task<HashSet<QrType>> LoadExistingPlacementTypesAsync(
            RestaurantLocation location
        )
        {
            var existing = new HashSet<QrType>();

            foreach (var entry in _context.ChangeTracker.Entries<QrCode>())
            {
                if (
                    entry.State is EntityState.Deleted or EntityState.Detached
                )
                {
                    continue;
                }

                var row = entry.Entity;
                var sameLocation =
                    ReferenceEquals(row.RestaurantLocation, location)
                    || (
                        location.Id != 0
                        && row.RestaurantLocationId == location.Id
                    );
                if (
                    !sameLocation
                    || row.Status == QrCodeStatus.Archived
                )
                {
                    continue;
                }

                existing.Add(row.QrType);
            }

            if (location.Id == 0)
            {
                return existing;
            }

            var persisted = await _context.QrCodes
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantLocationId == location.Id
                    && row.Status != QrCodeStatus.Archived
                )
                .Select(row => row.QrType)
                .ToListAsync();
            foreach (var qrType in persisted)
            {
                existing.Add(qrType);
            }

            return existing;
        }
    }
}
