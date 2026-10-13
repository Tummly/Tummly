import tummlyBagImg from "@/assets/images/shop/tummly-bag.png"
import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"
import { resolveBrandLogoSrc } from "@/lib/brandLogo/resolveBrandLogoSrc"

const SHOP_IMAGE_BY_URL: Record<string, string> = {
  "/assets/images/shop/tummly-sticker.png": tummlyStickerImg,
  "/assets/images/shop/tummly-bag.png": tummlyBagImg,
}

/**
 * Resolve catalog image URLs for <img src>.
 * Legacy seed paths map to bundled Vite assets; blob proxy paths go to the API host.
 */
export function resolveShopCatalogImageSrc(imageUrl: string): string {
  if (!imageUrl) return tummlyStickerImg
  const mapped = SHOP_IMAGE_BY_URL[imageUrl]
  if (mapped) return mapped
  if (imageUrl.startsWith("/api/")) {
    return resolveBrandLogoSrc(imageUrl) ?? imageUrl
  }
  return imageUrl
}

export function resolveShopCatalogImageSrcs(
  urls: string[] | null | undefined,
  fallback: string
): string[] {
  const list = (urls ?? []).filter(Boolean).map(resolveShopCatalogImageSrc)
  if (list.length > 0) return list
  return [resolveShopCatalogImageSrc(fallback)]
}
