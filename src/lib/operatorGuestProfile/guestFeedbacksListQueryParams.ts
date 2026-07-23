import {
  customRangeToUtcBounds,
  operatorUtcOffsetMinutes,
} from "@/lib/operatorGuestProfile/guestProfileListDateQueryFields"
import {
  getDateValue,
  getMultiSelectIds,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
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
  filters: OperatorFilterSelection
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

  const sentiment = getMultiSelectIds(input.filters, "sentiment")
  if (sentiment.length > 0) {
    params.sentiment = sentiment
  }

  const detectedTags = getMultiSelectIds(input.filters, "detectedTag")
  if (detectedTags.length > 0) {
    params.detectedTags = detectedTags
  }

  const date = getDateValue(input.filters, "date")
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
