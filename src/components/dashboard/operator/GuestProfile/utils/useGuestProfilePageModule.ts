import { useSyncExternalStore } from "react"

import { useGuestProfilePageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import type {
  OperatorGuestProfilePageModule,
  OperatorGuestProfilePageSnapshot,
} from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"

export type OperatorGuestProfilePageModuleApi = {
  snapshot: OperatorGuestProfilePageSnapshot
  retryLoad: OperatorGuestProfilePageModule["retryLoad"]
  ensureNotesLoaded: OperatorGuestProfilePageModule["ensureNotesLoaded"]
  retryNotesLoad: OperatorGuestProfilePageModule["retryNotesLoad"]
  createNote: OperatorGuestProfilePageModule["createNote"]
  exportGuestRecord: OperatorGuestProfilePageModule["exportGuestRecord"]
  openFeedbackDetails: OperatorGuestProfilePageModule["openFeedbackDetails"]
  closeFeedbackDetails: OperatorGuestProfilePageModule["closeFeedbackDetails"]
  retryFeedbackDetails: OperatorGuestProfilePageModule["retryFeedbackDetails"]
  startClassificationCorrection: OperatorGuestProfilePageModule["startClassificationCorrection"]
  setClassificationDraftSentiment: OperatorGuestProfilePageModule["setClassificationDraftSentiment"]
  cancelClassificationCorrection: OperatorGuestProfilePageModule["cancelClassificationCorrection"]
  saveClassificationCorrection: OperatorGuestProfilePageModule["saveClassificationCorrection"]
  setFeedbackInternalNoteDraft: OperatorGuestProfilePageModule["setFeedbackInternalNoteDraft"]
  createFeedbackInternalNote: OperatorGuestProfilePageModule["createFeedbackInternalNote"]
}

export function useGuestProfilePageModule(): OperatorGuestProfilePageModuleApi {
  const pageModule = useGuestProfilePageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    retryLoad: pageModule.retryLoad,
    ensureNotesLoaded: pageModule.ensureNotesLoaded,
    retryNotesLoad: pageModule.retryNotesLoad,
    createNote: pageModule.createNote,
    exportGuestRecord: pageModule.exportGuestRecord,
    openFeedbackDetails: pageModule.openFeedbackDetails,
    closeFeedbackDetails: pageModule.closeFeedbackDetails,
    retryFeedbackDetails: pageModule.retryFeedbackDetails,
    startClassificationCorrection: pageModule.startClassificationCorrection,
    setClassificationDraftSentiment: pageModule.setClassificationDraftSentiment,
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
    setFeedbackInternalNoteDraft: pageModule.setFeedbackInternalNoteDraft,
    createFeedbackInternalNote: pageModule.createFeedbackInternalNote,
  }
}
