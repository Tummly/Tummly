import { readShopApiErrorBody } from "@/lib/operatorShop/readShopApiErrorCode"

export type ShopUnavailableLine = { skuId: string; title: string }

export function buildShopReorderUnavailableMessage(
  lines: ReadonlyArray<ShopUnavailableLine>
): string {
  return `Some items are no longer available: ${lines.map((line) => line.title).join(", ")}`
}

/** `unavailableLines` from a reorder error body, or an empty list. */
export function readShopReorderUnavailableLines(
  error: unknown
): ShopUnavailableLine[] {
  const lines = readShopApiErrorBody(error)?.unavailableLines
  return Array.isArray(lines) ? (lines as ShopUnavailableLine[]) : []
}
