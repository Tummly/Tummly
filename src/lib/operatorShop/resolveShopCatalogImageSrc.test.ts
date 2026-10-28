import { describe, expect, it } from "vitest"
import { resolveShopCatalogImageSrc } from "@/lib/operatorShop/resolveShopCatalogImageSrc"
import { resolveImageSrc } from "@/lib/operatorShop/mapShopRecommendations"

// Core SkuIds and interest ProductKeys seeded by the backend.
const PRODUCT_KEYS = [
  "qr-counter-display",
  "table-tents",
  "table-talker",
  "window-stickers",
  "qr-guest-cards",
  "receipt-label",
  "delivery-bag-seal",
  "standard-package-label",
  "drink-cup-seal",
  "pizza-box-label",
  "poster-customer-club",
  "tray-liner",
]

describe("resolveShopCatalogImageSrc", () => {
  it.each(PRODUCT_KEYS)("maps the %s artwork path to a bundled asset", (key) => {
    const path = `/assets/images/shop/products/${key}.webp`
    const src = resolveShopCatalogImageSrc(path)
    expect(src).not.toBe(path)
    expect(src).toContain(key)
  })

  it("recommendations resolve artwork the same way", () => {
    const path = "/assets/images/shop/products/table-tents.webp"
    expect(resolveImageSrc(path)).toBe(resolveShopCatalogImageSrc(path))
  })
})
