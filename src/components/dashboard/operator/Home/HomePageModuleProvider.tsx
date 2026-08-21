import { createElement, useEffect, useState, type ReactNode } from "react"
import { toast } from "sonner"

import {
  closeOutFeedback,
  correctFeedbackClassification,
  createFeedbackInternalNote,
  getCampaignDraftById,
  getCampaignsList,
  getChecklistAcks,
  getFeedback,
  getFeedbackDetails,
  getHomeLatestActivity,
  getHomePerformance,
  getHomeRecommendation,
  listCatalogOffers,
  pauseCampaign,
  setChecklistAcks,
  setFeedbackWorkflowStatus,
  softDeleteFeedbackInternalNote,
  updateFeedbackDetectedTags,
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
      loadHomeRecommendation: async ({ request }) =>
        getHomeRecommendation(request),
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
      getChecklistAcks,
      setChecklistAcks,
      hasCreatedOffer: async (locationId) => {
        const response = await listCatalogOffers({
          locationId,
          view: "all",
          page: 1,
          pageSize: 1,
        })
        return response.tabCounts.all > 0 || response.totalCount > 0
      },
      hasCreatedCampaign: async (locationId) => {
        const response = await getCampaignsList({
          locationId,
          view: "all",
          page: 1,
          pageSize: 1,
        })
        return response.tabCounts.all > 0 || response.totalCount > 0
      },
      copyText,
      openSmartGuestLink,
      connectRealtime: connectFeedbackHomeHub,
      onPerformanceLoadError: (message) => {
        toast.error(message)
      },
      listLiveOffers: async (locationId) => {
        const response = await listCatalogOffers({
          locationId,
          view: "in-flight",
          sort: "recent-activity",
          page: 1,
          pageSize: 20,
          utcOffsetMinutes: -new Date().getTimezoneOffset(),
        })
        return response.items
      },
      listLiveCampaigns: async (locationId) => {
        const response = await getCampaignsList({
          locationId,
          view: "in-flight",
          status: ["scheduled", "sending"],
          sort: "recent-activity",
          page: 1,
          pageSize: 20,
          utcOffsetMinutes: -new Date().getTimezoneOffset(),
        })
        return response.items
      },
      pauseCampaign,
      getCampaignDraftById,
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
