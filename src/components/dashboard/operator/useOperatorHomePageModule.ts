import { useEffect, useRef, useSyncExternalStore } from "react"
import { toast } from "sonner"

import {
  correctFeedbackClassification,
  getChecklistAcks,
  getFeedback,
  getFeedbackDetails,
  getHomePerformance,
  setChecklistAcks,
} from "@/api/dashboardApi"
import { useOperatorDashboardUiStoreApi } from "@/components/dashboard/operator/OperatorDashboardUiStoreProvider"
import { connectFeedbackHomeHub } from "@/lib/operatorHome/connectFeedbackHomeHub"
import {
  createOperatorHomePageModule,
  type OperatorHomePageModule,
  type OperatorHomePageSnapshot,
} from "@/lib/operatorHome/createOperatorHomePageModule"

export type OperatorHomePageModuleApi = {
  snapshot: OperatorHomePageSnapshot
  syncWorkspace: OperatorHomePageModule["syncWorkspace"]
  retryLoad: OperatorHomePageModule["retryLoad"]
  reloadForHomePerformanceDateRange: OperatorHomePageModule["reloadForHomePerformanceDateRange"]
  previewGuestForm: OperatorHomePageModule["previewGuestForm"]
  copySmartGuestLink: () => void
  openFeedbackDetails: OperatorHomePageModule["openFeedbackDetails"]
  closeFeedbackDetails: OperatorHomePageModule["closeFeedbackDetails"]
  retryFeedbackDetails: OperatorHomePageModule["retryFeedbackDetails"]
  startClassificationCorrection: OperatorHomePageModule["startClassificationCorrection"]
  setClassificationDraftSentiment: OperatorHomePageModule["setClassificationDraftSentiment"]
  cancelClassificationCorrection: OperatorHomePageModule["cancelClassificationCorrection"]
  saveClassificationCorrection: OperatorHomePageModule["saveClassificationCorrection"]
}

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

export function useOperatorHomePageModule(): OperatorHomePageModuleApi {
  const dashboardUiStore = useOperatorDashboardUiStoreApi()
  const moduleRef = useRef<OperatorHomePageModule | null>(null)

  if (moduleRef.current == null) {
    moduleRef.current = createOperatorHomePageModule({
      getFeedback,
      getHomePerformance,
      getHomePerformanceDateRange: () =>
        dashboardUiStore.getState().homePerformanceDateRange,
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
  }

  const pageModule = moduleRef.current
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  const connectRef = useRef(pageModule.connect)
  const disconnectRef = useRef(pageModule.disconnect)
  connectRef.current = pageModule.connect
  disconnectRef.current = pageModule.disconnect

  useEffect(() => {
    void connectRef.current()
    return () => {
      void disconnectRef.current()
    }
  }, [])

  return {
    snapshot,
    syncWorkspace: pageModule.syncWorkspace,
    retryLoad: pageModule.retryLoad,
    reloadForHomePerformanceDateRange:
      pageModule.reloadForHomePerformanceDateRange,
    previewGuestForm: pageModule.previewGuestForm,
    copySmartGuestLink: () => {
      void pageModule.copySmartGuestLink().then((result) => {
        if (result === "copied") {
          toast.success("Smart Guest Link copied")
        }
      })
    },
    openFeedbackDetails: pageModule.openFeedbackDetails,
    closeFeedbackDetails: pageModule.closeFeedbackDetails,
    retryFeedbackDetails: pageModule.retryFeedbackDetails,
    startClassificationCorrection: pageModule.startClassificationCorrection,
    setClassificationDraftSentiment: pageModule.setClassificationDraftSentiment,
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
  }
}
