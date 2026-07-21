import { useSyncExternalStore } from "react"
import { toast } from "sonner"

import { useHomePageModuleApi } from "@/components/dashboard/operator/Home/utils/homePageModuleContext"
import type {
  OperatorHomePageModule,
  HomePageSnapshot,
} from "@/lib/operatorHome/createOperatorHomePageModule"

export type OperatorHomePageModuleApi = {
  snapshot: HomePageSnapshot
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

export function useHomePageModule(): OperatorHomePageModuleApi {
  const pageModule = useHomePageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

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
