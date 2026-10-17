/** Two large-screen rows (`lg:grid-cols-4`) per catalog page. */
export const SHOP_CATALOG_PAGE_SIZE = 8

/** Previous / Next only when the list needs more than one page. */
export function shouldShowShopCatalogPagination(productCount: number): boolean {
  return productCount > SHOP_CATALOG_PAGE_SIZE
}
