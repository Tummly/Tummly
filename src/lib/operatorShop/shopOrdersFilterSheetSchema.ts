/** Shop Orders filter schema, sort options, and filtering/sorting helpers. */

import type { ShopOrderDetailWire } from "@/api/shopOrdersApi"
import type {
  FilterSheetSchema,
  OperatorFilterSelection,
  SchemaOption,
} from "@/lib/operatorFilterSheet"

export type ShopOrdersFulfilmentStatusId =
  | "processing"
  | "in_transit"
  | "delivered"
  | "cancelled"

export const SHOP_ORDERS_FULFILMENT_STATUS_LABELS: Record<
  ShopOrdersFulfilmentStatusId,
  string
> = {
  processing: "Processing",
  in_transit: "Dispatched",
  delivered: "Delivered",
  cancelled: "Cancelled",
}

export type ShopOrdersPaymentStatusId = "paid" | "refunded"

export const SHOP_ORDERS_PAYMENT_STATUS_LABELS: Record<
  ShopOrdersPaymentStatusId,
  string
> = {
  paid: "Paid",
  refunded: "Refunded",
}

export type ShopOrdersMaterialTypeId =
  | "table-tents"
  | "counter-cards"
  | "window-stickers"
  | "packaging-stickers"
  | "receipt-stickers"
  | "delivery-inserts"

export const SHOP_ORDERS_MATERIAL_TYPE_LABELS: Record<
  ShopOrdersMaterialTypeId,
  string
> = {
  "table-tents": "Table tents",
  "counter-cards": "Counter cards",
  "packaging-stickers": "Packaging stickers",
  "receipt-stickers": "Receipt stickers",
  "window-stickers": "Window stickers",
  "delivery-inserts": "Delivery inserts",
}

export type ShopOrdersSortId =
  | "newest"
  | "oldest"
  | "highest-total"
  | "lowest-total"
  | "status"

export const SHOP_ORDERS_SORT_OPTIONS: Record<ShopOrdersSortId, string> = {
  newest: "Newest first",
  oldest: "Oldest first",
  "highest-total": "Highest total",
  "lowest-total": "Lowest total",
  status: "Status",
}

function toOptions<TId extends string>(
  labels: Record<TId, string>
): SchemaOption[] {
  return Object.entries(labels).map(([id, label]) => ({
    id,
    label: label as string,
  }))
}

export function shopOrdersFilterSheetSchema(
  catalog: {
    locations?: readonly SchemaOption[]
  } = {}
): FilterSheetSchema {
  return {
    fields: [
      {
        id: "location",
        kind: "location-scope",
        label: "Location",
        locations: catalog.locations ?? [],
      },
      {
        id: "fulfilmentStatus",
        kind: "multi-select",
        label: "Fulfilment status",
        chipKind: "fulfilment",
        options: toOptions(SHOP_ORDERS_FULFILMENT_STATUS_LABELS),
      },
      {
        id: "paymentStatus",
        kind: "multi-select",
        label: "Payment status",
        chipKind: "payment",
        options: toOptions(SHOP_ORDERS_PAYMENT_STATUS_LABELS),
      },
      {
        id: "orderDate",
        kind: "date",
        label: "Order date",
        hasAxis: false,
        presetLabels: {
          "last-30": "Last 30 days",
          "last-90": "Last 90 days",
          "this-year": "This year",
        },
      },
      {
        id: "materialType",
        kind: "multi-select",
        label: "Material type",
        chipKind: "material",
        options: toOptions(SHOP_ORDERS_MATERIAL_TYPE_LABELS),
      },
      {
        id: "sort",
        kind: "multi-select",
        label: "Sort",
        chipKind: "sort",
        options: toOptions(SHOP_ORDERS_SORT_OPTIONS),
      },
    ],
  }
}

export function getShopOrdersSortId(
  selection: OperatorFilterSelection
): ShopOrdersSortId {
  const sortField = selection.sort
  if (sortField?.kind === "multi-select" && sortField.ids.length > 0) {
    const lastId = sortField.ids[sortField.ids.length - 1]
    if (lastId in SHOP_ORDERS_SORT_OPTIONS) {
      return lastId as ShopOrdersSortId
    }
  }
  return "newest"
}

export type DetailedShopOrder = {
  id: string
  orderNumber: string
  orderDate: string
  isoDate: string
  locationId?: string | number
  locationName: string
  materials: string
  materialTypes: ShopOrdersMaterialTypeId[]
  placedBy: string
  total: string
  totalNumeric: number
  paymentStatus: "Paid" | "Refunded"
  fulfilmentStatus: "Processing" | "Dispatched" | "Delivered" | "Cancelled"
  updatedDate: string
  items?: string[]
  canCancel?: boolean
  cancelBlockReason?: string | null
  detail?: ShopOrderDetailWire
}

