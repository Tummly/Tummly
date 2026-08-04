import { useSyncExternalStore } from "react"

import { useGuestProfilePageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"

/** Edit-route commands on the shared Operator Guest Profile page module. */
export function useGuestProfileEditCommands() {
  const pageModule = useGuestProfilePageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    retryLoad: pageModule.retryLoad,
    setDraftField: pageModule.setDraftField,
    saveChanges: pageModule.saveChanges,
    stageTag: pageModule.stageTag,
    unstageTag: pageModule.unstageTag,
    cancelTagDraft: pageModule.cancelTagDraft,
    applyTags: pageModule.applyTags,
    setNoteDraft: pageModule.setNoteDraft,
    cancelNoteDraft: pageModule.cancelNoteDraft,
    saveNote: pageModule.saveNote,
    openFeedbackDetails: pageModule.openFeedbackDetails,
    closeFeedbackDetails: pageModule.closeFeedbackDetails,
    retryFeedbackDetails: pageModule.retryFeedbackDetails,
    startClassificationCorrection: pageModule.startClassificationCorrection,
    setClassificationDraftSentiment: pageModule.setClassificationDraftSentiment,
    setClassificationDraftReason: pageModule.setClassificationDraftReason,
    setClassificationDraftNote: pageModule.setClassificationDraftNote,
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
    setFeedbackWorkflowStatus: pageModule.setFeedbackWorkflowStatus,
    reopenFeedback: pageModule.reopenFeedback,
    startFeedbackMarkNoActionNeeded: pageModule.startFeedbackMarkNoActionNeeded,
    startFeedbackMarkResolved: pageModule.startFeedbackMarkResolved,
    setFeedbackCloseOutReason: pageModule.setFeedbackCloseOutReason,
    setFeedbackCloseOutNoteDraft: pageModule.setFeedbackCloseOutNoteDraft,
    setFeedbackCloseOutAcknowledged: pageModule.setFeedbackCloseOutAcknowledged,
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
    startRecovery: pageModule.startRecovery,
    closeStartRecovery: pageModule.closeStartRecovery,
    selectStartRecoveryIntent: pageModule.selectStartRecoveryIntent,
    retryStartRecovery: pageModule.retryStartRecovery,
    recoveryWizards: pageModule.recoveryWizards,
    getViewAllFeedbacksNavigation: pageModule.getViewAllFeedbacksNavigation,
    exportGuestRecord: pageModule.exportGuestRecord,
    deleteLocationGuest: pageModule.deleteLocationGuest,
  }
}
