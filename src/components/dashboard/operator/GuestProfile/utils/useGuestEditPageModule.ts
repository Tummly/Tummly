import { useSyncExternalStore } from "react"

import { useGuestEditPageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestEditPageModuleContext"

export function useGuestEditPageModule() {
  const pageModule = useGuestEditPageModuleApi()
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
    getViewAllFeedbacksNavigation: pageModule.getViewAllFeedbacksNavigation,
    exportGuestRecord: pageModule.exportGuestRecord,
    deleteGuest: pageModule.deleteGuest,
  }
}
