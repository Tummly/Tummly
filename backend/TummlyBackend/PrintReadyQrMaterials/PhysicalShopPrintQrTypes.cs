using TummlyBackend.Models;

namespace TummlyBackend.PrintReadyQrMaterials
{
    /// <summary>
    /// Physical QR types that Shop fulfilment can print (includes Counter).
    /// Starter mint stays on <see cref="StarterQrMaterialTypes"/>.
    /// </summary>
    public static class PhysicalShopPrintQrTypes
    {
        public static readonly IReadOnlyList<QrType> All =
        [
            QrType.CounterCard,
            QrType.TableTent,
            QrType.WindowSticker,
            QrType.OfferCard,
        ];

        public static bool Contains(QrType qrType) => All.Contains(qrType);
    }
}
