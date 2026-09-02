import axiosInstance from "@/api/axiosInstance"

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

export async function fetchShopDeliveryDefaults(
  locationId: number
): Promise<ShopDeliveryDefaultsWire> {
  const response = await axiosInstance.get<ShopDeliveryDefaultsWire>(
    `/api/shop/locations/${locationId}/delivery-defaults`
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
