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
  sendFeedbackGuestResponse,
  setFeedbackWorkflowStatus,
  softDeleteFeedbackInternalNote,
  triggerBrowserDownload,
  updateFeedbackInternalNote,
} from "@/api/dashboardApi"
import { feedbackPageModuleContext } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorFeedbackPageModule } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import { connectFeedbackHomeHub } from "@/lib/operatorHome/connectFeedbackHomeHub"

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

