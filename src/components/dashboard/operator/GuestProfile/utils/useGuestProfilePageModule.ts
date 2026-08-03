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
  setFeedbackWorkflowStatus: OperatorGuestProfilePageModule["setFeedbackWorkflowStatus"]
  reopenFeedback: OperatorGuestProfilePageModule["reopenFeedback"]
  startFeedbackMarkNoActionNeeded: OperatorGuestProfilePageModule["startFeedbackMarkNoActionNeeded"]
  startFeedbackMarkResolved: OperatorGuestProfilePageModule["startFeedbackMarkResolved"]
  setFeedbackCloseOutReason: OperatorGuestProfilePageModule["setFeedbackCloseOutReason"]
  setFeedbackCloseOutNoteDraft: OperatorGuestProfilePageModule["setFeedbackCloseOutNoteDraft"]
  cancelFeedbackCloseOut: OperatorGuestProfilePageModule["cancelFeedbackCloseOut"]
  confirmFeedbackCloseOut: OperatorGuestProfilePageModule["confirmFeedbackCloseOut"]
  setFeedbackInternalNoteDraft: OperatorGuestProfilePageModule["setFeedbackInternalNoteDraft"]
  createFeedbackInternalNote: OperatorGuestProfilePageModule["createFeedbackInternalNote"]
  startFeedbackNoteEdit: OperatorGuestProfilePageModule["startFeedbackNoteEdit"]
  setFeedbackNoteEditDraft: OperatorGuestProfilePageModule["setFeedbackNoteEditDraft"]
  cancelFeedbackNoteEdit: OperatorGuestProfilePageModule["cancelFeedbackNoteEdit"]
  saveFeedbackNoteEdit: OperatorGuestProfilePageModule["saveFeedbackNoteEdit"]
  startFeedbackNoteDelete: OperatorGuestProfilePageModule["startFeedbackNoteDelete"]
  cancelFeedbackNoteDelete: OperatorGuestProfilePageModule["cancelFeedbackNoteDelete"]
  confirmFeedbackNoteDelete: OperatorGuestProfilePageModule["confirmFeedbackNoteDelete"]
  updateNote: OperatorGuestProfilePageModule["updateNote"]
  softDeleteNote: OperatorGuestProfilePageModule["softDeleteNote"]
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
    setFeedbackWorkflowStatus: pageModule.setFeedbackWorkflowStatus,
    reopenFeedback: pageModule.reopenFeedback,
    startFeedbackMarkNoActionNeeded: pageModule.startFeedbackMarkNoActionNeeded,
    startFeedbackMarkResolved: pageModule.startFeedbackMarkResolved,
    setFeedbackCloseOutReason: pageModule.setFeedbackCloseOutReason,
    setFeedbackCloseOutNoteDraft: pageModule.setFeedbackCloseOutNoteDraft,
    cancelFeedbackCloseOut: pageModule.cancelFeedbackCloseOut,
    confirmFeedbackCloseOut: pageModule.confirmFeedbackCloseOut,
    setFeedbackInternalNoteDraft: pageModule.setFeedbackInternalNoteDraft,
    createFeedbackInternalNote: pageModule.createFeedbackInternalNote,
    startFeedbackNoteEdit: pageModule.startFeedbackNoteEdit,
    setFeedbackNoteEditDraft: pageModule.setFeedbackNoteEditDraft,
    cancelFeedbackNoteEdit: pageModule.cancelFeedbackNoteEdit,
    saveFeedbackNoteEdit: pageModule.saveFeedbackNoteEdit,
    startFeedbackNoteDelete: pageModule.startFeedbackNoteDelete,
    cancelFeedbackNoteDelete: pageModule.cancelFeedbackNoteDelete,
    confirmFeedbackNoteDelete: pageModule.confirmFeedbackNoteDelete,
    updateNote: pageModule.updateNote,
    softDeleteNote: pageModule.softDeleteNote,
  }
}
