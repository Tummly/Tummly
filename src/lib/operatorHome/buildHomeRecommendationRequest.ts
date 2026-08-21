import {
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type { HomeRecommendationRequest } from "@/types/operatorHome"

/** Build POST /home/recommendation body from the Home performance window. */
export function buildHomeRecommendationRequest(input: {
  locationId: number
  performanceDateRange: HomePerformanceDateRange
  refresh?: boolean
  now?: Date
}): HomeRecommendationRequest {
  const now = input.now ?? new Date()
  const range = input.performanceDateRange
  const window = resolveHomePerformanceWindow(range, now)
  const overviewDatePreset =
    range.kind === "preset" ? range.presetId : "custom"

  return {
    locationId: input.locationId,
    overviewDatePreset,
    from: window.from.toISOString(),
    to: window.to.toISOString(),
    refresh: input.refresh === true,
  }
}
