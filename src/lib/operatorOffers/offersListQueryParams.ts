import {
  getMultiSelectIds,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import type {
  CatalogOfferStatus,
  CatalogOffersListQueryParams,
  OperatorOffersListViewId,
  OperatorOffersSortId,
} from "@/types/operatorCampaigns"
import type { OffersFilterAttachSourceId } from "@/lib/operatorOffers/offersFilterSheetSchema"

function operatorUtcOffsetMinutes(now: Date = new Date()): number {
  return -now.getTimezoneOffset()
}

function appendFilterParams(
  params: CatalogOffersListQueryParams,
  filters: OperatorFilterSelection
): CatalogOffersListQueryParams {
  const next = { ...params }

  const status = getMultiSelectIds(filters, "status") as CatalogOfferStatus[]
  if (status.length > 0) {
    next.status = status
  }

  const attachSource = getMultiSelectIds(
    filters,
    "attachSource"
  ) as OffersFilterAttachSourceId[]
  if (attachSource.length > 0) {
    next.attachSource = attachSource
  }

  return next
}

export function buildOffersListQueryParams(input: {
  locationId: number
  view: OperatorOffersListViewId
  q: string
  sort: OperatorOffersSortId
  page: number
  pageSize: number
  filters: OperatorFilterSelection
  now?: Date
}): CatalogOffersListQueryParams {
  const now = input.now ?? new Date()
  const base: CatalogOffersListQueryParams = {
    locationId: input.locationId,
    view: input.view,
    q: input.q.trim() || undefined,
    sort: input.sort,
    page: input.page,
    pageSize: input.pageSize,
    utcOffsetMinutes: operatorUtcOffsetMinutes(now),
  }
  return appendFilterParams(base, input.filters)
}
