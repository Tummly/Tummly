import { describe, expect, it } from "vitest"
import {
  SHOP_CATALOG_PAGE_SIZE,
  shouldShowShopCatalogPagination,
} from "@/lib/operatorShop/shopCatalogPagination"

describe("shouldShowShopCatalogPagination", () => {
  it("hides when product count is at or below page size", () => {
    expect(shouldShowShopCatalogPagination(0)).toBe(false)
    expect(shouldShowShopCatalogPagination(SHOP_CATALOG_PAGE_SIZE)).toBe(false)
  })

  it("shows when product count exceeds page size", () => {
    expect(shouldShowShopCatalogPagination(SHOP_CATALOG_PAGE_SIZE + 1)).toBe(
      true
    )
  })
})
