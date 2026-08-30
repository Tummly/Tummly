import { createElement, useEffect, useState, type ReactNode } from "react"

import type { OperatorFilterSelection } from "@/lib/operatorFilterSheet"

import {
  closeOutFeedback,
  completeFeedbackRecovery,
  correctFeedbackClassification,
  createCatalogOffer,
  createFeedbackInternalNote,
  exportFeedback,
  exportSingleFeedback as exportSingleFeedbackApi,
  getCatalogOfferById,
  getFeedbackDetails,
  getFeedbackInbox,
  getFeedbackRecoveryOfferAttach,
  getFeedbackSummary,
  listCatalogOffers,
  recordFeedbackInternalAction,
  respondAndRecordInternalAction,
  sendAndIssueFeedbackRecoveryOffer,
  sendFeedbackGuestResponse,
  sendGuestPreviewTest,
  setFeedbackRecoveryOfferAttach,
  setFeedbackWorkflowStatus,
  softDeleteFeedbackInternalNote,
  triggerBrowserDownload,
  updateCatalogOffer,
  updateFeedbackDetectedTags,
  updateFeedbackInternalNote,
} from "@/api/dashboardApi"
import { feedbackPageModuleContext } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorFeedbackPageModule } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import { connectFeedbackHomeHub } from "@/lib/operatorHome/connectFeedbackHomeHub"
import { parseOperatorProfile } from "@/lib/operatorHome/parseOperatorProfile"
import { fetchCurrentUser } from "@/api/loginContextClient"
import { labelForInternalActionCategory } from "@/lib/operatorFeedback/internalActionPresentation"
import { prepareRecoveryDraft as prepareRecoveryDraftHttp } from "@/lib/operatorFeedback/prepareRecoveryDraft"
import { loadRecoveryCreditChrome } from "@/lib/operatorFeedback/loadRecoveryCreditChrome"

