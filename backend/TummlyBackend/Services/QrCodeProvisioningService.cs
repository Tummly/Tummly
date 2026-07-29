using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class QrCodeProvisioningService : IQrCodeProvisioningService
    {
        // Order mirrors CONTEXT.md "QR type": four placement types, then
        // Smart Guest (the operator-facing default link).
        private static readonly QrType[] DefaultQrTypes =
        {
            QrType.CounterCard,
            QrType.PackagingSticker,
            QrType.DeliveryInsert,
            QrType.WindowSticker,
            QrType.SmartGuest,
        };

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
            var qrCodes = new List<QrCode>(DefaultQrTypes.Length);

            foreach (var qrType in DefaultQrTypes)
            {
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
            }

            return qrCodes;
        }
    }
}
