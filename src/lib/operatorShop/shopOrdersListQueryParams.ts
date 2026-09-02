import {
  getDateValue,
  getLocationOverride,
  getMultiSelectIds,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import {
  getShopOrdersSortId,
  type ShopOrdersSortId,
} from "@/lib/operatorShop/shopOrdersFilterSheetSchema"

export type ShopOrdersListQueryParams = {
  locationId: number
  q: string
  sort: ShopOrdersSortId
  page: number
  pageSize: number
  locationScope?: "all"
  locationIds?: number[]
  fulfilmentStatus?: string[]
  paymentStatus?: string[]
  materialType?: string[]
  orderDatePreset?: string
  orderDateFrom?: string
  orderDateTo?: string
  utcOffsetMinutes?: number
}

function operatorUtcOffsetMinutes(now: Date = new Date()): number {
  return -now.getTimezoneOffset()
}

function customRangeToUtcBounds(
  dateFrom: string,
  dateTo: string
): { from: string; to: string } {
  const from = new Date(`${dateFrom}T00:00:00`)
  const to = new Date(`${dateTo}T23:59:59.999`)
  return { from: from.toISOString(), to: to.toISOString() }
}

function appendFilterParams(
  params: ShopOrdersListQueryParams,
  filters: OperatorFilterSelection,
  now: Date
): ShopOrdersListQueryParams {
  const next = { ...params }

  const fulfilmentStatus = getMultiSelectIds(filters, "fulfilmentStatus")
  if (fulfilmentStatus.length > 0) {
    next.fulfilmentStatus = fulfilmentStatus
  }

  const paymentStatus = getMultiSelectIds(filters, "paymentStatus")
  if (paymentStatus.length > 0) {
    next.paymentStatus = paymentStatus
  }

  const materialType = getMultiSelectIds(filters, "materialType")
  if (materialType.length > 0) {
    next.materialType = materialType
  }

  const location = getLocationOverride(filters, "location")
  if (location.kind === "all") {
    next.locationScope = "all"
  } else if (location.kind === "individual") {
    next.locationIds = location.locationIds.map((id) =>
      Number.parseInt(id, 10)
    )
  }

  const date = getDateValue(filters, "orderDate")
  if (date.kind === "preset") {
    next.orderDatePreset = date.preset
    next.utcOffsetMinutes = operatorUtcOffsetMinutes(now)
  } else if (date.kind === "custom") {
    const bounds = customRangeToUtcBounds(date.dateFrom, date.dateTo)
    next.orderDateFrom = bounds.from
    next.orderDateTo = bounds.to
  }

  return next
}

export function buildShopOrdersListQueryParams(input: {
  locationId: number
  q: string
  filters: OperatorFilterSelection
  page: number
  pageSize: number
  now?: Date
}): ShopOrdersListQueryParams {
  const sort = getShopOrdersSortId(input.filters)
  const base: ShopOrdersListQueryParams = {
    locationId: input.locationId,
    q: input.q,
    sort,
    page: input.page,
    pageSize: input.pageSize,
    locationScope: "all",
  }

  return appendFilterParams(base, input.filters, input.now ?? new Date())
}
