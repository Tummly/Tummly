import axiosInstance from "@/api/axiosInstance"
import type { ShopOrdersListQueryParams } from "@/lib/operatorShop/shopOrdersListQueryParams"

export type ShopShipToPayload = {
  contactName: string
  contactPhone?: string | null
  addressLine1: string
  addressLine2?: string | null
  postcode: string
  country: string
  deliveryInstructions?: string | null
}

export type ShopOrderLineWire = {
  skuId: string
  title: string
  materialType: string
  quantity: number
  unitNetPence: number
  lineNetPence: number
}

export type ShopOrderWire = {
  id: string
  orderNumber: string
  locationId: number
  locationName: string
  paymentStatus: string
  fulfilmentStatus: string | null
  deliveryMethod: string
  materialsNetPence: number
  vatPence: number
  deliveryNetPence: number
  grossPence: number
  currency: string
  isComplimentary?: boolean
  lines: ShopOrderLineWire[]
  shipTo: ShopShipToPayload
}

export type ShopOrderListItemWire = {
  id: string
  orderNumber: string
  orderDate: string
  locationId: number
  locationName: string
  materialsSummary: string
  materialTypes: string[]
  placedBy: string
  totalFormatted: string
  totalGrossPence: number
  paymentStatus: string
  fulfilmentStatus: string
  isComplimentary?: boolean
  updatedAtUtc: string
}

export type ShopOrdersListWire = {
  items: ShopOrderListItemWire[]
  totalCount: number
  page: number
  pageSize: number
  aggregates: {
    inProgress: number
    dispatched: number
    deliveredLast90Days: number
  }
}

export type ShopOrderProgressWire = {
  orderReceivedAtUtc: string | null
  processingStartedAtUtc: string | null
  dispatchedAtUtc: string | null
  deliveredAtUtc: string | null
  trackingUrl: string | null
}

export type ShopOrderPaymentSummaryWire = {
  paidAtUtc: string | null
  revolutOrderId: string | null
  invoiceDocumentNumber?: string | null
}

export type ShopOrderDetailWire = ShopOrderWire & {
  orderDate: string
  placedBy: string
  paymentStatusLabel: string
  fulfilmentStatusLabel: string
  paymentSummary: ShopOrderPaymentSummaryWire
  progress: ShopOrderProgressWire
  updatedAtUtc: string
  canCancel: boolean
  cancelBlockReason?: string | null
  isComplimentary?: boolean
}

export type ShopReorderPrefillWire = {
  locationId: number
  lines: Array<{
    skuId: string
    quantity: number
    title: string
    unitNetPence: number
    lineNetPence: number
  }>
  shipTo: ShopShipToPayload
  deliveryMethod: "standard" | "express"
  sourceOrderNumber: string
  /** Lines dropped from the prefill because they are no longer sold in the Shop. */
  unavailableLines?: { skuId: string; title: string }[]
}

export type ShopDeliveryDefaultsWire = {
  locationId: number
  locationName: string
  contactName: string
  contactPhone: string | null
  addressLine1: string
  addressLine2: string | null
  postcode: string
  country: string
}

export type PlaceShopOrderInput = {
  locationId: number
  fromCart?: true
  lines?: Array<{ skuId: string; quantity: number }>
  deliveryMethod: "standard" | "express"
  expectedGrossPence: number
  shipTo: ShopShipToPayload
}

export type CheckoutLine = {
  skuId: string
  title: string
  quantity: number
  unitNetPence: number
  lineNetPence: number
  specification?: string
  /** Catalog pack tiers for express checkout quantity edits. */
  packs?: Array<{ qty: number; netPence: number }>
  /** Per-SKU standard delivery net (pence). */
  deliveryNetPence?: number
}

export async function placeShopOrder(
  input: PlaceShopOrderInput
): Promise<ShopOrderWire> {
  const response = await axiosInstance.post<ShopOrderWire>(
    "/shop/orders",
    input
  )
  return response.data
}

