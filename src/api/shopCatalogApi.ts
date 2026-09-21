import axiosInstance from "@/api/axiosInstance"
import {
  mapShopCatalogDetail,
  mapShopCatalogListResponse,
} from "@/lib/operatorShop/mapShopCatalogItem"
import type {
  ShopCatalogDetailWire,
  ShopCatalogListItemWire,
  ShopProduct,
} from "@/lib/operatorShop/shopCatalogTypes"

type ShopCatalogListResponse = {
  success: boolean
  catalogVersion: string
  /** Seller effective rate (bps); 0 when TUMMLY_VAT_MODE_ACTIVE is false. */
  vatRateBps?: number
  items: ShopCatalogListItemWire[]
}

type ShopCatalogDetailResponse = {
  success: boolean
  item: ShopCatalogDetailWire
}

export async function fetchShopCatalog(
  locationId: number
): Promise<{
  catalogVersion: string
  vatRateBps: number
  products: ShopProduct[]
}> {
  const response = await axiosInstance.get<ShopCatalogListResponse>(
    "/shop/catalog",
    { params: { locationId } }
  )

  return {
    catalogVersion: response.data.catalogVersion,
    vatRateBps: response.data.vatRateBps ?? 0,
    products: mapShopCatalogListResponse(
      response.data.items,
      response.data.catalogVersion
    ),
  }
}

export async function fetchShopCatalogItem(
  locationId: number,
  skuId: string
): Promise<ShopProduct> {
  const response = await axiosInstance.get<ShopCatalogDetailResponse>(
    `/shop/catalog/${encodeURIComponent(skuId)}`,
    { params: { locationId } }
  )

  return mapShopCatalogDetail(response.data.item)
}
