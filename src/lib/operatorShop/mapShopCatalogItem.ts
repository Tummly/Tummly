import tummlyBagImg from "@/assets/images/shop/tummly-bag.png"
import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"
import type {
  ShopCatalogDetailWire,
  ShopCatalogListItemWire,
  ShopProduct,
} from "@/lib/operatorShop/shopCatalogTypes"

const SHOP_IMAGE_BY_URL: Record<string, string> = {
  "/assets/images/shop/tummly-sticker.png": tummlyStickerImg,
  "/assets/images/shop/tummly-bag.png": tummlyBagImg,
}

function resolveImageSrc(imageUrl: string): string {
  return SHOP_IMAGE_BY_URL[imageUrl] ?? imageUrl
}

function mapListItemToProduct(
  item: ShopCatalogListItemWire,
  catalogVersion: string
): ShopProduct {
  return {
    id: item.skuId,
    title: item.title,
    category: item.category,
    description: item.description,
    material: "",
    dimensions: "",
    price: item.unitNetPence / 100,
    unitNetPence: item.unitNetPence,
    currency: item.currency,
    qrType: item.qrType,
    catalogVersion,
    minOrderQty: 1,
    mintOnShopFulfilment: false,
    isPlanIncluded: item.isPlanIncluded === true ? true : undefined,
    popularBadge: item.popularBadge ?? undefined,
    imageSrc: resolveImageSrc(item.imageUrl),
  }
}

export function mapShopCatalogListResponse(
  items: ShopCatalogListItemWire[],
  catalogVersion: string
): ShopProduct[] {
  return items.map((item) => mapListItemToProduct(item, catalogVersion))
}

export function mapShopCatalogDetail(item: ShopCatalogDetailWire): ShopProduct {
  return {
    id: item.skuId,
    title: item.title,
    category: item.category,
    description: item.description,
    material: item.material,
    dimensions: item.dimensions,
    price: item.unitNetPence / 100,
    unitNetPence: item.unitNetPence,
    currency: item.currency,
    qrType: item.qrType,
    catalogVersion: item.catalogVersion,
    minOrderQty: item.minOrderQty,
    mintOnShopFulfilment: item.mintOnShopFulfilment,
    isPlanIncluded: item.isPlanIncluded === true ? true : undefined,
    popularBadge: item.popularBadge ?? undefined,
    imageSrc: resolveImageSrc(item.imageUrl),
  }
}
