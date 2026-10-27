import type {
  AdminShopFulfilmentStatus,
  AdminShopOrderListItem,
} from "@/api/adminShopOrdersApi"

export const ADMIN_SHOP_FULFILMENT_STATUS_LABELS: Record<
  AdminShopFulfilmentStatus,
  string
> = {
  processing: "Processing",
  in_transit: "Dispatched",
  delivered: "Delivered",
  cancelled: "Cancelled",
}

export const ADMIN_SHOP_FULFILMENT_FILTER_OPTIONS: Array<{
  id: AdminShopFulfilmentStatus | "all"
  label: string
}> = [
  { id: "processing", label: "Processing" },
  { id: "in_transit", label: "Dispatched" },
  { id: "delivered", label: "Delivered" },
  { id: "cancelled", label: "Cancelled" },
  { id: "all", label: "All statuses" },
]

export const ALL_ADMIN_SHOP_FULFILMENT_STATUSES: AdminShopFulfilmentStatus[] = [
  "processing",
  "in_transit",
  "delivered",
  "cancelled",
]

export function adminShopFulfilmentLabel(status: string): string {
  if (status in ADMIN_SHOP_FULFILMENT_STATUS_LABELS) {
    return ADMIN_SHOP_FULFILMENT_STATUS_LABELS[
      status as AdminShopFulfilmentStatus
    ]
  }
  return status
}

export function formatAdminShopGbpFromPence(pence: number): string {
  return new Intl.NumberFormat("en-GB", {
    style: "currency",
    currency: "GBP",
  }).format(pence / 100)
}

export function nextAdminShopFulfilmentAction(
  status: string
): { status: "in_transit" | "delivered"; label: string } | null {
  if (status === "processing") {
    return { status: "in_transit", label: "Mark as Dispatched" }
  }
  if (status === "in_transit") {
    return { status: "delivered", label: "Mark as Delivered" }
  }
  return null
}

export function canEditAdminShopTrackingUrl(status: string): boolean {
  return status === "processing" || status === "in_transit"
}

export function canEditAdminShopOpsNotes(status: string): boolean {
  return (
    status === "processing" ||
    status === "in_transit" ||
    status === "delivered"
  )
}

export type AdminShopOrderReviewNotice = {
  title: string
  lines: string[]
}

const ADMIN_SHOP_REVIEW_REASON_LABELS: Record<string, string> = {
  qr_capacity_unavailable_after_payment:
    "paid, but the plan had no room for a new QR code.",
}

function formatReviewSince(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return iso
  }
  return `${date.toISOString().slice(0, 16).replace("T", " ")} UTC`
}

/**
 * Warning for Admin when a paid Shop order needs a manual look: the review
 * reason, or QR slot reservations held in `review`. Null when none.
 */
export function adminShopOrderReviewNotice(
  order: Pick<AdminShopOrderListItem, "reviewReason" | "reviewQrReservations">
): AdminShopOrderReviewNotice | null {
  const reservations = order.reviewQrReservations ?? []
  const reason = order.reviewReason?.trim() || null

  if (!reason && reservations.length === 0) {
    return null
  }

  const title = reason
    ? `Needs review: ${ADMIN_SHOP_REVIEW_REASON_LABELS[reason] ?? reason}`
    : "Needs review: a QR slot has been held for over 24 hours."

  return {
    title,
    lines: reservations.map(
      (row) =>
        `${row.qrType} held since ${formatReviewSince(row.createdAtUtc)} (${
          row.lastReconcileError?.trim() || "no error recorded"
        })`
    ),
  }
}
