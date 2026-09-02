import axiosInstance from "@/api/axiosInstance"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"
import { findShopProductById } from "@/lib/operatorShop/shopCatalogTypes"
import type { CartItem } from "@/components/dashboard/operator/Shop/ShopCartDrawer"

export type ShopCartLineWire = {
  skuId: string
  quantity: number
  title: string
  unitNetPence: number
  lineNetPence: number
}

export type ShopCartWire = {
  locationId: number
  lines: ShopCartLineWire[]
  materialsNetPence: number
  currency: string
}

export async function fetchShopCart(
  locationId: number
): Promise<ShopCartWire> {
  const response = await axiosInstance.get<ShopCartWire>("/api/shop/cart", {
    params: { locationId },
  })
  return response.data
}

export async function upsertShopCartLine(
  locationId: number,
  skuId: string,
  quantity: number
): Promise<ShopCartWire> {
  const response = await axiosInstance.put<ShopCartWire>(
    "/api/shop/cart/lines",
    { locationId, skuId, quantity }
  )
  return response.data
}

export async function deleteShopCartLine(
  locationId: number,
  skuId: string
): Promise<ShopCartWire> {
  const response = await axiosInstance.delete<ShopCartWire>(
    `/api/shop/cart/lines/${encodeURIComponent(skuId)}`,
    { params: { locationId } }
  )
  return response.data
}

export function mapShopCartToItems(
  cart: ShopCartWire,
  catalogProducts: ShopProduct[]
): CartItem[] {
  return cart.lines.map((line) => {
    const fromCatalog = findShopProductById(catalogProducts, line.skuId)
    if (fromCatalog) {
      return { product: fromCatalog, quantity: line.quantity }
    }

    const fallback: ShopProduct = {
      id: line.skuId,
      title: line.title,
      category: "tabletop",
      description: "",
      material: "",
      dimensions: "",
      price: line.unitNetPence / 100,
      unitNetPence: line.unitNetPence,
      currency: cart.currency,
      qrType: "CounterCard",
      catalogVersion: "",
      minOrderQty: 1,
      mintOnShopFulfilment: false,
      imageSrc: "",
    }
    return { product: fallback, quantity: line.quantity }
  })
}
