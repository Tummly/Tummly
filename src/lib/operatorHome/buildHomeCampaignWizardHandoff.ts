import type { CampaignWizardOpenFromRecommendationInput } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { isHomeRecommendationCampaignType } from "@/lib/operatorHome/homeRecommendationPresentation"
import type { HomeRecommendation } from "@/types/operatorHome"

/**
 * Build Campaign wizard `openFromRecommendation` input from a Home campaign-type
 * recommendation (ticket 06). Returns null when type is not campaign or draft is missing.
 */
export function buildHomeCampaignWizardHandoff(input: {
  locationId: number
  locationName: string
  locationAddress?: string | null
  recommendation: HomeRecommendation
}): CampaignWizardOpenFromRecommendationInput | null {
  if (!isHomeRecommendationCampaignType(input.recommendation.type)) {
    return null
  }

  const draftPrefill = input.recommendation.draftPrefill
  if (draftPrefill == null) {
    return null
  }

  return {
    locationId: input.locationId,
    locationName: input.locationName,
    locationAddress: input.locationAddress,
    draftPrefill,
  }
}
