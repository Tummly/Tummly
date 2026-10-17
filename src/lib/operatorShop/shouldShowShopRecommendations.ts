import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"

/**
 * Recommended for Location needs more than one buyable QR material.
 * Coming-soon (not AvailableInShop) tiles do not count.
 */
export function shouldShowShopRecommendations(
  catalogProducts: readonly ShopProduct[]
): boolean {
  const availableCount = catalogProducts.filter(
    (product) => product.availableInShop
  ).length
  return availableCount > 1
}
