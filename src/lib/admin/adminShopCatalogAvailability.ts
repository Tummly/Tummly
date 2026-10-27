import type { AdminShopCatalogSku } from "@/api/adminShopCatalogApi"

/**
 * Immutable availability update for the catalog list. Used for optimistic
 * toggles and for rolling a toggle back when the request fails.
 */
export function withSkuAvailableInShop(
  skus: AdminShopCatalogSku[],
  skuId: string,
  availableInShop: boolean
): AdminShopCatalogSku[] {
  return skus.map((row) =>
    row.skuId === skuId ? { ...row, availableInShop } : row
  )
}
