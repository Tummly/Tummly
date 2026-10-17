import { describe, expect, it } from "vitest"
import { shouldShowShopRecommendations } from "@/lib/operatorShop/shouldShowShopRecommendations"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"

function product(partial: Partial<ShopProduct> & { id: string }): ShopProduct {
  return {
    title: partial.title ?? partial.id,
    category: "tabletop",
    description: "",
    material: "",
    dimensions: "",
    price: 0,
    unitNetPence: 0,
    currency: "GBP",
    qrType: "Static",
    catalogVersion: "1",
    minOrderQty: 1,
    packs: [],
    deliveryNetPence: 0,
    mintOnShopFulfilment: false,
    availableInShop: false,
    tags: [],
    imageSrc: "",
    imageSrcs: [],
    aboutBody: "",
    specifications: [],
    ...partial,
  }
}

describe("shouldShowShopRecommendations", () => {
  it("hides when no available materials", () => {
    expect(shouldShowShopRecommendations([])).toBe(false)
  })

  it("hides when only one AvailableInShop material", () => {
    expect(
      shouldShowShopRecommendations([
        product({ id: "counter", availableInShop: true }),
        product({ id: "coming-soon", availableInShop: false }),
      ])
    ).toBe(false)
  })

  it("shows when two or more AvailableInShop materials", () => {
    expect(
      shouldShowShopRecommendations([
        product({ id: "a", availableInShop: true }),
        product({ id: "b", availableInShop: true }),
        product({ id: "soon", availableInShop: false }),
      ])
    ).toBe(true)
  })
})
