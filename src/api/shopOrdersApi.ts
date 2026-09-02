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
}

export async function placeShopOrder(
  input: PlaceShopOrderInput
): Promise<ShopOrderWire> {
  const response = await axiosInstance.post<ShopOrderWire>(
    "/api/shop/orders",
    input
  )
  return response.data
}

export async function fetchShopOrder(
  orderId: string,
  locationId: number
): Promise<ShopOrderDetailWire> {
  const response = await axiosInstance.get<ShopOrderDetailWire>(
    `/api/shop/orders/${orderId}`,
    { params: { locationId } }
  )
  return response.data
}

export async function fetchShopOrdersList(
  params: ShopOrdersListQueryParams
): Promise<ShopOrdersListWire> {
  const response = await axiosInstance.get<ShopOrdersListWire>(
    "/api/shop/orders",
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
    `/api/shop/orders/${input.orderId}/pay`,
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
    `/api/shop/locations/${locationId}/delivery-defaults`
  )
  return response.data
}

export async function cancelShopOrder(input: {
  orderId: string
  locationId: number
  reason: string
}): Promise<ShopOrderDetailWire> {
  const response = await axiosInstance.post<ShopOrderDetailWire>(
    `/api/shop/orders/${input.orderId}/cancel`,
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
    `/api/shop/orders/${input.orderId}/reorder`,
    {},
    { params: { locationId: input.locationId } }
  )
  return response.data
}

/** Matches backend TummlyVatMath (20% UK VAT, half-up AwayFromZero). */
export function computeShopCheckoutTotalsPence(input: {
  materialsNetPence: number
  deliveryMethod: "standard" | "express"
}): {
  materialsNetPence: number
  vatPence: number
  deliveryNetPence: number
  grossPence: number
} {
  const deliveryNetPence = input.deliveryMethod === "express" ? 2000 : 0
  const vatPence = Math.round(input.materialsNetPence * 0.2)
  return {
    materialsNetPence: input.materialsNetPence,
    vatPence,
    deliveryNetPence,
    grossPence: input.materialsNetPence + vatPence + deliveryNetPence,
  }
}
