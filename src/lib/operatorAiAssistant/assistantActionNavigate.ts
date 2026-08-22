import type { OperatorAiAssistantAction } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { OperatorAiAssistantAnalysisScope } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { AssistantSendScheduleRoute } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { RecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"
import {
  operatorDashboardCaptureLocationPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  operatorDashboardOfferDetailsPath,
  operatorDashboardRootPath,
  type NavigableOperatorSidebarPrimaryNavId,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type { CampaignWizardContinueEditingStep } from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type {
  CampaignDraftDetail,
  CampaignRecommendationDraftPrefill,
  CatalogOfferDetail,
} from "@/types/operatorCampaigns"
import type { CampaignScheduleModeId } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
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

export type AssistantCampaignContinueEditingStep =
  CampaignWizardContinueEditingStep

export type AssistantCampaignsIntent =
  | {
      previewCampaignId: number
      campaign?: CampaignDraftDetail
    }
  | {
      continueEditingCampaignId: number
      continueEditingStep: AssistantCampaignContinueEditingStep
      scheduleMode?: CampaignScheduleModeId
      dateLocal?: string
      timeLocal?: string
      campaign?: CampaignDraftDetail
    }
  | {
      /** Home Recommended next step — Review campaign draft (ticket 06). */
      openFromRecommendation: {
        draftPrefill: CampaignRecommendationDraftPrefill
      }
    }

export type AssistantOffersIntent = {
  view: "drafts"
}

export type AssistantActionNavigatePlan = {
  path: string
  selectLocationId: number | null
  feedbackDateRange?: HomePerformanceDateRange
  captureDateRange?: HomePerformanceDateRange
  feedbackInbox?: AssistantFeedbackInboxIntent
  guests?: AssistantGuestsIntent
  campaigns?: AssistantCampaignsIntent
  offers?: AssistantOffersIntent
  recoveryDraft?: RecoveryDraftActionPayload
}

function sectionNavPath(
  mode: OperatorDashboardMode,
  navId: NavigableOperatorSidebarPrimaryNavId,
  locationId: number | null
): string {
  if (locationId == null) {
    const root = operatorDashboardRootPath(mode)
    return navId === "home" ? root : `${root}/${navId}`
  }
  return operatorDashboardNavPath(mode, navId, locationId)
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
  campaignDraft?: CampaignDraftDetail | null
  catalogOffer?: CatalogOfferDetail | null
}): AssistantActionNavigatePlan {
  const locationId =
    input.campaignDraft?.locationId
    ?? input.catalogOffer?.locationId
    ?? input.recoveryDraft?.locationId
    ?? input.analysisScope.ownedLocationId
  const { action, mode } = input

  switch (action.type) {
    case "review-campaign":
      return {
        path: sectionNavPath(mode, "campaigns", locationId),
        selectLocationId: locationId,
        campaigns:
          action.campaignId != null
            ? {
                previewCampaignId: action.campaignId,
                ...(input.campaignDraft != null
                  ? { campaign: input.campaignDraft }
                  : {}),
              }
            : undefined,
      }
    case "change-audience":
    case "add-offer":
      return {
        path: sectionNavPath(mode, "campaigns", locationId),
        selectLocationId: locationId,
        campaigns:
          action.campaignId != null
            ? {
                continueEditingCampaignId: action.campaignId,
                continueEditingStep:
                  action.type === "change-audience" ? "audience" : "offer",
                ...(input.campaignDraft != null
                  ? { campaign: input.campaignDraft }
                  : {}),
              }
            : undefined,
      }
    case "review-offer":
      return {
        path:
          action.offerId != null && locationId != null
            ? operatorDashboardOfferDetailsPath(mode, action.offerId, locationId)
            : sectionNavPath(mode, "offers", locationId),
        selectLocationId: locationId,
      }
    case "open-recovery":
      return {
        path: sectionNavPath(mode, "feedback", locationId),
        selectLocationId: locationId,
        recoveryDraft: input.recoveryDraft ?? undefined,
      }
    case "prepare-recovery":
      return {
        path: sectionNavPath(mode, "feedback", locationId),
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
        path: sectionNavPath(mode, "feedback", locationId),
        selectLocationId: locationId,
        feedbackDateRange: input.analysisScope.reportingPeriod,
        feedbackInbox: Object.keys(inbox).length > 0 ? inbox : undefined,
      }
    }
    case "view-campaigns":
      return {
        path: sectionNavPath(mode, "campaigns", locationId),
        selectLocationId: locationId,
      }
    case "view-offers":
      return {
        path: sectionNavPath(mode, "offers", locationId),
        selectLocationId: locationId,
      }
    case "view-offer":
      return {
        path:
          action.offerId != null && locationId != null
            ? operatorDashboardOfferDetailsPath(mode, action.offerId, locationId)
            : sectionNavPath(mode, "offers", locationId),
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
        path: sectionNavPath(mode, "guests", locationId),
        selectLocationId: locationId,
        guests: Object.keys(guests).length > 0 ? guests : undefined,
      }
    }
    case "view-guest":
      return {
        path:
          action.guestId != null && locationId != null
            ? operatorDashboardGuestProfilePath(mode, action.guestId, locationId)
            : sectionNavPath(mode, "guests", locationId),
        selectLocationId: locationId,
      }
    case "view-capture":
      return {
        path:
          mode === "multi" && locationId != null
            ? operatorDashboardCaptureLocationPath(locationId)
            : sectionNavPath(mode, "capture", locationId),
        selectLocationId: locationId,
        captureDateRange: input.analysisScope.reportingPeriod,
      }
    default:
      return {
        path: sectionNavPath(mode, "feedback", locationId),
        selectLocationId: locationId,
      }
  }
}

export function planAssistantSendScheduleRoute(input: {
  route: AssistantSendScheduleRoute
  analysisScope: OperatorAiAssistantAnalysisScope
  mode: OperatorDashboardMode
  campaignDraft?: CampaignDraftDetail | null
  recoveryDraft?: RecoveryDraftActionPayload | null
}): AssistantActionNavigatePlan {
  const locationId =
    input.campaignDraft?.locationId
    ?? input.recoveryDraft?.locationId
    ?? input.analysisScope.ownedLocationId
  if (input.route.kind === "recovery") {
    return {
      path: sectionNavPath(input.mode, "feedback", locationId),
      selectLocationId: locationId,
      recoveryDraft: input.recoveryDraft ?? undefined,
    }
  }

  const campaignId = input.route.campaignId
  const step =
    input.route.step === "schedule" || input.route.step === "review"
      ? input.route.step
      : "review"
  return {
    path: sectionNavPath(input.mode, "campaigns", locationId),
    selectLocationId: locationId,
    campaigns:
      campaignId != null
        ? {
            continueEditingCampaignId: campaignId,
            continueEditingStep: step,
            ...(input.route.scheduleMode != null
              ? { scheduleMode: input.route.scheduleMode }
              : {}),
            ...(input.route.dateLocal
              ? { dateLocal: input.route.dateLocal }
              : {}),
            ...(input.route.timeLocal
              ? { timeLocal: input.route.timeLocal }
              : {}),
            ...(input.campaignDraft != null
              ? { campaign: input.campaignDraft }
              : {}),
          }
        : undefined,
  }
}
