import { createElement, useEffect, useState, type ReactNode } from "react"

import {
  closeOutFeedback,
  completeFeedbackRecovery,
  correctFeedbackClassification,
  createFeedbackInternalNote,
  exportFeedback,
  getFeedbackDetails,
  getFeedbackInbox,
  getFeedbackSummary,
  prepareFeedbackRecoveryDraft,
  recordFeedbackInternalAction,
  respondAndRecordInternalAction,
  sendAndIssueFeedbackRecoveryOffer,
  sendFeedbackGuestResponse,
  setFeedbackWorkflowStatus,
  softDeleteFeedbackInternalNote,
  triggerBrowserDownload,
  updateFeedbackInternalNote,
} from "@/api/dashboardApi"
import { isAxiosError } from "axios"
import { feedbackPageModuleContext } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorFeedbackPageModule } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import { connectFeedbackHomeHub } from "@/lib/operatorHome/connectFeedbackHomeHub"
import { labelForInternalActionCategory } from "@/lib/operatorFeedback/internalActionPresentation"
import type { PrepareRecoveryDraftResult } from "@/lib/operatorFeedback/createRespondToGuestModule"

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
      triggerBrowserDownload,
      getFeedbackPageDateRange: () =>
        dashboardUiStore.getState().feedbackPageDateRange,
      getFeedbackDetails,
      correctClassification: async (feedbackId, sentiment) => {
        const result = await correctFeedbackClassification(
          feedbackId,
          sentiment
        )
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
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
            title: offerActivity.offerTitle ?? request.offer.title,
            validity:
              (offerActivity.offerValidity as typeof request.offer.validity)
              ?? request.offer.validity,
            expiryAt: offerActivity.offerExpiryAt ?? null,
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
      prepareRecoveryOfferDraft: async (request, signal) => {
        const timeoutMs = 60_000
        const timeoutController = new AbortController()
        const timeoutId = window.setTimeout(() => {
          timeoutController.abort()
        }, timeoutMs)
        const onOuterAbort = () => {
          timeoutController.abort()
        }
        signal?.addEventListener("abort", onOuterAbort)
        try {
          const result = await prepareFeedbackRecoveryDraft(
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
            timeoutController.signal
          )
          if (!result.success || result.body == null || result.channel == null) {
            return {
              status: "failed",
              retryable: result.retryable !== false,
            } satisfies PrepareRecoveryDraftResult
          }
          return {
            status: "succeeded",
            body: result.body,
            subject: result.subject ?? null,
            channel: result.channel,
          } satisfies PrepareRecoveryDraftResult
        } catch (error) {
          if (signal?.aborted) {
            throw error
          }
          if (isAxiosError(error) && error.code === "ERR_CANCELED") {
            if (signal?.aborted) {
              throw error
            }
            return { status: "failed", retryable: true }
          }
          if (isAxiosError(error) && error.response?.status === 502) {
            const data = error.response.data as
              | { retryable?: boolean }
              | undefined
            return {
              status: "failed",
              retryable: data?.retryable !== false,
            }
          }
          return { status: "failed", retryable: true }
        } finally {
          window.clearTimeout(timeoutId)
          signal?.removeEventListener("abort", onOuterAbort)
        }
      },
      prepareRecoveryDraft: async (request, signal) => {
        const timeoutMs = 60_000
        const timeoutController = new AbortController()
        const timeoutId = window.setTimeout(() => {
          timeoutController.abort()
        }, timeoutMs)
        const onOuterAbort = () => {
          timeoutController.abort()
        }
        signal?.addEventListener("abort", onOuterAbort)
        try {
          const result = await prepareFeedbackRecoveryDraft(
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
            timeoutController.signal
          )
          if (!result.success || result.body == null || result.channel == null) {
            return {
              status: "failed",
              retryable: result.retryable !== false,
            } satisfies PrepareRecoveryDraftResult
          }
          return {
            status: "succeeded",
            body: result.body,
            subject: result.subject ?? null,
            channel: result.channel,
          } satisfies PrepareRecoveryDraftResult
        } catch (error) {
          if (signal?.aborted) {
            throw error
          }
          if (isAxiosError(error) && error.code === "ERR_CANCELED") {
            if (signal?.aborted) {
              throw error
            }
            return { status: "failed", retryable: true }
          }
          if (isAxiosError(error) && error.response?.status === 502) {
            const data = error.response.data as
              | { retryable?: boolean }
              | undefined
            return {
              status: "failed",
              retryable: data?.retryable !== false,
            }
          }
          return { status: "failed", retryable: true }
        } finally {
          window.clearTimeout(timeoutId)
          signal?.removeEventListener("abort", onOuterAbort)
        }
      },
      connectRealtime: connectFeedbackHomeHub,
    })
  )

  useEffect(() => {
    void pageModule.connect()
    return () => {
      void pageModule.disconnect()
    }
  }, [pageModule])

  return createElement(
    feedbackPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

