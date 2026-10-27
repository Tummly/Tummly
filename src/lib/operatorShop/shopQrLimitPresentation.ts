import { readShopApiErrorBody, readShopApiErrorCode } from "@/lib/operatorShop/readShopApiErrorCode"

export const SHOP_QR_LIMIT_COPY = {
  title: "QR code limit reached",
  backToOrder: "Back to order",
  reviewPlanOptions: "Review plan options",
  manageQrCodes: "Manage QR codes",
} as const

export function buildShopQrLimitBody(limit: number): string {
  return `Your plan allows ${limit} active QR codes at this location. You can use an existing active QR code or upgrade your plan to add another.`
}

/** Plan limit from a 409 `qr_limit_reached` body, or null for any other error. */
export function readShopQrLimitError(error: unknown): number | null {
  if (readShopApiErrorCode(error) !== "qr_limit_reached") {
    return null
  }
  const limit = readShopApiErrorBody(error)?.limit
  return typeof limit === "number" ? limit : null
}
