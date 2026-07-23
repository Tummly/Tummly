import {
  customRangeToUtcBounds,
  operatorUtcOffsetMinutes,
} from "@/lib/operatorGuestProfile/guestProfileListDateQueryFields"
import type { GuestFeedbacksFilterSelection } from "@/lib/operatorGuestProfile/guestFeedbacksFilterSelection"
import type { OperatorGuestFeedbacksSortId } from "@/types/operatorGuestProfile"

export const GUEST_FEEDBACKS_PAGE_SIZE = 25

export type GuestFeedbacksListQueryParams = {
  guestId: number
  locationId: number
  q: string
  sort: OperatorGuestFeedbacksSortId
  page: number
  pageSize: number
  sentiment?: string[]
  detectedTags?: string[]
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  utcOffsetMinutes?: number
}

export function buildGuestFeedbacksListQueryParams(input: {
  guestId: number
  locationId: number
  q: string
  sort: OperatorGuestFeedbacksSortId
  page: number
  pageSize?: number
  filters: GuestFeedbacksFilterSelection
  now?: Date
}): GuestFeedbacksListQueryParams {
  const now = input.now ?? new Date()
  const params: GuestFeedbacksListQueryParams = {
    guestId: input.guestId,
    locationId: input.locationId,
    q: input.q,
    sort: input.sort,
    page: input.page,
    pageSize: input.pageSize ?? GUEST_FEEDBACKS_PAGE_SIZE,
  }

  if (input.filters.sentiment.length > 0) {
    params.sentiment = [...input.filters.sentiment]
  }

  if (input.filters.detectedTags.length > 0) {
    params.detectedTags = [...input.filters.detectedTags]
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
