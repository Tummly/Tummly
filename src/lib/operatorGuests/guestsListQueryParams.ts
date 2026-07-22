import {
  resolveGuestsOverviewWindow,
  type GuestsOverviewDateRange,
} from "@/lib/operatorGuests/guestsOverviewDateRange"
import {
  parseLocalDateKey,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  DateFilter,
  GuestsFilterSelection,
} from "@/lib/operatorGuests/guestsFilterSelection"
import type {
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
} from "@/types/operatorGuests"

export type GuestsListQueryParams = {
  locationId: number
  smartGroup: OperatorGuestSmartGroupId
  q: string
  sort: OperatorGuestSortId
  page: number
  pageSize: number
  marketing?: string[]
  contact?: string[]
  sentiment?: string[]
  tagIds?: number[]
  dateAxis?: string
  datePreset?: string
  dateFrom?: string
  dateTo?: string
  locationScope?: "all"
  locationIds?: number[]
  overviewDatePreset?: string
  overviewDateFrom?: string
  overviewDateTo?: string
  /** Minutes east of UTC; required when sending datePreset / overviewDatePreset. */
  utcOffsetMinutes?: number
}

export type GuestsExportQueryParams = Omit<
  GuestsListQueryParams,
  | "page"
  | "pageSize"
  | "overviewDatePreset"
  | "overviewDateFrom"
  | "overviewDateTo"
> & {
  guestIds?: number[]
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

/**
 * Resolve Filters table date presets in the operator's local calendar.
 * Prefer sending wire `datePreset` + `utcOffsetMinutes`; this helper remains
 * for tests / callers that need explicit bounds.
 */
export function resolveGuestsTableDateWindow(
  date: Extract<DateFilter, { kind: "preset" }>,
  now: Date = new Date()
): { from: Date; to: Date } {
  if (date.preset === "today") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate()),
      to: new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1),
    }
  }

  if (date.preset === "last-7") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate() - 6),
      to: now,
    }
  }

  if (date.preset === "last-30") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate() - 29),
      to: now,
    }
  }

  if (date.preset === "this-month") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), 1),
      to: now,
    }
  }

  const firstOfThisMonth = new Date(now.getFullYear(), now.getMonth(), 1)
  return {
    from: new Date(now.getFullYear(), now.getMonth() - 1, 1),
    to: firstOfThisMonth,
  }
}

function appendFilterParams(
  params: GuestsListQueryParams,
  filters: GuestsFilterSelection,
  now: Date
): GuestsListQueryParams {
  const next = { ...params }

  if (filters.marketing.length > 0) {
    next.marketing = [...filters.marketing]
  }
  if (filters.contact.length > 0) {
    next.contact = [...filters.contact]
  }
  if (filters.sentiment.length > 0) {
    next.sentiment = [...filters.sentiment]
  }
  if (filters.tagIds.length > 0) {
    next.tagIds = filters.tagIds.map((id) => Number.parseInt(id, 10))
  }

  if (filters.location.kind === "all") {
    next.locationScope = "all"
  } else if (filters.location.kind === "individual") {
    next.locationIds = filters.location.locationIds.map((id) =>
      Number.parseInt(id, 10)
    )
  }

  if (filters.date.kind === "preset") {
    next.dateAxis = filters.date.axis
    next.datePreset = filters.date.preset
    next.utcOffsetMinutes = operatorUtcOffsetMinutes(now)
  } else if (filters.date.kind === "custom") {
    next.dateAxis = filters.date.axis
    const bounds = customRangeToUtcBounds(
      filters.date.dateFrom,
      filters.date.dateTo
    )
    next.dateFrom = bounds.from
    next.dateTo = bounds.to
  }

  return next
}

function appendOverviewParams(
  params: GuestsListQueryParams,
  overviewDateRange: GuestsOverviewDateRange,
  now: Date
): GuestsListQueryParams {
  // US 75: resolve overview in the operator's local calendar; send UTC bounds.
  const window = resolveGuestsOverviewWindow(overviewDateRange, now)
  if (window == null) {
    return params
  }

  return {
    ...params,
    overviewDateFrom: window.from.toISOString(),
    overviewDateTo: window.to.toISOString(),
  }
}

export function buildGuestsListQueryParams(input: {
  locationId: number
  smartGroup: OperatorGuestSmartGroupId
  q: string
  sort: OperatorGuestSortId
  page: number
  pageSize: number
  filters: GuestsFilterSelection
  overviewDateRange: GuestsOverviewDateRange
  now?: Date
}): GuestsListQueryParams {
  const now = input.now ?? new Date()
  const base: GuestsListQueryParams = {
    locationId: input.locationId,
    smartGroup: input.smartGroup,
    q: input.q,
    sort: input.sort,
    page: input.page,
    pageSize: input.pageSize,
  }
  return appendOverviewParams(
    appendFilterParams(base, input.filters, now),
    input.overviewDateRange,
    now
  )
}

export function buildGuestsExportQueryParams(input: {
  locationId: number
  smartGroup: OperatorGuestSmartGroupId
  q: string
  sort: OperatorGuestSortId
  filters: GuestsFilterSelection
  guestIds?: readonly string[]
  now?: Date
}): GuestsExportQueryParams {
  const listParams = appendFilterParams(
    {
      locationId: input.locationId,
      smartGroup: input.smartGroup,
      q: input.q,
      sort: input.sort,
      page: 1,
      pageSize: 25,
    },
    input.filters,
    input.now ?? new Date()
  )

  const {
    page: _page,
    pageSize: _pageSize,
    overviewDatePreset: _overviewDatePreset,
    overviewDateFrom: _overviewDateFrom,
    overviewDateTo: _overviewDateTo,
    ...exportParams
  } = listParams

  if (input.guestIds != null) {
    return {
      ...exportParams,
      guestIds: input.guestIds.map((id) => Number.parseInt(id, 10)),
    }
  }

  return exportParams
}

/** Custom overview commit uses the same Home Performance custom shape. */
export type GuestsOverviewCustomRange = Extract<
  HomePerformanceDateRange,
  { kind: "custom" }
>
