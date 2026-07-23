import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
  type HomePerformancePresetId,
} from "@/lib/operatorHome/homePerformanceDateRange"

/**
 * Guest overview date range — All time default + Home Performance presets/Custom.
 * Visit-scoped via `guestsOverviewDateRange` (separate from Home).
 */
export type GuestsOverviewDateRange =
  | { kind: "all-time" }
  | { kind: "preset"; presetId: HomePerformancePresetId }
  | {
      kind: "custom"
      startDate: string
      endDate: string
    }

export const DEFAULT_GUESTS_OVERVIEW_DATE_RANGE: GuestsOverviewDateRange = {
  kind: "all-time",
}

export const GUESTS_OVERVIEW_ALL_TIME_LABEL = "All time"

export function labelForGuestsOverviewDateRange(
  range: GuestsOverviewDateRange
): string {
  if (range.kind === "all-time") {
    return GUESTS_OVERVIEW_ALL_TIME_LABEL
  }
  return labelForHomePerformanceDateRange(range)
}

/** Map Guests overview range onto Home Performance shape (All time → null). */
export function toHomePerformanceDateRange(
  range: GuestsOverviewDateRange
): HomePerformanceDateRange | null {
  if (range.kind === "all-time") {
    return null
  }
  return range
}

/**
 * Resolve overview window in the operator's local calendar.
 * All time → null (omit overview date params on the wire).
 */
export function resolveGuestsOverviewWindow(
  range: GuestsOverviewDateRange,
  now: Date = new Date()
): { from: Date; to: Date } | null {
  const homeRange = toHomePerformanceDateRange(range)
  if (homeRange == null) {
    return null
  }
  return resolveHomePerformanceWindow(homeRange, now)
}
