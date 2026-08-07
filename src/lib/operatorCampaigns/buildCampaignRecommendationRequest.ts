import type { CampaignsOverviewDateRange } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { resolveCampaignsOverviewWindow } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import type { CampaignRecommendationRequest } from "@/types/operatorCampaigns"

/** Build POST /campaigns/recommendation body from the Campaigns overview window. */
export function buildCampaignRecommendationRequest(input: {
  locationId: number
  overviewDateRange: CampaignsOverviewDateRange
  refresh?: boolean
  now?: Date
}): CampaignRecommendationRequest {
  const now = input.now ?? new Date()
  const range = input.overviewDateRange

  if (range.kind === "all-time") {
    return {
      locationId: input.locationId,
      overviewDatePreset: "all-time",
      from: null,
      to: null,
      refresh: input.refresh === true,
    }
  }

  const window = resolveCampaignsOverviewWindow(range, now)
  const preset =
    range.kind === "preset" ? range.presetId : "custom"

  return {
    locationId: input.locationId,
    overviewDatePreset: preset,
    from: window?.from.toISOString() ?? null,
    to: window?.to.toISOString() ?? null,
    refresh: input.refresh === true,
  }
}
