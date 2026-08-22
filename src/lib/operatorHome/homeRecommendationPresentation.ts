import {
  HOME_RECOMMENDATION_CAMPAIGN_TYPES,
  type HomeRecommendationCampaignType,
} from "@/lib/operatorHome/homeRecommendationContract"
import { RECOMMENDED_EMPTY_COPY } from "@/lib/operatorHome/operatorHomeSectionPresentation"
import type { HomeRecommendation } from "@/types/operatorHome"

/** Home Recommended next step copy (Figma 3353:42550 + Campaigns live CTAs). */
export const HOME_RECOMMENDATION_COPY = {
  title: "Recommended next step",
  subtitle: "AI-assisted guidance based on your recent guest activity.",
  emptyCopy: RECOMMENDED_EMPTY_COPY,
  failCopy: "Could not load a recommendation. Please try again.",
  retry: "Retry",
  notNow: "Not now",
  viewFeedback: "View feedback",
  viewGuests: "View guests",
  viewGuestProfile: "View guest profile",
  viewOffers: "View offers",
  editOffer: "Edit offer",
  createOffer: "Create offer",
  reviewCampaignDraft: "Review campaign draft",
  viewEligibleAudience: "View eligible audience",
  opportunityLabel: "Opportunity",
  eligibleAudienceLabel: "Eligible audience",
  whyLabel: "Why this is recommended",
  whyLabelHome: "Why this recommendation?",
  whyIntro: "These guests:",
  suggestedChannelLabel: "Suggested channel",
  estimatedUsageLabel: "Estimated usage",
  audienceDisclaimer:
    "These counts are live Guest Loop signals for this location — not full Campaign eligibility.",
  audienceClose: "Close",
} as const

export function isHomeRecommendationCampaignType(
  type: string
): type is HomeRecommendationCampaignType {
  return (HOME_RECOMMENDATION_CAMPAIGN_TYPES as readonly string[]).includes(type)
}

/** Primary button label for a ready Home recommendation payload. */
export function primaryCtaLabelForHomeRecommendation(
  recommendation: HomeRecommendation
): string {
  if (isHomeRecommendationCampaignType(recommendation.type)) {
    return HOME_RECOMMENDATION_COPY.reviewCampaignDraft
  }

  const action = recommendation.action
  if (action == null) {
    switch (recommendation.type) {
      case "review-open-feedback":
        return HOME_RECOMMENDATION_COPY.viewFeedback
      case "thank-or-follow-guest":
        return HOME_RECOMMENDATION_COPY.viewGuests
      case "promote-or-fix-offer":
        return HOME_RECOMMENDATION_COPY.viewOffers
      default:
        return HOME_RECOMMENDATION_COPY.viewFeedback
    }
  }

  switch (action.kind) {
    case "open-feedback":
      return HOME_RECOMMENDATION_COPY.viewFeedback
    case "open-guest":
      return action.locationGuestId != null
        ? HOME_RECOMMENDATION_COPY.viewGuestProfile
        : HOME_RECOMMENDATION_COPY.viewGuests
    case "open-offer":
      if (action.offerId != null) {
        return HOME_RECOMMENDATION_COPY.editOffer
      }
      return recommendation.type === "promote-or-fix-offer"
        ? HOME_RECOMMENDATION_COPY.createOffer
        : HOME_RECOMMENDATION_COPY.viewOffers
  }
}
