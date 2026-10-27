import { describe, expect, it } from "vitest"

import type { AdminShopCatalogSku } from "@/api/adminShopCatalogApi"
import { withSkuAvailableInShop } from "./adminShopCatalogAvailability"

function sku(skuId: string, availableInShop: boolean): AdminShopCatalogSku {
  return { skuId, availableInShop } as AdminShopCatalogSku
}

describe("withSkuAvailableInShop", () => {
  it("returns a new list with only the matching SKU changed", () => {
    const before = [sku("a", false), sku("b", false)]

    const after = withSkuAvailableInShop(before, "b", true)

    expect(after).not.toBe(before)
    expect(after[0]).toBe(before[0])
    expect(after[1]).toEqual(expect.objectContaining({ skuId: "b", availableInShop: true }))
    expect(before[1]!.availableInShop).toBe(false)
  })

  it("can revert a SKU to its previous value", () => {
    const toggled = withSkuAvailableInShop([sku("a", false)], "a", true)

    const reverted = withSkuAvailableInShop(toggled, "a", false)

    expect(reverted[0]!.availableInShop).toBe(false)
  })

  it("leaves the list unchanged when no SKU matches", () => {
    const before = [sku("a", false)]

    expect(withSkuAvailableInShop(before, "missing", true)).toEqual(before)
  })
})
