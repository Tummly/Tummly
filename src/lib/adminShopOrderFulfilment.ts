import type { AdminShopFulfilmentStatus } from "@/api/adminShopOrdersApi"

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
): { status: AdminShopFulfilmentStatus; label: string } | null {
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
