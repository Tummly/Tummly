import tummlyBagImg from "@/assets/images/shop/tummly-bag.png"
import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"

const SHOP_IMAGE_BY_URL: Record<string, string> = {
  "/assets/images/shop/tummly-sticker.png": tummlyStickerImg,
  "/assets/images/shop/tummly-bag.png": tummlyBagImg,
}

export function resolveImageSrc(imageUrl: string): string {
  return SHOP_IMAGE_BY_URL[imageUrl] ?? imageUrl
}

export function formatShopPence(pence: number): string {
  return `£${(pence / 100).toFixed(2)}`
}