export async function fetchShopOrder(
  orderId: string,
  locationId: number
): Promise<ShopOrderDetailWire> {
  const response = await axiosInstance.get<ShopOrderDetailWire>(
    `/shop/orders/${orderId}`,
    { params: { locationId } }
  )
  return response.data
}

export async function fetchShopOrdersList(
  params: ShopOrdersListQueryParams
): Promise<ShopOrdersListWire> {
  const response = await axiosInstance.get<ShopOrdersListWire>(
    "/shop/orders",
    {
      params,
      paramsSerializer: {
        serialize: (rawParams) => {
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
      },
    }
  )
  return response.data
}

export type ShopOrderPayWire = {
  outcome: string
  redirectUrl: string
}

export async function payShopOrder(input: {
  orderId: string
  locationId: number
  idempotencyKey: string
}): Promise<ShopOrderPayWire> {
  const response = await axiosInstance.post<ShopOrderPayWire>(
    `/shop/orders/${input.orderId}/pay`,
    {},
    {
      params: { locationId: input.locationId },
      headers: { "Idempotency-Key": input.idempotencyKey },
    }
  )
  return response.data
}

export async function pollShopOrderUntilPaid(input: {
  orderId: string
  locationId: number
  attempts?: number
  intervalMs?: number
}): Promise<ShopOrderWire | null> {
  const attempts = input.attempts ?? 12
  const intervalMs = input.intervalMs ?? 1500

  for (let attempt = 0; attempt < attempts; attempt += 1) {
    const order = await fetchShopOrder(input.orderId, input.locationId)
    if (order.paymentStatus === "paid") {
      return order
    }
    if (attempt < attempts - 1) {
      await new Promise((resolve) => {
        setTimeout(resolve, intervalMs)
      })
    }
  }

  return null
}

export async function fetchShopDeliveryDefaults(
  locationId: number
): Promise<ShopDeliveryDefaultsWire> {
  const response = await axiosInstance.get<ShopDeliveryDefaultsWire>(
    `/shop/locations/${locationId}/delivery-defaults`
  )
  return response.data
}

export async function cancelShopOrder(input: {
  orderId: string
  locationId: number
  reason: string
}): Promise<ShopOrderDetailWire> {
  const response = await axiosInstance.post<ShopOrderDetailWire>(
    `/shop/orders/${input.orderId}/cancel`,
    {
      locationId: input.locationId,
      reason: input.reason,
    }
  )
  return response.data
}

export async function reorderShopOrder(input: {
  orderId: string
  locationId: number
}): Promise<ShopReorderPrefillWire> {
  const response = await axiosInstance.post<ShopReorderPrefillWire>(
    `/shop/orders/${input.orderId}/reorder`,
    {},
    { params: { locationId: input.locationId } }
  )
  return response.data
}

const EXPRESS_DELIVERY_SURCHARGE_PENCE = 2000

/** Matches backend TummlyVatMath (exclusive, half-up AwayFromZero). */
export function computeShopCheckoutTotalsPence(input: {
  materialsNetPence: number
  deliveryMethod: "standard" | "express"
  /** Catalog / seller effective rate in basis points; 0 = net-only (launch OFF). */
  vatRateBps?: number
  /** Max of per-SKU standard delivery nets on the order. */
  standardDeliveryNetPence?: number
}): {
  materialsNetPence: number
  vatPence: number
  deliveryNetPence: number
  grossPence: number
} {
  const standardDeliveryNetPence = Math.max(
    0,
    input.standardDeliveryNetPence ?? 0
  )
  const deliveryNetPence =
    input.deliveryMethod === "express"
      ? standardDeliveryNetPence + EXPRESS_DELIVERY_SURCHARGE_PENCE
      : standardDeliveryNetPence
  const vatRateBps = input.vatRateBps ?? 0
  const vatPence = Math.round(input.materialsNetPence * (vatRateBps / 10_000))
  return {
    materialsNetPence: input.materialsNetPence,
    vatPence,
    deliveryNetPence,
    grossPence: input.materialsNetPence + vatPence + deliveryNetPence,
  }
}
