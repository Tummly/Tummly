using TummlyBackend.Models;

namespace TummlyBackend.PrintReadyQrMaterials
{
    /// <summary>
    /// Physical QR types that Shop fulfilment can print.
    /// Starter mint stays on <see cref="StarterQrMaterialTypes"/>.
    /// </summary>
    public static class PhysicalShopPrintQrTypes
    {
        public static readonly IReadOnlyList<QrType> All =
        [
            QrType.CounterCard,
            QrType.TableTent,
            QrType.TableTalker,
            QrType.WindowSticker,
            QrType.OfferCard,
            QrType.GuestCard,
        ];

        public static bool Contains(QrType qrType) => All.Contains(qrType);
    }
}
