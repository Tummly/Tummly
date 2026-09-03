import type { CapturePlacementQrType } from "@/types/dashboard"

export type ShopCatalogCategory =
  | "tabletop"
  | "window"
  | "payment"
  | "takeaway"
  | "delivery"

export type ShopProduct = {
  id: string
  title: string
  category: ShopCatalogCategory
  description: string
  material: string
  dimensions: string
  price: number
  unitNetPence: number
  currency: string
  qrType: CapturePlacementQrType
  catalogVersion: string
  minOrderQty: number
  mintOnShopFulfilment: boolean
  isPlanIncluded?: boolean
  popularBadge?: string
  imageSrc: string
}

export type ShopCatalogListItemWire = {
  skuId: string
  title: string
  category: ShopCatalogCategory
  description: string
  unitNetPence: number
  currency: string
  imageUrl: string
  qrType: CapturePlacementQrType
  isPlanIncluded?: boolean | null
  popularBadge?: string | null
}

export type ShopCatalogDetailWire = ShopCatalogListItemWire & {
  material: string
  dimensions: string
  minOrderQty: number
  catalogVersion: string
  mintOnShopFulfilment: boolean
}

export function findShopProductById(
  products: ShopProduct[],
  skuId: string
): ShopProduct | undefined {
  return products.find((product) => product.id === skuId)
}
