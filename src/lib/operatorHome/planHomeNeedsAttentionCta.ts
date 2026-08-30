/** Home Needs attention CTA destinations (ticket 03). */

import type { CreditChannelId } from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import {
  operatorDashboardBillingCreditsManagePlanPath,
  operatorDashboardBillingCreditsPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
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

export type HomeNeedsAttentionNavigatePlan = {
  kind: "navigate"
  path: string
  feedbackInbox?: AssistantFeedbackInboxIntent
}

export type HomeNeedsAttentionDuplicateDraftPlan = {
  kind: "duplicate-as-draft"
  campaignId: number
  campaignsPath: string
}

export type HomeNeedsAttentionCtaPlan =
  | HomeNeedsAttentionNavigatePlan
  | HomeNeedsAttentionDuplicateDraftPlan

function creditChannelOf(item: HomeNeedsAttentionItem): CreditChannelId | null {
  return item.sourceKind === "credit" ? item.channel : null
}

function campaignIdOf(item: HomeNeedsAttentionItem): number | null {
  return item.sourceKind === "campaign" ? item.campaignId : null
}

function offerIdOf(item: HomeNeedsAttentionItem): number | null {
  return item.sourceKind === "offer" ? item.offerId : null
}

function navigatePlan(
  path: string,
  extra?: Pick<HomeNeedsAttentionNavigatePlan, "feedbackInbox">
): HomeNeedsAttentionNavigatePlan {
  return extra == null
    ? { kind: "navigate", path }
    : { kind: "navigate", path, ...extra }
}

export function planHomeNeedsAttentionCta(input: {
  item: HomeNeedsAttentionItem
  ctaKind: HomeNeedsAttentionCtaKind
  mode: OperatorDashboardMode
  locationId: number
}): HomeNeedsAttentionCtaPlan {
  const { mode, locationId } = input
  const campaignsPath = operatorDashboardNavPath(mode, "campaigns", locationId)

  switch (input.ctaKind) {
    case "review-feedback":
      return navigatePlan(
        operatorDashboardNavPath(mode, "feedback", locationId),
        { feedbackInbox: { tab: "needs-attention" } }
      )
    case "view-usage":
      return navigatePlan(
        operatorDashboardBillingCreditsPath(mode, locationId, {
          tab: "credits-usage",
        })
      )
    case "buy-channel-credits": {
      const channel = creditChannelOf(input.item)
      return navigatePlan(
        operatorDashboardBillingCreditsManagePlanPath(mode, locationId, {
          section: "credit-top-ups",
          channel: channel ?? undefined,
        })
      )
    }
    case "change-plan":
      return navigatePlan(
        operatorDashboardBillingCreditsManagePlanPath(mode, locationId)
      )
    case "preview-campaign": {
      const campaignId = campaignIdOf(input.item)
      return navigatePlan(
        campaignId != null
          ? operatorDashboardCampaignDetailsPath(mode, campaignId, locationId)
          : campaignsPath
      )
    }
    case "retry-remaining":
      return navigatePlan(campaignsPath)
    case "duplicate-as-draft": {
      const campaignId = campaignIdOf(input.item)
      if (campaignId == null) {
        return navigatePlan(campaignsPath)
      }
      return {
        kind: "duplicate-as-draft",
        campaignId,
        campaignsPath,
      }
    }
    case "manage-offer": {
      const offerId = offerIdOf(input.item)
      return navigatePlan(
        offerId != null
          ? operatorDashboardOfferDetailsPath(mode, offerId, locationId)
          : operatorDashboardNavPath(mode, "offers", locationId)
      )
    }
    case "view-redemptions": {
      const offerId = offerIdOf(input.item)
      return navigatePlan(
        offerId != null
          ? operatorDashboardOfferDetailsPath(mode, offerId, locationId, {
              tab: "redemptions",
            })
          : operatorDashboardNavPath(mode, "offers", locationId)
      )
    }
    default: {
      const _exhaustive: never = input.ctaKind
      return _exhaustive
    }
  }
}
