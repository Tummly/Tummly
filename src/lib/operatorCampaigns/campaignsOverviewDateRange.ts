import {
  labelForGuestsOverviewDateRange,
  resolveGuestsOverviewWindow,
  toHomePerformanceDateRange,
  type GuestsOverviewDateRange,
} from "@/lib/operatorGuests/guestsOverviewDateRange"

/**
 * Campaigns overview date range — Guests overview family (All time + presets/Custom).
 * Visit-scoped via `campaignsOverviewDateRange` (independent of Guests; not URL).
 * Default Last 30 days (unlike Guests All-time default).
 */
export type CampaignsOverviewDateRange = GuestsOverviewDateRange

export const DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE: CampaignsOverviewDateRange = {
  kind: "preset",
  presetId: "last30",
}

export const CAMPAIGNS_OVERVIEW_ALL_TIME_LABEL = "All time"

export function labelForCampaignsOverviewDateRange(
  range: CampaignsOverviewDateRange
): string {
  return labelForGuestsOverviewDateRange(range)
}

export function toCampaignsHomePerformanceDateRange(
  range: CampaignsOverviewDateRange
) {
  return toHomePerformanceDateRange(range)
}

/**
 * Resolve overview window in the operator's local calendar.
 * All time → null (omit overview date params on the wire).
 */
export function resolveCampaignsOverviewWindow(
  range: CampaignsOverviewDateRange,
  now: Date = new Date()
): { from: Date; to: Date } | null {
  return resolveGuestsOverviewWindow(range, now)
}
