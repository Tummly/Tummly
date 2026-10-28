import axiosInstance from "./axiosInstance"

export type AdminShopFulfilmentStatus =
  | "processing"
  | "in_transit"
  | "delivered"
  | "cancelled"

export type AdminShopOrderLineSummary = {
  catalogSkuId: string
  titleSnapshot: string
  quantity: number
  unitNetPence: number
  lineNetPence: number
}

export type AdminShopPrintAssetQrType =
  | "CounterCard"
  | "TableTent"
  | "WindowSticker"
  | "OfferCard"

export type AdminShopPrintAsset = {
  qrType: AdminShopPrintAssetQrType
  /** Ordered SKU title (for example "QR Counter Display"). */
  productTitle: string
  quantity: number
  status: "Preparing" | "Ready" | "Failed"
  fileName: string | null
  lastError: string | null
  approved: boolean
  printRoute?: string | null
}

export type AdminShopPrintAssetPreview = {
  productTitle: string
  locationId: number
  locationName: string
  placement: string
  quantity: number
  qrDestinationUrl: string
  offerHeadline: string | null
  printRoute: string
  approvedAtUtc: string | null
}

export type AdminShopQrReservationReview = {
  status: string
  qrType: string
  lastReconcileError: string | null
  createdAtUtc: string
}

export type AdminShopOrderListItem = {
  id: string
  orderNumber: string
  restaurantId: number
  restaurantName: string
  locationId: number
  locationNameSnapshot: string
  fulfilmentStatus: AdminShopFulfilmentStatus | string
  paymentStatus: string
  isComplimentary?: boolean
  revolutOrderId: string | null
  trackingUrl: string | null
  opsNotes: string | null
  paidAtUtc: string | null
  /** ISO UTC when the order was placed. */
  createdAtUtc: string
  /** ISO UTC when Admin recorded that preparation started; null until then. */
  preparationStartedAtUtc: string | null
  /** ISO UTC when Admin recorded that production started; null until then. */
  productionStartedAtUtc: string | null
  /** ISO UTC of the dispatch event (carrier handoff); null until dispatched. */
  dispatchedAtUtc: string | null
  dispatchCarrierName: string | null
  dispatchCarrierReference: string | null
  /** How the dispatch evidence was recorded, for example `legacy_backfill`. */
  dispatchSource?: string | null
  /** Admin who recorded the dispatch evidence; null for legacy dispatches. */
  dispatchedByAdminUserId?: number | null
  /** ISO UTC of the delivery confirmation; null until delivered. */
  deliveredAtUtc: string | null
  grossPence: number
  lines: AdminShopOrderLineSummary[]
  printAssets: AdminShopPrintAsset[]
  /** Set when a paid order needs Admin attention (for example no QR room). */
  reviewReason?: string | null
  /** QR slot reservations held in `review` after 24 h without a final answer. */
  reviewQrReservations?: AdminShopQrReservationReview[]
}

export type AdminShopOrderListResponse = {
  items: AdminShopOrderListItem[]
  totalCount: number
  page: number
  pageSize: number
}

export type AdminShopOrdersListParams = {
  page?: number
  pageSize?: number
  q?: string
  restaurantId?: number
  fulfilmentStatus?: AdminShopFulfilmentStatus[]
  /** Only orders stored as Dispatched or Delivered without carrier evidence. */
  dispatchEvidenceMissing?: boolean
}

/** Status moves use the transition calls below. The patch sets tracking and notes only. */
export type AdminShopOrderFulfilmentPatch = {
  trackingUrl?: string | null
  opsNotes?: string | null
}

const arrayParamsSerializer = {
  serialize: (rawParams: Record<string, unknown>) => {
    const search = new URLSearchParams()
    for (const [key, value] of Object.entries(rawParams)) {
      if (value == null || value === "") continue
      if (Array.isArray(value)) {
        for (const item of value) {
          if (item == null || item === "") continue
          search.append(key, String(item))
        }
        continue
      }
      search.append(key, String(value))
    }
    return search.toString()
  },
}

export async function fetchAdminShopOrders(
  params: AdminShopOrdersListParams = {}
): Promise<AdminShopOrderListResponse> {
  const response = await axiosInstance.get<AdminShopOrderListResponse>(
    "/admin/shop-orders",
    {
      params,
      paramsSerializer: arrayParamsSerializer,
    }
  )
  return response.data
}

