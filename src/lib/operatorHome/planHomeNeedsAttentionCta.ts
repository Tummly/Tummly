/** Home Needs attention CTA destinations (ticket 03). */

import type { AssistantFeedbackInboxIntent } from "@/lib/operatorAiAssistant/assistantActionNavigate"
import type {
  HomeNeedsAttentionCtaKind,
  HomeNeedsAttentionItem,
} from "@/lib/operatorHome/buildHomeNeedsAttention"
import {
  operatorDashboardCampaignDetailsPath,
  operatorDashboardNavPath,
  operatorDashboardOfferDetailsPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"

export type HomeNeedsAttentionCtaPlan = {
  path: string
  feedbackInbox?: AssistantFeedbackInboxIntent
}

function campaignIdOf(item: HomeNeedsAttentionItem): number | null {
  return item.sourceKind === "campaign" ? item.campaignId : null
}

function offerIdOf(item: HomeNeedsAttentionItem): number | null {
  return item.sourceKind === "offer" ? item.offerId : null
}

export function planHomeNeedsAttentionCta(input: {
  item: HomeNeedsAttentionItem
  ctaKind: HomeNeedsAttentionCtaKind
  mode: OperatorDashboardMode
  locationId: number
}): HomeNeedsAttentionCtaPlan {
  const { mode, locationId } = input

  switch (input.ctaKind) {
    case "review-feedback":
      return {
        path: operatorDashboardNavPath(mode, "feedback", locationId),
        feedbackInbox: { tab: "needs-attention" },
      }
    case "preview-campaign": {
      const campaignId = campaignIdOf(input.item)
      return {
        path:
          campaignId != null
            ? operatorDashboardCampaignDetailsPath(mode, campaignId, locationId)
            : operatorDashboardNavPath(mode, "campaigns", locationId),
      }
    }
    case "retry-remaining":
    case "duplicate-as-draft":
      return {
        path: operatorDashboardNavPath(mode, "campaigns", locationId),
      }
    case "manage-offer": {
      const offerId = offerIdOf(input.item)
      return {
        path:
          offerId != null
            ? operatorDashboardOfferDetailsPath(mode, offerId, locationId)
            : operatorDashboardNavPath(mode, "offers", locationId),
      }
    }
    case "view-redemptions": {
      const offerId = offerIdOf(input.item)
      return {
        path:
          offerId != null
            ? operatorDashboardOfferDetailsPath(mode, offerId, locationId, {
                tab: "redemptions",
              })
            : operatorDashboardNavPath(mode, "offers", locationId),
      }
    }
    default: {
      const _exhaustive: never = input.ctaKind
      return _exhaustive
    }
  }
}
