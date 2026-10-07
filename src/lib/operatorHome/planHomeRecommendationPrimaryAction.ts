/** Home Recommended next step primary destinations (ticket 02 resolve Feedback). */

import type { AssistantFeedbackInboxIntent } from "@/lib/operatorAiAssistant/assistantActionNavigate"
import { homeCampaignRecommendationDraftPrefill } from "@/lib/operatorHome/homeCampaignRecommendationDraftPrefill"
import { isHomeRecommendationCampaignType } from "@/lib/operatorHome/homeRecommendationPresentation"
import {
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  operatorDashboardOfferDetailsPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import { locationDetailRecoveryFeedbackPath } from "@/lib/operatorLocations/locationDetailApi"
import type { CampaignRecommendationDraftPrefill } from "@/types/operatorCampaigns"
import type { HomeRecommendation } from "@/types/operatorHome"

export type HomeRecommendationPrimaryNavigatePlan = {
  kind: "navigate"
  path: string
  feedbackInbox?: AssistantFeedbackInboxIntent
}

export type HomeRecommendationPrimaryCampaignPlan = {
  kind: "open-campaign-draft"
  path: string
  draftPrefill: CampaignRecommendationDraftPrefill
}

export type HomeRecommendationPrimaryPlan =
  | HomeRecommendationPrimaryNavigatePlan
  | HomeRecommendationPrimaryCampaignPlan
  | { kind: "noop" }

function feedbackPath(
  mode: OperatorDashboardMode,
  locationId: number
): string {
  return operatorDashboardNavPath(mode, "feedback", locationId)
}

function planReviewOpenFeedback(input: {
  recommendation: HomeRecommendation
  mode: OperatorDashboardMode
  locationId: number
}): HomeRecommendationPrimaryNavigatePlan {
  const path = feedbackPath(input.mode, input.locationId)
  const action = input.recommendation.action
  if (
    action?.kind === "open-feedback"
    && action.feedbackId != null
    && action.feedbackId > 0
  ) {
    return {
      kind: "navigate",
      path: locationDetailRecoveryFeedbackPath(path, action.feedbackId),
    }
  }

  return {
    kind: "navigate",
    path,
    feedbackInbox: { tab: "needs-attention" },
  }
}

/**
 * Pure primary-click plan for a ready Home recommendation.
 * `review-open-feedback` pushes Start recovery when a single feedbackId
 * exists; otherwise opens Feedback Needs attention.
 */
export function planHomeRecommendationPrimaryAction(input: {
  recommendation: HomeRecommendation
  mode: OperatorDashboardMode
  locationId: number
}): HomeRecommendationPrimaryPlan {
  const { recommendation, mode, locationId } = input

  if (isHomeRecommendationCampaignType(recommendation.type)) {
    const draftPrefill =
      homeCampaignRecommendationDraftPrefill(recommendation)
    if (draftPrefill == null) {
      return {
        kind: "navigate",
        path: operatorDashboardNavPath(mode, "campaigns", locationId),
      }
    }
    return {
      kind: "open-campaign-draft",
      path: operatorDashboardNavPath(mode, "campaigns", locationId),
      draftPrefill,
    }
  }

  if (
    recommendation.type === "review-open-feedback"
    || recommendation.action?.kind === "open-feedback"
  ) {
    return planReviewOpenFeedback(input)
  }

  const action = recommendation.action
  if (action == null) {
    switch (recommendation.type) {
      case "thank-or-follow-guest":
        return {
          kind: "navigate",
          path: operatorDashboardNavPath(mode, "guests", locationId),
        }
      case "promote-or-fix-offer":
        return {
          kind: "navigate",
          path: operatorDashboardNavPath(mode, "offers", locationId),
        }
      default:
        return { kind: "noop" }
    }
  }

  switch (action.kind) {
    case "open-guest":
      if (action.locationGuestId != null) {
        return {
          kind: "navigate",
          path: operatorDashboardGuestProfilePath(
            mode,
            action.locationGuestId,
            locationId
          ),
        }
      }
      return {
        kind: "navigate",
        path: operatorDashboardNavPath(mode, "guests", locationId),
      }
    case "open-offer":
      if (action.offerId != null) {
        return {
          kind: "navigate",
          path: operatorDashboardOfferDetailsPath(
            mode,
            action.offerId,
            locationId
          ),
        }
      }
      return {
        kind: "navigate",
        path: operatorDashboardNavPath(mode, "offers", locationId),
      }
    case "open-feedback":
      return planReviewOpenFeedback(input)
    default: {
      const _exhaustive: never = action
      return _exhaustive
    }
  }
}
