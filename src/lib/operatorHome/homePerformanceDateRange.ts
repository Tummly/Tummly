import { differenceInCalendarDays, format } from "date-fns"

export const HOME_PERFORMANCE_PRESET_IDS = [
  "last7",
  "last30",
  "thisMonth",
] as const

export type HomePerformancePresetId =
  (typeof HOME_PERFORMANCE_PRESET_IDS)[number]

/** Inclusive max Custom span (calendar days). */
export const HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS = 180

export type HomePerformanceDateRange =
  | { kind: "preset"; presetId: HomePerformancePresetId }
  | {
      kind: "custom"
      /** Local calendar date `YYYY-MM-DD`. */
      startDate: string
      /** Local calendar date `YYYY-MM-DD` (inclusive). */
      endDate: string
    }

export const DEFAULT_HOME_PERFORMANCE_DATE_RANGE: HomePerformanceDateRange = {
  kind: "preset",
  presetId: "last7",
}

const PRESET_LABELS: Record<HomePerformancePresetId, string> = {
  last7: "Last 7 days",
  last30: "Last 30 days",
  thisMonth: "This month",
}

/** Label for the default preset — shared by Performance overview and unwired Latest activity chrome. */
export const HOME_PERFORMANCE_DEFAULT_DATE_RANGE_LABEL = PRESET_LABELS.last7

/** Parse a local `YYYY-MM-DD` key into a local midnight Date. */
export function parseLocalDateKey(dateKey: string): Date {
  const [year, month, day] = dateKey.split("-").map(Number)
  return new Date(year, month - 1, day)
}

/** Format a Date as a local `YYYY-MM-DD` key. */
export function toLocalDateKey(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, "0")
  const day = String(date.getDate()).padStart(2, "0")
  return `${year}-${month}-${day}`
}

/**
 * Inclusive calendar-day span between local date keys.
 * Same day → 1; max allowed Custom → 180.
 */
export function inclusiveLocalDateSpanDays(
  startDate: string,
  endDate: string
): number {
  return (
    differenceInCalendarDays(
      parseLocalDateKey(endDate),
      parseLocalDateKey(startDate)
    ) + 1
  )
}

export function isHomePerformanceCustomSpanAllowed(
  startDate: string,
  endDate: string
): boolean {
  const start = parseLocalDateKey(startDate)
  const end = parseLocalDateKey(endDate)
  if (end < start) {
    return false
  }
  return (
    inclusiveLocalDateSpanDays(startDate, endDate)
    <= HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS
  )
}

export function labelForHomePerformanceDateRange(
  range: HomePerformanceDateRange
): string {
  if (range.kind === "preset") {
    return PRESET_LABELS[range.presetId]
  }

  const start = parseLocalDateKey(range.startDate)
  const end = parseLocalDateKey(range.endDate)
  if (range.startDate === range.endDate) {
    return format(start, "d MMM yyyy")
  }
  return `${format(start, "d")}–${format(end, "d MMM yyyy")}`
}

export function homePerformancePresetOptions(): ReadonlyArray<{
  presetId: HomePerformancePresetId
  label: string
}> {
  return HOME_PERFORMANCE_PRESET_IDS.map((presetId) => ({
    presetId,
    label: PRESET_LABELS[presetId],
  }))
}

/**
 * Resolve Home performance bounds in the operator's local timezone.
 * Callers send the returned instants as UTC ISO strings to the API.
 *
 * - Last 7 days: start of local day 6 days before today → now
 * - Last 30 days: start of local day 29 days before today → now
 * - This month: 1st of current local month 00:00 → now
 * - Custom: start of start date → start of day after end date
 */
export function resolveHomePerformanceWindow(
  range: HomePerformanceDateRange,
  now: Date = new Date()
): { from: Date; to: Date } {
  if (range.kind === "custom") {
    const from = parseLocalDateKey(range.startDate)
    const end = parseLocalDateKey(range.endDate)
    const to = new Date(end.getFullYear(), end.getMonth(), end.getDate() + 1)
    return { from, to }
  }

  if (range.presetId === "last7") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate() - 6),
      to: now,
    }
  }

  if (range.presetId === "last30") {
    return {
      from: new Date(now.getFullYear(), now.getMonth(), now.getDate() - 29),
      to: now,
    }
  }

  return {
    from: new Date(now.getFullYear(), now.getMonth(), 1),
    to: now,
  }
}