export function FeedbackPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorFeedbackPageModule({
      getFeedbackSummary: async ({ locationId, from, to }) =>
        getFeedbackSummary(locationId, from, to),
      getFeedbackInbox: async (params) => getFeedbackInbox(params),
      exportFeedback: async (params) => exportFeedback(params),
      exportSingleFeedback: async (params) => exportSingleFeedbackApi(params),
      triggerBrowserDownload,
      getFeedbackPageDateRange: () =>
        dashboardUiStore.getState().feedbackPageDateRange,
      getFeedbackDetails,
      loadCreditChrome: async (nav) => loadRecoveryCreditChrome(nav),
      getRecoveryOfferAttach: async (feedbackId) => {
        const result = await getFeedbackRecoveryOfferAttach(feedbackId)
        return result.offerId
      },
      setRecoveryOfferAttach: async (feedbackId, offerId) => {
        await setFeedbackRecoveryOfferAttach(feedbackId, offerId)
      },
      listCatalogOffers,
      createOffer: async (body) => {
        const response = await createCatalogOffer(body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog create failed.")
        }
        return response.offer
      },
      updateOffer: async (offerId, body) => {
        const response = await updateCatalogOffer(offerId, body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog update failed.")
        }
        return response.offer
      },
      getOffer: async (offerId) => {
        const response = await getCatalogOfferById(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog load failed.")
        }
        return response.offer
      },
      correctClassification: async (feedbackId, input) => {
        const trimmedNote = input.noteBody?.trim() ?? ""
        const result = await correctFeedbackClassification(feedbackId, {
          sentiment: input.sentiment,
          reason: input.reason,
          ...(trimmedNote.length > 0 || input.reason === "other"
            ? { note: trimmedNote }
            : {}),
        })
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
          activityEvent: result.activityEvent ?? null,
        }
      },
      updateDetectedTags: async (feedbackId, input) => {
        const result = await updateFeedbackDetectedTags(feedbackId, {
          detectedTags: input.detectedTags,
          ...(input.sentiment != null ? { sentiment: input.sentiment } : {}),
        })
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
          needsAttention: result.needsAttention,
          classifiedAt: result.classifiedAt ?? null,
          activityEvent: result.activityEvent ?? null,
        }
      },
      setWorkflowStatus: async (feedbackId, workflowStatus) => {
        const result = await setFeedbackWorkflowStatus(
          feedbackId,
          workflowStatus
        )
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          activityEvent: result.activityEvent ?? null,
        }
      },
      createInternalNote: async (feedbackId, body) =>
        createFeedbackInternalNote({ feedbackId, body }),
      updateInternalNote: async (feedbackId, noteId, body) =>
        updateFeedbackInternalNote({ feedbackId, noteId, body }),
      deleteInternalNote: async (feedbackId, noteId) =>
        softDeleteFeedbackInternalNote({ feedbackId, noteId }),
      closeOutFeedback: async (feedbackId, body) => {
        const result = await closeOutFeedback(feedbackId, body)
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          activityEvent: result.activityEvent,
          noteActivityEvent: result.noteActivityEvent ?? null,
          note: result.note ?? null,
        }
      },
      sendGuestResponse: async (request) => {
        const result = await sendFeedbackGuestResponse(request.feedbackId, {
          channel: request.channel,
          subject: request.subject,
          body: request.body,
          intent: request.intent,
          purpose: request.purpose,
          tone: request.tone,
          includeNotes: request.includeNotes,
        })
        const activity = result.activityEvent
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          activityEvent: {
            kind: "guest_response_sent",
            at: activity.at,
            actorDisplayName: activity.actorDisplayName ?? null,
            channel: activity.channel ?? request.channel,
            maskedDestination: activity.maskedDestination ?? "",
          },
        }
      },
      sendGuestPreviewTest: async (request) => {
        await sendGuestPreviewTest(request.feedbackId, {
          subject: request.subject,
          body: request.body,
          toEmail: request.toEmail,
          offer: request.offer ?? null,
        })
      },
      getOperatorAccountEmail: async () => {
        const result = await fetchCurrentUser()
        return parseOperatorProfile(result)?.email ?? null
      },
      completeRecovery: async (feedbackId, intent) => {
        const result = await completeFeedbackRecovery(feedbackId, { intent })
        const activity = result.activityEvent
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          activityEvent: {
            kind: "recovery_completed",
            at: activity.at,
            actorDisplayName: activity.actorDisplayName ?? null,
            recoveryIntent: activity.recoveryIntent ?? intent,
            fromWorkflowStatus: activity.fromWorkflowStatus ?? "in_progress",
            toWorkflowStatus: "resolved",
          },
        }
      },
      recordInternalAction: async (request) => {
        const result = await recordFeedbackInternalAction(request.feedbackId, {
          category: request.category,
          note: request.note,
          intent: request.intent,
        })
        const activity = result.activityEvent
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          activityEvent: {
            kind: "internal_action_recorded",
            at: activity.at,
            actorDisplayName: activity.actorDisplayName ?? null,
            category: (activity.category
              ?? request.category) as typeof request.category,
            categoryLabel:
              activity.categoryLabel
              ?? labelForInternalActionCategory(request.category)
              ?? request.category,
            note: activity.note ?? request.note,
          },
        }
      },
      sendAndRecord: async (request) => {
        const result = await respondAndRecordInternalAction(request.feedbackId, {
          channel: request.channel,
          subject: request.subject,
          body: request.body,
          intent: request.intent,
          purpose: request.purpose,
          tone: request.tone,
          includeNotes: request.includeNotes,
          category: request.category,
          note: request.note,
        })
        const guestActivity = result.guestResponseActivityEvent
        const internalActivity = result.internalActionActivityEvent
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          guestResponseActivityEvent: {
            kind: "guest_response_sent",
            at: guestActivity.at,
            actorDisplayName: guestActivity.actorDisplayName ?? null,
            channel: guestActivity.channel ?? request.channel,
            maskedDestination: guestActivity.maskedDestination ?? "",
          },
          internalActionActivityEvent: {
            kind: "internal_action_recorded",
            at: internalActivity.at,
            actorDisplayName: internalActivity.actorDisplayName ?? null,
            category: (internalActivity.category
              ?? request.category) as typeof request.category,
            categoryLabel:
              internalActivity.categoryLabel
              ?? labelForInternalActionCategory(request.category)
              ?? request.category,
            note: internalActivity.note ?? request.note,
          },
        }
      },
      sendAndIssueRecoveryOffer: async (request) => {
        const result = await sendAndIssueFeedbackRecoveryOffer(
          request.feedbackId,
          {
            channel: request.channel,
            subject: request.subject,
            body: request.body,
            intent: request.intent,
            purpose: request.purpose,
            tone: request.tone,
            includeNotes: request.includeNotes,
            offer: request.offer,
          }
        )
        const guestActivity = result.activityEvent
        const offerActivity = result.recoveryOfferActivityEvent
        return {
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          guestResponseActivityEvent: {
            kind: "guest_response_sent",
            at: guestActivity.at,
            actorDisplayName: guestActivity.actorDisplayName ?? null,
            channel: guestActivity.channel ?? request.channel,
            maskedDestination: guestActivity.maskedDestination ?? "",
          },
          recoveryOfferActivityEvent: {
            kind: "recovery_offer_issued",
            at: offerActivity.at,
            actorDisplayName: offerActivity.actorDisplayName ?? null,
            offerType:
              (offerActivity.offerType as typeof request.offer.offerType)
              ?? request.offer.offerType,
            title:
              offerActivity.offerTitle
              ?? result.recoveryOffer.title
              ?? request.offer.title,
            validity:
              (offerActivity.offerValidity as typeof request.offer.validity)
              ?? (result.recoveryOffer.validity as typeof request.offer.validity)
              ?? request.offer.validity,
            expiryAt:
              offerActivity.offerExpiryAt
              ?? result.recoveryOffer.expiryAt
              ?? null,
            redemptionCode:
              offerActivity.redemptionCode
              ?? result.recoveryOffer.redemptionCode,
          },
          issuedOffer: {
            title: result.recoveryOffer.title,
            redemptionCode: result.recoveryOffer.redemptionCode,
            expiryAt: result.recoveryOffer.expiryAt,
            validity:
              result.recoveryOffer.validity as typeof request.offer.validity,
          },
        }
      },
      prepareRecoveryOfferDraft: async (request, signal) =>
        prepareRecoveryDraftHttp(
          request.feedbackId,
          {
            channel: request.channel,
            purpose: request.purpose,
            tone: request.tone,
            includeNotes: request.includeNotes,
            mode: request.mode,
            currentBody: request.currentBody,
            currentSubject: request.currentSubject,
            confirmedOffer: request.confirmedOffer,
          },
          signal
        ),
      prepareRecoveryDraft: async (request, signal) =>
        prepareRecoveryDraftHttp(
          request.feedbackId,
          {
            channel: request.channel,
            purpose: request.purpose,
            tone: request.tone,
            includeNotes: request.includeNotes,
            mode: request.mode,
            currentBody: request.currentBody,
            currentSubject: request.currentSubject,
            confirmedInternalActionCategory:
              request.confirmedInternalAction?.category ?? null,
            confirmedInternalActionNote:
              request.confirmedInternalAction?.note ?? null,
          },
          signal
        ),
      connectRealtime: connectFeedbackHomeHub,
    })
  )

  useEffect(() => {
    void pageModule.connect()
    return () => {
      void pageModule.disconnect()
    }
  }, [pageModule])

  useEffect(() => {
    return dashboardUiStore.subscribe((state) => {
      const intent = state.feedbackInboxIntent
      if (intent == null) {
        return
      }

      const filters: OperatorFilterSelection = {}
      if (intent.sentiment) {
        filters.sentiment = {
          kind: "multi-select",
          ids: [intent.sentiment],
        }
      }
      if (intent.detectedTag) {
        filters.detectedTag = {
          kind: "multi-select",
          ids: [intent.detectedTag],
        }
      }
      pageModule.applyFilters(filters)
      if (intent.tab) {
        pageModule.setActiveInboxTabId(intent.tab)
      }
      state.setFeedbackInboxIntent(null)
    })
  }, [dashboardUiStore, pageModule])

  return createElement(
    feedbackPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

