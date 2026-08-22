import { isHomeRecommendationCampaignType } from "@/lib/operatorHome/homeRecommendationPresentation"
import type { CampaignRecommendationDraftPrefill } from "@/types/operatorCampaigns"
import type { HomeRecommendation } from "@/types/operatorHome"

/**
 * Draft prefill for Campaign wizard `openFromRecommendation` from a Home
 * campaign-type recommendation (ticket 06). Null when type is not campaign
 * or draft is missing. Campaigns page supplies location when opening the wizard.
 */
export function homeCampaignRecommendationDraftPrefill(
  recommendation: HomeRecommendation
): CampaignRecommendationDraftPrefill | null {
  if (!isHomeRecommendationCampaignType(recommendation.type)) {
    return null
  }

  return recommendation.draftPrefill ?? null
}
