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
  items: ShopCatalogListItemWire[]
}

type ShopCatalogDetailResponse = {
  success: boolean
  item: ShopCatalogDetailWire
}

export async function fetchShopCatalog(
  locationId: number
): Promise<{ catalogVersion: string; products: ShopProduct[] }> {
  const response = await axiosInstance.get<ShopCatalogListResponse>(
    "/api/shop/catalog",
    { params: { locationId } }
  )

  return {
    catalogVersion: response.data.catalogVersion,
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
    `/api/shop/catalog/${encodeURIComponent(skuId)}`,
    { params: { locationId } }
  )

  return mapShopCatalogDetail(response.data.item)
}
