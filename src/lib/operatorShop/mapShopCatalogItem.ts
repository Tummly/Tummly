import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"
import type {
  ShopCatalogDetailWire,
  ShopCatalogListItemWire,
  ShopProduct,
} from "@/lib/operatorShop/shopCatalogTypes"
import { resolveShopCatalogImageSrcs } from "@/lib/operatorShop/resolveShopCatalogImageSrc"

function mapListItemToProduct(
  item: ShopCatalogListItemWire,
  catalogVersion: string
): ShopProduct {
  const imageSrcs = resolveShopCatalogImageSrcs(item.imageUrls, item.imageUrl)
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
    tags: item.tags ?? [],
    imageSrc: imageSrcs[0] ?? tummlyStickerImg,
    imageSrcs,
    aboutBody: "",
    specifications: [],
  }
}

export function mapShopCatalogListResponse(
  items: ShopCatalogListItemWire[],
  catalogVersion: string
): ShopProduct[] {
  return items.map((item) => mapListItemToProduct(item, catalogVersion))
}

export function mapShopCatalogDetail(item: ShopCatalogDetailWire): ShopProduct {
  const imageSrcs = resolveShopCatalogImageSrcs(item.imageUrls, item.imageUrl)
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
    tags: item.tags ?? [],
    imageSrc: imageSrcs[0] ?? tummlyStickerImg,
    imageSrcs,
    aboutBody: item.aboutBody?.trim() ?? "",
    specifications: item.specifications ?? [],
  }
}
