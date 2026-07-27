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
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
    setFeedbackInternalNoteDraft: pageModule.setFeedbackInternalNoteDraft,
    createFeedbackInternalNote: pageModule.createFeedbackInternalNote,
    startFeedbackNoteEdit: pageModule.startFeedbackNoteEdit,
    setFeedbackNoteEditDraft: pageModule.setFeedbackNoteEditDraft,
    cancelFeedbackNoteEdit: pageModule.cancelFeedbackNoteEdit,
    saveFeedbackNoteEdit: pageModule.saveFeedbackNoteEdit,
    startFeedbackNoteDelete: pageModule.startFeedbackNoteDelete,
    cancelFeedbackNoteDelete: pageModule.cancelFeedbackNoteDelete,
    confirmFeedbackNoteDelete: pageModule.confirmFeedbackNoteDelete,
    getViewAllFeedbacksNavigation: pageModule.getViewAllFeedbacksNavigation,
    exportGuestRecord: pageModule.exportGuestRecord,
    deleteLocationGuest: pageModule.deleteLocationGuest,
  }
}
