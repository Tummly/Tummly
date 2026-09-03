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

export type AdminShopOrderListItem = {
  id: string
  orderNumber: string
  restaurantId: number
  locationId: number
  locationNameSnapshot: string
  fulfilmentStatus: AdminShopFulfilmentStatus | string
  paymentStatus: string
  revolutOrderId: string | null
  trackingUrl: string | null
  opsNotes: string | null
  paidAtUtc: string | null
  grossPence: number
  lines: AdminShopOrderLineSummary[]
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
}

export type AdminShopOrderFulfilmentPatch = {
  fulfilmentStatus?: AdminShopFulfilmentStatus
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