/** Matches status string to enum ID. */
export function normalizeFulfilmentStatusToId(
  status: DetailedShopOrder["fulfilmentStatus"]
): ShopOrdersFulfilmentStatusId {
  switch (status) {
    case "Processing":
      return "processing"
    case "Dispatched":
      return "in_transit"
    case "Delivered":
      return "delivered"
    case "Cancelled":
      return "cancelled"
  }
}

/** Matches payment status string to enum ID. */
export function normalizePaymentStatusToId(
  status: DetailedShopOrder["paymentStatus"]
): ShopOrdersPaymentStatusId {
  switch (status) {
    case "Paid":
      return "paid"
    case "Refunded":
      return "refunded"
  }
}

/** Checks whether an order matches the active filters. */
export function matchesShopOrderFilters(
  order: DetailedShopOrder,
  selection: OperatorFilterSelection,
  refDate: Date = new Date("2026-08-30T12:00:00Z")
): boolean {
  // Location filter
  const locationField = selection.location
  if (locationField?.kind === "location-scope") {
    const override = locationField.value
    if (override.kind === "individual" && override.locationIds.length > 0) {
      const orderLocId = String(order.locationId ?? "")
      const orderLocName = order.locationName.toLowerCase()
      const matchesLoc = override.locationIds.some(
        (id) =>
          id === orderLocId ||
          orderLocName.includes(id.toLowerCase())
      )
      if (!matchesLoc) {
        return false
      }
    }
  }

  // Fulfilment status filter
  const fulfilmentField = selection.fulfilmentStatus
  if (fulfilmentField?.kind === "multi-select" && fulfilmentField.ids.length > 0) {
    const orderFulfilmentId = normalizeFulfilmentStatusToId(order.fulfilmentStatus)
    if (!fulfilmentField.ids.includes(orderFulfilmentId)) {
      return false
    }
  }

  // Payment status filter
  const paymentField = selection.paymentStatus
  if (paymentField?.kind === "multi-select" && paymentField.ids.length > 0) {
    const orderPaymentId = normalizePaymentStatusToId(order.paymentStatus)
    if (!paymentField.ids.includes(orderPaymentId)) {
      return false
    }
  }

  // Material type filter
  const materialField = selection.materialType
  if (materialField?.kind === "multi-select" && materialField.ids.length > 0) {
    const hasMatchingMaterial = materialField.ids.some((id) =>
      order.materialTypes.includes(id as ShopOrdersMaterialTypeId)
    )
    if (!hasMatchingMaterial) {
      return false
    }
  }

  // Order date filter
  const dateField = selection.orderDate
  if (dateField?.kind === "date" && dateField.value.kind !== "none") {
    const orderTimestamp = new Date(order.isoDate).getTime()
    const nowTimestamp = refDate.getTime()
    const oneDayMs = 24 * 60 * 60 * 1000

    if (dateField.value.kind === "preset") {
      const preset = dateField.value.preset
      if (preset === "last-30") {
        const threshold = nowTimestamp - 30 * oneDayMs
        if (orderTimestamp < threshold) return false
      } else if (preset === "last-90") {
        const threshold = nowTimestamp - 90 * oneDayMs
        if (orderTimestamp < threshold) return false
      } else if (preset === "this-year") {
        const refYear = refDate.getUTCFullYear()
        const orderYear = new Date(order.isoDate).getUTCFullYear()
        if (orderYear !== refYear) return false
      }
    } else if (dateField.value.kind === "custom") {
      const fromMs = new Date(dateField.value.dateFrom).getTime()
      const toMs = new Date(dateField.value.dateTo).getTime() + oneDayMs - 1
      if (orderTimestamp < fromMs || orderTimestamp > toMs) {
        return false
      }
    }
  }

  return true
}

/** Sorts orders based on sortId. */
export function sortShopOrders(
  orders: DetailedShopOrder[],
  sortId: ShopOrdersSortId
): DetailedShopOrder[] {
  const sorted = [...orders]
  switch (sortId) {
    case "newest":
      return sorted.sort(
        (a, b) => new Date(b.isoDate).getTime() - new Date(a.isoDate).getTime()
      )
    case "oldest":
      return sorted.sort(
        (a, b) => new Date(a.isoDate).getTime() - new Date(b.isoDate).getTime()
      )
    case "highest-total":
      return sorted.sort((a, b) => b.totalNumeric - a.totalNumeric)
    case "lowest-total":
      return sorted.sort((a, b) => a.totalNumeric - b.totalNumeric)
    case "status":
      return sorted.sort((a, b) =>
        a.fulfilmentStatus.localeCompare(b.fulfilmentStatus)
      )
    default:
      return sorted
  }
}
