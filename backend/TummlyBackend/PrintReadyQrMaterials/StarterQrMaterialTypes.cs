using TummlyBackend.Models;

namespace TummlyBackend.PrintReadyQrMaterials
{
    public static class StarterQrMaterialTypes
    {
        public static readonly IReadOnlyList<QrType> All =
        [
            QrType.TableTent,
            QrType.WindowSticker,
            QrType.OfferCard,
        ];

        public static bool Contains(QrType qrType) => All.Contains(qrType);
    }
}
