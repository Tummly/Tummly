import deliveryBagSealImg from "@/assets/images/shop/products/delivery-bag-seal.webp"
import drinkCupSealImg from "@/assets/images/shop/products/drink-cup-seal.webp"
import pizzaBoxLabelImg from "@/assets/images/shop/products/pizza-box-label.webp"
import posterCustomerClubImg from "@/assets/images/shop/products/poster-customer-club.webp"
import qrCounterDisplayImg from "@/assets/images/shop/products/qr-counter-display.webp"
import qrGuestCardsImg from "@/assets/images/shop/products/qr-guest-cards.webp"
import receiptLabelImg from "@/assets/images/shop/products/receipt-label.webp"
import standardPackageLabelImg from "@/assets/images/shop/products/standard-package-label.webp"
import tableTalkerImg from "@/assets/images/shop/products/table-talker.webp"
import tableTentsImg from "@/assets/images/shop/products/table-tents.webp"
import trayLinerImg from "@/assets/images/shop/products/tray-liner.webp"
import windowStickersImg from "@/assets/images/shop/products/window-stickers.webp"

/**
 * Product artwork keyed by the legacy public path the backend seed stores
 * (`/assets/images/shop/products/{skuId or productKey}.webp`).
 * Keep in sync with `ShopProductArtworkPaths` in the backend.
 */
export const SHOP_PRODUCT_ARTWORK_BY_URL: Record<string, string> = {
  "/assets/images/shop/products/qr-counter-display.webp": qrCounterDisplayImg,
  "/assets/images/shop/products/table-tents.webp": tableTentsImg,
  "/assets/images/shop/products/table-talker.webp": tableTalkerImg,
  "/assets/images/shop/products/window-stickers.webp": windowStickersImg,
  "/assets/images/shop/products/qr-guest-cards.webp": qrGuestCardsImg,
  "/assets/images/shop/products/receipt-label.webp": receiptLabelImg,
  "/assets/images/shop/products/delivery-bag-seal.webp": deliveryBagSealImg,
  "/assets/images/shop/products/standard-package-label.webp":
    standardPackageLabelImg,
  "/assets/images/shop/products/drink-cup-seal.webp": drinkCupSealImg,
  "/assets/images/shop/products/pizza-box-label.webp": pizzaBoxLabelImg,
  "/assets/images/shop/products/poster-customer-club.webp":
    posterCustomerClubImg,
  "/assets/images/shop/products/tray-liner.webp": trayLinerImg,
}
