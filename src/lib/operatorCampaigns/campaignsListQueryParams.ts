import {
  getDateValue,
  getLocationOverride,
  getMultiSelectIds,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { parseLocalDateKey } from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  CampaignsListQueryParams,
  OperatorCampaignsListViewId,
  OperatorCampaignsSortId,
} from "@/types/operatorCampaigns"

function operatorUtcOffsetMinutes(now: Date = new Date()): number {
  return -now.getTimezoneOffset()
}

function customRangeToUtcBounds(
  dateFrom: string,
  dateTo: string
): { from: string; to: string } {
  const from = parseLocalDateKey(dateFrom)
  const end = parseLocalDateKey(dateTo)
  const to = new Date(end.getFullYear(), end.getMonth(), end.getDate() + 1)
  return { from: from.toISOString(), to: to.toISOString() }
}

function appendFilterParams(
  params: CampaignsListQueryParams,
  filters: OperatorFilterSelection,
  now: Date
): CampaignsListQueryParams {
  const next = { ...params }

  const status = getMultiSelectIds(filters, "status")
  if (status.length > 0) {
    next.status = status
  }
  const channel = getMultiSelectIds(filters, "channel")
  if (channel.length > 0) {
    next.channel = channel
  }
  const goalId = getMultiSelectIds(filters, "goal")
  if (goalId.length > 0) {
    next.goalId = goalId
  }
  const offerStance = getMultiSelectIds(filters, "offerStance")
  if (offerStance.length > 0) {
    next.offerStance = offerStance
  }
  const createdBy = getMultiSelectIds(filters, "createdBy")
  if (createdBy.length > 0) {
    next.createdBy = createdBy.map((id) => Number.parseInt(id, 10))
  }
  const deliveryIssue = getMultiSelectIds(filters, "deliveryIssue")
  if (deliveryIssue.length > 0) {
    next.deliveryIssue = deliveryIssue
  }

  const location = getLocationOverride(filters, "location")
  if (location.kind === "all") {
    next.locationScope = "all"
  } else if (location.kind === "individual") {
    next.locationIds = location.locationIds.map((id) =>
      Number.parseInt(id, 10)
    )
  }

  const date = getDateValue(filters, "date")
  if (date.kind === "preset") {
    next.dateAxis = date.axis
    next.datePreset = date.preset
    next.utcOffsetMinutes = operatorUtcOffsetMinutes(now)
  } else if (date.kind === "custom") {
    next.dateAxis = date.axis
    const bounds = customRangeToUtcBounds(date.dateFrom, date.dateTo)
    next.dateFrom = bounds.from
    next.dateTo = bounds.to
  }

  return next
}

export function buildCampaignsListQueryParams(input: {
  locationId: number
  view: OperatorCampaignsListViewId
  q: string
  sort: OperatorCampaignsSortId
  page: number
  pageSize: number
  filters: OperatorFilterSelection
  now?: Date
}): CampaignsListQueryParams {
  const now = input.now ?? new Date()
  const base: CampaignsListQueryParams = {
    locationId: input.locationId,
    view: input.view,
    q: input.q.trim() || undefined,
    sort: input.sort,
    page: input.page,
    pageSize: input.pageSize,
  }
  return appendFilterParams(base, input.filters, now)
}
