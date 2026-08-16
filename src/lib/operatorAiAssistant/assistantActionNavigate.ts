import type { OperatorAiAssistantAction } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { OperatorAiAssistantAnalysisScope } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { RecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"
import {
  operatorDashboardCaptureLocationPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  operatorDashboardOfferDetailsPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type { OperatorFeedbackInboxTabId } from "@/types/operatorFeedback"

export type AssistantFeedbackInboxIntent = {
  tab?: OperatorFeedbackInboxTabId
  sentiment?: string
  detectedTag?: string
}

export type AssistantGuestsIntent = {
  smartGroup?: string
  marketingEligible?: boolean
}

export type AssistantCampaignsIntent = {
  view: "drafts"
}

export type AssistantOffersIntent = {
  view: "drafts"
}

export type AssistantActionNavigatePlan = {
  path: string
  selectLocationId: number
  feedbackDateRange?: HomePerformanceDateRange
  captureDateRange?: HomePerformanceDateRange
  feedbackInbox?: AssistantFeedbackInboxIntent
  guests?: AssistantGuestsIntent
  campaigns?: AssistantCampaignsIntent
  offers?: AssistantOffersIntent
  recoveryDraft?: RecoveryDraftActionPayload
}

function isInboxTab(
  value: string | null | undefined
): value is OperatorFeedbackInboxTabId {
  return (
    value === "all"
    || value === "needs-attention"
    || value === "new"
    || value === "in-progress"
    || value === "resolved"
  )
}

export function planAssistantActionNavigate(input: {
  action: OperatorAiAssistantAction
  analysisScope: OperatorAiAssistantAnalysisScope
  mode: OperatorDashboardMode
  recoveryDraft?: RecoveryDraftActionPayload | null
}): AssistantActionNavigatePlan {
  const locationId = input.analysisScope.ownedLocationId
  const { action, mode } = input

  switch (action.type) {
    case "draft-campaign":
      return {
        path: operatorDashboardNavPath(mode, "campaigns", locationId),
        selectLocationId: locationId,
        campaigns: { view: "drafts" },
      }
    case "draft-offer":
      return {
        path: operatorDashboardNavPath(mode, "offers", locationId),
        selectLocationId: locationId,
        offers: { view: "drafts" },
      }
    case "open-recovery":
      return {
        path: operatorDashboardNavPath(mode, "feedback", locationId),
        selectLocationId: locationId,
        recoveryDraft: input.recoveryDraft ?? undefined,
      }
    case "prepare-recovery":
      return {
        path: operatorDashboardNavPath(mode, "feedback", locationId),
        selectLocationId: locationId,
        feedbackDateRange: input.analysisScope.reportingPeriod,
        feedbackInbox: { tab: "needs-attention" },
      }
    case "view-feedback-set": {
      const inbox: AssistantFeedbackInboxIntent = {}
      if (isInboxTab(action.tab)) {
        inbox.tab = action.tab
      }
      if (action.sentiment) {
        inbox.sentiment = action.sentiment
      }
      if (action.detectedTag) {
        inbox.detectedTag = action.detectedTag
      }
      return {
        path: operatorDashboardNavPath(mode, "feedback", locationId),
        selectLocationId: locationId,
        feedbackDateRange: input.analysisScope.reportingPeriod,
        feedbackInbox: Object.keys(inbox).length > 0 ? inbox : undefined,
      }
    }
    case "view-campaigns":
      return {
        path: operatorDashboardNavPath(mode, "campaigns", locationId),
        selectLocationId: locationId,
      }
    case "view-offers":
      return {
        path: operatorDashboardNavPath(mode, "offers", locationId),
        selectLocationId: locationId,
      }
    case "view-offer":
      return {
        path:
          action.offerId != null
            ? operatorDashboardOfferDetailsPath(mode, action.offerId, locationId)
            : operatorDashboardNavPath(mode, "offers", locationId),
        selectLocationId: locationId,
      }
    case "view-guests": {
      const guests: AssistantGuestsIntent = {}
      if (action.smartGroup) {
        guests.smartGroup = action.smartGroup
      }
      if (action.marketingEligible === true) {
        guests.marketingEligible = true
      }
      return {
        path: operatorDashboardNavPath(mode, "guests", locationId),
        selectLocationId: locationId,
        guests: Object.keys(guests).length > 0 ? guests : undefined,
      }
    }
    case "view-guest":
      return {
        path:
          action.guestId != null
            ? operatorDashboardGuestProfilePath(mode, action.guestId, locationId)
            : operatorDashboardNavPath(mode, "guests", locationId),
        selectLocationId: locationId,
      }
    case "view-capture":
      return {
        path:
          mode === "multi"
            ? operatorDashboardCaptureLocationPath(locationId)
            : operatorDashboardNavPath(mode, "capture", locationId),
        selectLocationId: locationId,
        captureDateRange: input.analysisScope.reportingPeriod,
      }
    default:
      return {
        path: operatorDashboardNavPath(mode, "feedback", locationId),
        selectLocationId: locationId,
      }
  }
}
