import {
  getDateValue,
  getMultiSelectIds,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import {
  parseLocalDateKey,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type { OperatorFeedbackInboxSortId } from "@/types/operatorFeedback"
import type { OperatorFeedbackInboxTabId } from "@/types/operatorFeedback"

export const FEEDBACK_INBOX_PAGE_SIZE = 25

export type FeedbackInboxListQueryParams = {
  locationId: number
  from: string
  to: string
  tab: OperatorFeedbackInboxTabId
  q: string
  sort: OperatorFeedbackInboxSortId
  page: number
  pageSize: number
  sentiment?: string[]
  detectedTags?: string[]
  qrSource?: string[]
  contact?: string[]
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  utcOffsetMinutes?: number
}

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
  params: FeedbackInboxListQueryParams,
  filters: OperatorFilterSelection
): FeedbackInboxListQueryParams {
  const sentiment = getMultiSelectIds(filters, "sentiment")
  const detectedTags = getMultiSelectIds(filters, "detectedTag")
  const qrSource = getMultiSelectIds(filters, "qrSource")
  const contact = getMultiSelectIds(filters, "contact")
  const date = getDateValue(filters, "date")

  const next: FeedbackInboxListQueryParams = { ...params }

  if (sentiment.length > 0) {
    next.sentiment = sentiment
  }
  if (detectedTags.length > 0) {
    next.detectedTags = detectedTags
  }
  if (qrSource.length > 0) {
    next.qrSource = qrSource
  }
  if (contact.length > 0) {
    next.contact = contact
  }

  if (date.kind === "preset") {
    next.datePreset = date.preset
    next.utcOffsetMinutes = operatorUtcOffsetMinutes()
  } else if (date.kind === "custom") {
    const bounds = customRangeToUtcBounds(date.dateFrom, date.dateTo)
    next.dateFrom = bounds.from
    next.dateTo = bounds.to
  }

  return next
}

export function buildFeedbackInboxListQueryParams(input: {
  locationId: number
  headerDateRange: HomePerformanceDateRange
  tab: OperatorFeedbackInboxTabId
  q: string
  sort: OperatorFeedbackInboxSortId
  page: number
  filters: OperatorFilterSelection
  now?: Date
}): FeedbackInboxListQueryParams {
  const now = input.now ?? new Date()
  const window = resolveHomePerformanceWindow(input.headerDateRange, now)
  const base: FeedbackInboxListQueryParams = {
    locationId: input.locationId,
    from: window.from.toISOString(),
    to: window.to.toISOString(),
    tab: input.tab,
    q: input.q.trim(),
    sort: input.sort,
    page: input.page,
    pageSize: FEEDBACK_INBOX_PAGE_SIZE,
  }
  return appendFilterParams(base, input.filters)
}
