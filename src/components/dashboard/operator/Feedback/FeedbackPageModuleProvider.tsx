import { createElement, useEffect, useState, type ReactNode } from "react"

import {
  correctFeedbackClassification,
  createFeedbackInternalNote,
  getFeedbackDetails,
  getFeedbackInbox,
  getFeedbackSummary,
  setFeedbackWorkflowStatus,
  softDeleteFeedbackInternalNote,
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
