import type {
  AdminShopOrderListItem,
  AdminShopPrintAssetQrType,
} from "@/api/adminShopOrdersApi"
import {
  type AdminShopFulfilmentAction,
  type AdminShopFulfilmentEvidence,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"

const LONDON_DATE_TIME_FORMAT = new Intl.DateTimeFormat("en-GB", {
  timeZone: "Europe/London",
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
})

export type AdminShopOrderTableLine = {
  key: string
  product: string
  quantity: number
}

/** Product and quantity cells: one row per order line, in the same order. */
export function adminShopOrderTableLines(
  order: Pick<AdminShopOrderListItem, "lines">
): AdminShopOrderTableLine[] {
  return order.lines.map((line, index) => ({
    key: `${line.catalogSkuId}-${index}`,
    product: line.titleSnapshot,
    quantity: line.quantity,
  }))
}

/** Order list date: `dd MMM yyyy, HH:mm` in Europe/London. */
export function formatAdminShopOrderDate(createdAtUtc: string): string {
  const date = new Date(createdAtUtc)
  if (Number.isNaN(date.getTime())) {
    return createdAtUtc
  }
  return LONDON_DATE_TIME_FORMAT.format(date)
}

export function adminShopOrderPaymentLabel(
  order: Pick<AdminShopOrderListItem, "paymentStatus" | "isComplimentary">
): "Paid" | "Unpaid" | "Refunded" | "Free" {
  if (order.isComplimentary) {
    return "Free"
  }
  if (order.paymentStatus === "paid") {
    return "Paid"
  }
  if (order.paymentStatus === "refunded") {
    return "Refunded"
  }
  return "Unpaid"
}


export type AdminShopOrderRowActions = {
  /** Assets that are Ready and approved; download is enabled when non-empty. */
  downloadQrTypes: AdminShopPrintAssetQrType[]
  /** Assets that are Ready and not yet approved. */
  approveQrTypes: AdminShopPrintAssetQrType[]
  nextFulfilmentAction: AdminShopFulfilmentAction | null
}

export function adminShopOrderRowActions(
  order: AdminShopFulfilmentEvidence &
    Pick<AdminShopOrderListItem, "printAssets">
): AdminShopOrderRowActions {
  const downloadQrTypes: AdminShopPrintAssetQrType[] = []
  const approveQrTypes: AdminShopPrintAssetQrType[] = []
  for (const asset of order.printAssets) {
    if (asset.status !== "Ready") {
      continue
    }
    if (asset.approved) {
      downloadQrTypes.push(asset.qrType)
    } else {
      approveQrTypes.push(asset.qrType)
    }
  }

  const nextFulfilmentAction = nextAdminShopFulfilmentAction(order)

  return { downloadQrTypes, approveQrTypes, nextFulfilmentAction }
}
