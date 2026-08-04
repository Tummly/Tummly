import { createElement, useEffect, useState, type ReactNode } from "react"
import { toast } from "sonner"

import {
  closeOutFeedback,
  correctFeedbackClassification,
  createFeedbackInternalNote,
  getChecklistAcks,
  getFeedback,
  getFeedbackDetails,
  getHomeLatestActivity,
  getHomePerformance,
  setChecklistAcks,
  setFeedbackWorkflowStatus,
  softDeleteFeedbackInternalNote,
  updateFeedbackInternalNote,
} from "@/api/dashboardApi"
import { homePageModuleContext } from "@/components/dashboard/operator/Home/utils/homePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { connectFeedbackHomeHub } from "@/lib/operatorHome/connectFeedbackHomeHub"
import {
  createOperatorHomePageModule,
} from "@/lib/operatorHome/createOperatorHomePageModule"

function openSmartGuestLink(url: string): void {
  window.open(url, "_blank", "noopener,noreferrer")
}

async function copyText(
  text: string
): Promise<{ ok: true } | { ok: false; error: string }> {
  try {
    await navigator.clipboard.writeText(text)
    return { ok: true }
  } catch {
    return {
      ok: false,
      error: "Could not copy Smart Guest Link. Please try again.",
    }
  }
}

export function HomePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorHomePageModule({
      getFeedback,
      getHomeLatestActivity,
      getHomePerformance,
      getHomePerformanceDateRange: () =>
        dashboardUiStore.getState().homePerformanceDateRange,
      getFeedbackDetails,
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
      getChecklistAcks,
      setChecklistAcks,
      copyText,
      openSmartGuestLink,
      connectRealtime: connectFeedbackHomeHub,
      onPerformanceLoadError: (message) => {
        toast.error(message)
      },
    })
  )

  useEffect(() => {
    void pageModule.connect()
    return () => {
      void pageModule.disconnect()
    }
  }, [pageModule])

  return createElement(
    homePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
