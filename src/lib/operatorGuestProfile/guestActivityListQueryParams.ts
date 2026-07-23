import {
  customRangeToUtcBounds,
  operatorUtcOffsetMinutes,
} from "@/lib/operatorGuestProfile/guestProfileListDateQueryFields"
import type { GuestActivityFilterSelection } from "@/lib/operatorGuestProfile/guestActivityFilterSelection"
import type { OperatorGuestActivitySortId } from "@/types/operatorGuestProfile"

export const GUEST_ACTIVITY_PAGE_SIZE = 25

export type GuestActivityListQueryParams = {
  guestId: number
  locationId: number
  sort: OperatorGuestActivitySortId
  page: number
  pageSize: number
  type?: string[]
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  utcOffsetMinutes?: number
}

export function buildGuestActivityListQueryParams(input: {
  guestId: number
  locationId: number
  sort: OperatorGuestActivitySortId
  page: number
  pageSize?: number
  filters: GuestActivityFilterSelection
  now?: Date
}): GuestActivityListQueryParams {
  const now = input.now ?? new Date()
  const params: GuestActivityListQueryParams = {
    guestId: input.guestId,
    locationId: input.locationId,
    sort: input.sort,
    page: input.page,
    pageSize: input.pageSize ?? GUEST_ACTIVITY_PAGE_SIZE,
  }

  if (input.filters.activityTypes.length > 0) {
    params.type = [...input.filters.activityTypes]
  }

  const { date } = input.filters
  if (date.kind === "preset") {
    params.datePreset = date.preset
    params.utcOffsetMinutes = operatorUtcOffsetMinutes(now)
  } else if (date.kind === "custom") {
    const bounds = customRangeToUtcBounds(date.dateFrom, date.dateTo)
    params.dateFrom = bounds.from
    params.dateTo = bounds.to
  }

  return params
}
