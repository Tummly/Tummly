import axiosInstance from "@/api/axiosInstance"
import { resolveImageSrc } from "@/lib/operatorShop/mapShopRecommendations"

export type ShopLocationDetailsPayload = {
  tableCount: number
  counterCount: number
  entranceCount: number
  secondaryEntranceCount: number
  takeawayVolume: string
  promptLocations: string
  existingMaterials: string
}

export type ShopRecommendationLineWire = {
  skuId: string
  quantity: number
  title: string
  unitNetPence: number
  imageUrl: string
  allocationText: string
  reason: string
}

export type ShopRecommendationsSummaryWire = {
  materialTypeCount: number
  totalPieces: number
  materialsNetPence: number
  currency: string
}

export type ShopLocationRecommendationsWire = {
  locationId: number
  needsLocationDetails: boolean
  window: { from: string; to: string }
  basedOn?: {
    tableCount: number
    counterCount: number
    entranceCount: number
    secondaryEntranceCount: number
    takeawayVolume: string
    promptLocations: string[]
    existingMaterials: string
  }
  lines: ShopRecommendationLineWire[]
  summary: ShopRecommendationsSummaryWire
}

export type ShopRecommendationLine = ShopRecommendationLineWire & {
  imageSrc: string
  unitPriceGbp: number
  lineNetPence: number
}

export type ShopLocationRecommendations = Omit<
  ShopLocationRecommendationsWire,
  "lines"
> & {
  lines: ShopRecommendationLine[]
}

function mapRecommendations(
  wire: ShopLocationRecommendationsWire
): ShopLocationRecommendations {
  return {
    ...wire,
    lines: wire.lines.map((line) => ({
      ...line,
      imageSrc: resolveImageSrc(line.imageUrl),
      unitPriceGbp: line.unitNetPence / 100,
      lineNetPence: line.unitNetPence * line.quantity,
    })),
  }
}

export async function fetchShopLocationRecommendations(
  locationId: number
): Promise<ShopLocationRecommendations> {
  const response = await axiosInstance.get<ShopLocationRecommendationsWire>(
    `/shop/locations/${locationId}/recommendations`
  )
  return mapRecommendations(response.data)
}

export async function saveShopLocationDetails(
  locationId: number,
  payload: ShopLocationDetailsPayload
): Promise<void> {
  await axiosInstance.put(
    `/shop/locations/${locationId}/details`,
    payload
  )
}