export async function patchAdminShopOrderFulfilment(
  orderId: string,
  patch: AdminShopOrderFulfilmentPatch
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.patch<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/fulfilment`,
    patch
  )
  return response.data
}

export async function markShopOrderPreparationStarted(
  orderId: string
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.post<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/preparation-started`
  )
  return response.data
}

export async function markShopOrderProductionStarted(
  orderId: string
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.post<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/production-started`
  )
  return response.data
}

export type AdminShopDispatchBody = {
  carrierName: string
  carrierReference: string
  /** Optional supporting link; a dispatch never needs one. */
  trackingUrl?: string | null
}

export async function markShopOrderDispatched(
  orderId: string,
  body: AdminShopDispatchBody
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.post<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/dispatched`,
    {
      carrierName: body.carrierName.trim(),
      carrierReference: body.carrierReference.trim(),
      trackingUrl: body.trackingUrl?.trim() || null,
    }
  )
  return response.data
}

export async function markShopOrderDelivered(
  orderId: string
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.post<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/delivered`
  )
  return response.data
}

export type AdminShopDispatchEvidenceBody = {
  carrierName: string
  carrierReference: string
  /** ISO UTC of the actual dispatch time. */
  dispatchedAtUtc: string
}

/**
 * Backfills carrier evidence for a legacy dispatch. The order keeps its
 * status and tracking URL.
 */
export async function recordShopOrderDispatchEvidence(
  orderId: string,
  body: AdminShopDispatchEvidenceBody
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.post<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/dispatch-evidence`,
    {
      carrierName: body.carrierName.trim(),
      carrierReference: body.carrierReference.trim(),
      dispatchedAtUtc: body.dispatchedAtUtc,
    }
  )
  return response.data
}

/** Returns an order with unsupported dispatch or delivery evidence to its last substantiated state. */
export async function correctShopOrderStatus(
  orderId: string,
  reason: string
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.post<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/status-correction`,
    { reason: reason.trim() }
  )
  return response.data
}

export type AdminShopForceCancelBody = {
  reason: string
  skipRefund?: boolean
}

export async function forceCancelShopOrder(
  orderId: string,
  body: AdminShopForceCancelBody
): Promise<AdminShopOrderListItem> {
  const response = await axiosInstance.post<AdminShopOrderListItem>(
    `/admin/shop-orders/${orderId}/force-cancel`,
    {
      reason: body.reason,
      skipRefund: body.skipRefund ?? false,
    }
  )
  return response.data
}

export async function downloadAdminShopOrderPrintAsset(
  orderId: string,
  qrType: AdminShopPrintAsset["qrType"]
): Promise<Blob> {
  const response = await axiosInstance.get<Blob>(
    `/admin/shop-orders/${orderId}/print-assets/${qrType}/download`,
    { responseType: "blob" }
  )
  return response.data
}

export async function previewAdminShopOrderPrintAsset(
  orderId: string,
  qrType: AdminShopPrintAsset["qrType"]
): Promise<AdminShopPrintAssetPreview> {
  const response = await axiosInstance.get<AdminShopPrintAssetPreview>(
    `/admin/shop-orders/${orderId}/print-assets/${qrType}/preview`
  )
  return response.data
}

export async function approveAdminShopOrderPrintAsset(
  orderId: string,
  qrType: AdminShopPrintAsset["qrType"]
): Promise<AdminShopPrintAsset> {
  const response = await axiosInstance.post<AdminShopPrintAsset>(
    `/admin/shop-orders/${orderId}/print-assets/${qrType}/approve`
  )
  return response.data
}

export async function retryAdminShopOrderPrintAsset(
  orderId: string,
  qrType: AdminShopPrintAsset["qrType"]
): Promise<AdminShopPrintAsset> {
  const response = await axiosInstance.post<AdminShopPrintAsset>(
    `/admin/shop-orders/${orderId}/print-assets/${qrType}/retry`
  )
  return response.data
}

export async function downloadAdminShopOrdersCsv(
  params: AdminShopOrdersListParams = {}
): Promise<{ blob: Blob; fileName: string }> {
  const response = await axiosInstance.get<Blob>(
    "/admin/shop-orders/export.csv",
    {
      params,
      paramsSerializer: arrayParamsSerializer,
      responseType: "blob",
    }
  )

  const disposition = response.headers["content-disposition"] as
    | string
    | undefined
  const match = disposition?.match(/filename="?([^"]+)"?/i)
  const fileName = match?.[1] ?? "tummly-shop-orders.csv"

  return { blob: response.data, fileName }
}
