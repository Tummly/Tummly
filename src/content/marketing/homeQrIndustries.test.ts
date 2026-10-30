import { describe, expect, it } from "vitest"

import { HOME_INDUSTRIES, HOME_QR_MATERIALS } from "@/content/marketing/homePage"
import { PRODUCT_CAPTURE_MATERIALS } from "@/content/marketing/productPage"
import meta from "@/lib/marketingMediaMeta.json" with { type: "json" }

describe("HOME_QR_MATERIALS", () => {
  it("lists the Figma formats in order, Counter Display first", () => {
    expect(HOME_QR_MATERIALS.items.map((item) => item.label)).toEqual([
      "QR Counter Display",
      "QR Guest Cards",
      "QR Tray Liner",
      "QR Table Tent",
      "QR Table Talker",
      "QR Window Sticker",
      "QR Receipt Sticker",
      "QR Delivery Bag Seal",
      "QR Packaging Sticker",
      "QR Cup Seal",
      "QR Pizza Box Sticker",
      "QR Customer Club Poster",
    ])
  })

  it("reuses every Product Capture format, changing only order and label", () => {
    expect(HOME_QR_MATERIALS.items).toHaveLength(PRODUCT_CAPTURE_MATERIALS.length)
    for (const item of HOME_QR_MATERIALS.items) {
      const product = PRODUCT_CAPTURE_MATERIALS.find((m) => m.id === item.id)
      expect(product).toBeDefined()
      expect({ ...item, label: "" }).toEqual({ ...product, label: "" })
    }
  })

  it("uses unique ids and published media ids", () => {
    const ids = HOME_QR_MATERIALS.items.map((item) => item.id)
    expect(new Set(ids).size).toBe(ids.length)
    for (const item of HOME_QR_MATERIALS.items) {
      expect(meta).toHaveProperty([item.mediaId])
    }
  })
})

describe("HOME_INDUSTRIES", () => {
  it("has the four Figma cards", () => {
    expect(HOME_INDUSTRIES.title).toBe("Built for real restaurant days.")
    expect(HOME_INDUSTRIES.items.map((item) => item.title)).toEqual([
      "Takeaways & QSRs",
      "Cafés & bakeries",
      "Casual dining",
      "Growing restaurant groups",
    ])
  })
})
