import { resolveShopCatalogImageSrc } from "@/lib/operatorShop/resolveShopCatalogImageSrc"

export function resolveImageSrc(imageUrl: string): string {
  return resolveShopCatalogImageSrc(imageUrl)
}

export function formatShopPence(pence: number): string {
  return `£${(pence / 100).toFixed(2)}`
}
