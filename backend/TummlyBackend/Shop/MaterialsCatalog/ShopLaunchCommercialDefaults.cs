using TummlyBackend.Helpers;

namespace TummlyBackend.Shop.MaterialsCatalog
{
    /// <summary>
    /// Launch PRD commercial defaults (packs / prices / delivery). Editable in Admin after seed.
    /// </summary>
    public static class ShopLaunchCommercialDefaults
    {
        public sealed record LaunchCommercial(
            string ProductKey,
            IReadOnlyList<ShopMaterialPackRow> Packs,
            int DeliveryNetPence
        );

        public static readonly LaunchCommercial QrCounterDisplay = new(
            "qr-counter-display",
            [
                new ShopMaterialPackRow { Qty = 1, NetPence = 999 },
                new ShopMaterialPackRow { Qty = 2, NetPence = 1899 },
                new ShopMaterialPackRow { Qty = 5, NetPence = 4499 },
            ],
            DeliveryNetPence: 299
        );

        public static readonly LaunchCommercial TableTent = new(
            "table-tents",
            [
                new ShopMaterialPackRow { Qty = 5, NetPence = 999 },
                new ShopMaterialPackRow { Qty = 10, NetPence = 1699 },
                new ShopMaterialPackRow { Qty = 20, NetPence = 2999 },
            ],
            DeliveryNetPence: 399
        );

        public static readonly LaunchCommercial TableTalker = new(
            "table-talker",
            [
                new ShopMaterialPackRow { Qty = 10, NetPence = 999 },
                new ShopMaterialPackRow { Qty = 25, NetPence = 1999 },
                new ShopMaterialPackRow { Qty = 50, NetPence = 3499 },
            ],
            DeliveryNetPence: 399
        );

        public static readonly LaunchCommercial WindowSticker = new(
            "window-stickers",
            [
                new ShopMaterialPackRow { Qty = 1, NetPence = 499 },
                new ShopMaterialPackRow { Qty = 2, NetPence = 799 },
                new ShopMaterialPackRow { Qty = 5, NetPence = 1499 },
            ],
            DeliveryNetPence: 399
        );

        public static readonly LaunchCommercial QrGuestCards = new(
            "qr-guest-cards",
            [
                new ShopMaterialPackRow { Qty = 250, NetPence = 1499 },
                new ShopMaterialPackRow { Qty = 500, NetPence = 2499 },
                new ShopMaterialPackRow { Qty = 1000, NetPence = 3999 },
            ],
            DeliveryNetPence: 399
        );

        public static IReadOnlyList<LaunchCommercial> AllFive { get; } =
        [
            QrCounterDisplay,
            TableTent,
            TableTalker,
            WindowSticker,
            QrGuestCards,
        ];

        /// <summary>
        /// Maps current seed skuIds (and launch keys) onto PRD commercial rows.
        /// </summary>
        public static bool TryForSkuId(string skuId, out LaunchCommercial commercial)
        {
            if (string.Equals(skuId, "table-tents", StringComparison.Ordinal))
            {
                commercial = TableTent;
                return true;
            }

            if (string.Equals(skuId, "window-stickers", StringComparison.Ordinal))
            {
                commercial = WindowSticker;
                return true;
            }

            // Legacy offer-card skuId still maps to Counter commercial for old rows.
            if (string.Equals(skuId, "offer-card", StringComparison.Ordinal)
                || string.Equals(skuId, QrCounterDisplay.ProductKey, StringComparison.Ordinal))
            {
                commercial = QrCounterDisplay;
                return true;
            }

            if (string.Equals(skuId, TableTalker.ProductKey, StringComparison.Ordinal))
            {
                commercial = TableTalker;
                return true;
            }

            if (string.Equals(skuId, QrGuestCards.ProductKey, StringComparison.Ordinal))
            {
                commercial = QrGuestCards;
                return true;
            }

            commercial = null!;
            return false;
        }
    }
}
