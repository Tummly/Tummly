import { useSyncExternalStore } from "react"
import { toast } from "sonner"

import { useHomePageModuleApi } from "@/components/dashboard/operator/Home/utils/homePageModuleContext"
import type {
  OperatorHomePageModule,
  OperatorHomePageSnapshot,
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
  setFeedbackWorkflowStatus: OperatorHomePageModule["setFeedbackWorkflowStatus"]
  reopenFeedback: OperatorHomePageModule["reopenFeedback"]
  markFeedbackNoActionNeeded: OperatorHomePageModule["markFeedbackNoActionNeeded"]
  setFeedbackInternalNoteDraft: OperatorHomePageModule["setFeedbackInternalNoteDraft"]
  createFeedbackInternalNote: OperatorHomePageModule["createFeedbackInternalNote"]
  startFeedbackNoteEdit: OperatorHomePageModule["startFeedbackNoteEdit"]
  setFeedbackNoteEditDraft: OperatorHomePageModule["setFeedbackNoteEditDraft"]
  cancelFeedbackNoteEdit: OperatorHomePageModule["cancelFeedbackNoteEdit"]
  saveFeedbackNoteEdit: OperatorHomePageModule["saveFeedbackNoteEdit"]
  startFeedbackNoteDelete: OperatorHomePageModule["startFeedbackNoteDelete"]
  cancelFeedbackNoteDelete: OperatorHomePageModule["cancelFeedbackNoteDelete"]
  confirmFeedbackNoteDelete: OperatorHomePageModule["confirmFeedbackNoteDelete"]
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
    setFeedbackWorkflowStatus: pageModule.setFeedbackWorkflowStatus,
    reopenFeedback: pageModule.reopenFeedback,
    markFeedbackNoActionNeeded: pageModule.markFeedbackNoActionNeeded,
    setFeedbackInternalNoteDraft: pageModule.setFeedbackInternalNoteDraft,
    createFeedbackInternalNote: pageModule.createFeedbackInternalNote,
    startFeedbackNoteEdit: pageModule.startFeedbackNoteEdit,
    setFeedbackNoteEditDraft: pageModule.setFeedbackNoteEditDraft,
    cancelFeedbackNoteEdit: pageModule.cancelFeedbackNoteEdit,
    saveFeedbackNoteEdit: pageModule.saveFeedbackNoteEdit,
    startFeedbackNoteDelete: pageModule.startFeedbackNoteDelete,
    cancelFeedbackNoteDelete: pageModule.cancelFeedbackNoteDelete,
    confirmFeedbackNoteDelete: pageModule.confirmFeedbackNoteDelete,
  }
}
