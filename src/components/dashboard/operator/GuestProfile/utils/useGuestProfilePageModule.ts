import { useSyncExternalStore } from "react"

import { useGuestProfilePageModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestProfilePageModuleContext"
import type {
  OperatorGuestProfilePageModule,
  OperatorGuestProfilePageSnapshot,
} from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"
import type { ManageMarketingPreferencesSnapshot } from "@/lib/operatorGuests/createManageMarketingPreferencesSessionModule"

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
  setClassificationDraftReason: OperatorGuestProfilePageModule["setClassificationDraftReason"]
  setClassificationDraftNote: OperatorGuestProfilePageModule["setClassificationDraftNote"]
  cancelClassificationCorrection: OperatorGuestProfilePageModule["cancelClassificationCorrection"]
  saveClassificationCorrection: OperatorGuestProfilePageModule["saveClassificationCorrection"]
  startFeedbackEditTags: OperatorGuestProfilePageModule["startFeedbackEditTags"]
  stageFeedbackEditTag: OperatorGuestProfilePageModule["stageFeedbackEditTag"]
  unstageFeedbackEditTag: OperatorGuestProfilePageModule["unstageFeedbackEditTag"]
  setFeedbackEditTagsSentiment: OperatorGuestProfilePageModule["setFeedbackEditTagsSentiment"]
  cancelFeedbackEditTags: OperatorGuestProfilePageModule["cancelFeedbackEditTags"]
  applyFeedbackEditTags: OperatorGuestProfilePageModule["applyFeedbackEditTags"]
  setFeedbackWorkflowStatus: OperatorGuestProfilePageModule["setFeedbackWorkflowStatus"]
  reopenFeedback: OperatorGuestProfilePageModule["reopenFeedback"]
  startFeedbackMarkNoActionNeeded: OperatorGuestProfilePageModule["startFeedbackMarkNoActionNeeded"]
  startFeedbackMarkResolved: OperatorGuestProfilePageModule["startFeedbackMarkResolved"]
  setFeedbackCloseOutReason: OperatorGuestProfilePageModule["setFeedbackCloseOutReason"]
  setFeedbackCloseOutNoteDraft: OperatorGuestProfilePageModule["setFeedbackCloseOutNoteDraft"]
  setFeedbackCloseOutAcknowledged: OperatorGuestProfilePageModule["setFeedbackCloseOutAcknowledged"]
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
  startRecovery: OperatorGuestProfilePageModule["startRecovery"]
  closeStartRecovery: OperatorGuestProfilePageModule["closeStartRecovery"]
  selectStartRecoveryIntent: OperatorGuestProfilePageModule["selectStartRecoveryIntent"]
  retryStartRecovery: OperatorGuestProfilePageModule["retryStartRecovery"]
  recoveryWizards: OperatorGuestProfilePageModule["recoveryWizards"]
  updateNote: OperatorGuestProfilePageModule["updateNote"]
  softDeleteNote: OperatorGuestProfilePageModule["softDeleteNote"]
  marketingPreferencesSnapshot: ManageMarketingPreferencesSnapshot
  openManageMarketingPreferences: OperatorGuestProfilePageModule["openManageMarketingPreferences"]
  closeManageMarketingPreferences: OperatorGuestProfilePageModule["closeManageMarketingPreferences"]
  saveManageMarketingPreferences: OperatorGuestProfilePageModule["saveManageMarketingPreferences"]
  setMarketingPreferenceDraft: OperatorGuestProfilePageModule["marketingPreferences"]["setDraftPreference"]
  setMarketingPreferenceNote: OperatorGuestProfilePageModule["marketingPreferences"]["setDraftNote"]
}

export function useGuestProfilePageModule(): OperatorGuestProfilePageModuleApi {
  const pageModule = useGuestProfilePageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const marketingPreferencesSnapshot = useSyncExternalStore(
    pageModule.marketingPreferences.subscribe,
    pageModule.marketingPreferences.getSnapshot,
    pageModule.marketingPreferences.getSnapshot
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
    setClassificationDraftReason: pageModule.setClassificationDraftReason,
    setClassificationDraftNote: pageModule.setClassificationDraftNote,
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
    startFeedbackEditTags: pageModule.startFeedbackEditTags,
    stageFeedbackEditTag: pageModule.stageFeedbackEditTag,
    unstageFeedbackEditTag: pageModule.unstageFeedbackEditTag,
    setFeedbackEditTagsSentiment: pageModule.setFeedbackEditTagsSentiment,
    cancelFeedbackEditTags: pageModule.cancelFeedbackEditTags,
    applyFeedbackEditTags: pageModule.applyFeedbackEditTags,
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
    updateNote: pageModule.updateNote,
    softDeleteNote: pageModule.softDeleteNote,
    marketingPreferencesSnapshot,
    openManageMarketingPreferences: pageModule.openManageMarketingPreferences,
    closeManageMarketingPreferences: pageModule.closeManageMarketingPreferences,
    saveManageMarketingPreferences: pageModule.saveManageMarketingPreferences,
    setMarketingPreferenceDraft: pageModule.marketingPreferences.setDraftPreference,
    setMarketingPreferenceNote: pageModule.marketingPreferences.setDraftNote,
  }
}
