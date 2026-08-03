import { useSyncExternalStore } from "react"

import { useFeedbackPageModuleApi } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import type {
  OperatorFeedbackPageModule,
  OperatorFeedbackPageSnapshot,
} from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"

export type OperatorFeedbackPageModuleApi = {
  snapshot: OperatorFeedbackPageSnapshot
  retryLoad: OperatorFeedbackPageModule["retryLoad"]
  reloadForFeedbackPageDateRange: OperatorFeedbackPageModule["reloadForFeedbackPageDateRange"]
  reviewNeedsAttention: OperatorFeedbackPageModule["reviewNeedsAttention"]
  requestOpenDateRange: OperatorFeedbackPageModule["requestOpenDateRange"]
  setActiveInboxTabId: OperatorFeedbackPageModule["setActiveInboxTabId"]
  setSearchQuery: OperatorFeedbackPageModule["setSearchQuery"]
  setSortId: OperatorFeedbackPageModule["setSortId"]
  goToPreviousPage: OperatorFeedbackPageModule["goToPreviousPage"]
  goToNextPage: OperatorFeedbackPageModule["goToNextPage"]
  openFilters: OperatorFeedbackPageModule["openFilters"]
  closeFilters: OperatorFeedbackPageModule["closeFilters"]
  setFiltersSession: OperatorFeedbackPageModule["setFiltersSession"]
  applyFilters: OperatorFeedbackPageModule["applyFilters"]
  removeFilterChip: OperatorFeedbackPageModule["removeFilterChip"]
  clearSearchAndFilters: OperatorFeedbackPageModule["clearSearchAndFilters"]
  openExportDialog: OperatorFeedbackPageModule["openExportDialog"]
  closeExportDialog: OperatorFeedbackPageModule["closeExportDialog"]
  setExportScope: OperatorFeedbackPageModule["setExportScope"]
  setExportFormat: OperatorFeedbackPageModule["setExportFormat"]
  setExportIncludeGuestContact: OperatorFeedbackPageModule["setExportIncludeGuestContact"]
  downloadExport: OperatorFeedbackPageModule["downloadExport"]
  openFeedbackDetails: OperatorFeedbackPageModule["openFeedbackDetails"]
  closeFeedbackDetails: OperatorFeedbackPageModule["closeFeedbackDetails"]
  openPreviousFeedback: OperatorFeedbackPageModule["openPreviousFeedback"]
  openNextFeedback: OperatorFeedbackPageModule["openNextFeedback"]
  startInboxMarkResolved: OperatorFeedbackPageModule["startInboxMarkResolved"]
  startInboxMarkNoActionNeeded: OperatorFeedbackPageModule["startInboxMarkNoActionNeeded"]
  startInboxRecovery: OperatorFeedbackPageModule["startInboxRecovery"]
  closeStartRecovery: OperatorFeedbackPageModule["closeStartRecovery"]
  selectStartRecoveryIntent: OperatorFeedbackPageModule["selectStartRecoveryIntent"]
  retryStartRecovery: OperatorFeedbackPageModule["retryStartRecovery"]
  saveAndExitRespondToGuest: OperatorFeedbackPageModule["saveAndExitRespondToGuest"]
  closeRespondToGuest: OperatorFeedbackPageModule["closeRespondToGuest"]
  backRespondToGuest: OperatorFeedbackPageModule["backRespondToGuest"]
  setRespondToGuestChannel: OperatorFeedbackPageModule["setRespondToGuestChannel"]
  setRespondToGuestPurpose: OperatorFeedbackPageModule["setRespondToGuestPurpose"]
  setRespondToGuestTone: OperatorFeedbackPageModule["setRespondToGuestTone"]
  setRespondToGuestIncludeNotes: OperatorFeedbackPageModule["setRespondToGuestIncludeNotes"]
  continueRespondToGuestSetup: OperatorFeedbackPageModule["continueRespondToGuestSetup"]
  writeRespondToGuestManually: OperatorFeedbackPageModule["writeRespondToGuestManually"]
  prepareRespondToGuestDraft: OperatorFeedbackPageModule["prepareRespondToGuestDraft"]
  rewriteRespondToGuestDraft: OperatorFeedbackPageModule["rewriteRespondToGuestDraft"]
  retryRespondToGuestAiDraft: OperatorFeedbackPageModule["retryRespondToGuestAiDraft"]
  dismissRespondToGuestPreparingOverlay: OperatorFeedbackPageModule["dismissRespondToGuestPreparingOverlay"]
  setRespondToGuestSubject: OperatorFeedbackPageModule["setRespondToGuestSubject"]
  setRespondToGuestMessage: OperatorFeedbackPageModule["setRespondToGuestMessage"]
  continueRespondToGuestWrite: OperatorFeedbackPageModule["continueRespondToGuestWrite"]
  openRespondToGuestSendConfirm: OperatorFeedbackPageModule["openRespondToGuestSendConfirm"]
  cancelRespondToGuestSendConfirm: OperatorFeedbackPageModule["cancelRespondToGuestSendConfirm"]
  confirmRespondToGuestSend: OperatorFeedbackPageModule["confirmRespondToGuestSend"]
  keepRespondToGuestInProgress: OperatorFeedbackPageModule["keepRespondToGuestInProgress"]
  markRespondToGuestResolved: OperatorFeedbackPageModule["markRespondToGuestResolved"]
  retryFeedbackDetails: OperatorFeedbackPageModule["retryFeedbackDetails"]
  startClassificationCorrection: OperatorFeedbackPageModule["startClassificationCorrection"]
  setClassificationDraftSentiment: OperatorFeedbackPageModule["setClassificationDraftSentiment"]
  cancelClassificationCorrection: OperatorFeedbackPageModule["cancelClassificationCorrection"]
  saveClassificationCorrection: OperatorFeedbackPageModule["saveClassificationCorrection"]
  setFeedbackWorkflowStatus: OperatorFeedbackPageModule["setFeedbackWorkflowStatus"]
  reopenFeedbackDetails: OperatorFeedbackPageModule["reopenFeedbackDetails"]
  startFeedbackMarkNoActionNeeded: OperatorFeedbackPageModule["startFeedbackMarkNoActionNeeded"]
  startFeedbackMarkResolved: OperatorFeedbackPageModule["startFeedbackMarkResolved"]
  setFeedbackCloseOutReason: OperatorFeedbackPageModule["setFeedbackCloseOutReason"]
  setFeedbackCloseOutNoteDraft: OperatorFeedbackPageModule["setFeedbackCloseOutNoteDraft"]
  cancelFeedbackCloseOut: OperatorFeedbackPageModule["cancelFeedbackCloseOut"]
  confirmFeedbackCloseOut: OperatorFeedbackPageModule["confirmFeedbackCloseOut"]
  setFeedbackInternalNoteDraft: OperatorFeedbackPageModule["setFeedbackInternalNoteDraft"]
  createFeedbackInternalNote: OperatorFeedbackPageModule["createFeedbackInternalNote"]
  startFeedbackNoteEdit: OperatorFeedbackPageModule["startFeedbackNoteEdit"]
  setFeedbackNoteEditDraft: OperatorFeedbackPageModule["setFeedbackNoteEditDraft"]
  cancelFeedbackNoteEdit: OperatorFeedbackPageModule["cancelFeedbackNoteEdit"]
  saveFeedbackNoteEdit: OperatorFeedbackPageModule["saveFeedbackNoteEdit"]
  startFeedbackNoteDelete: OperatorFeedbackPageModule["startFeedbackNoteDelete"]
  cancelFeedbackNoteDelete: OperatorFeedbackPageModule["cancelFeedbackNoteDelete"]
  confirmFeedbackNoteDelete: OperatorFeedbackPageModule["confirmFeedbackNoteDelete"]
}

export function useFeedbackPageModule(): OperatorFeedbackPageModuleApi {
  const pageModule = useFeedbackPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    retryLoad: pageModule.retryLoad,
    reloadForFeedbackPageDateRange: pageModule.reloadForFeedbackPageDateRange,
    reviewNeedsAttention: pageModule.reviewNeedsAttention,
    requestOpenDateRange: pageModule.requestOpenDateRange,
    setActiveInboxTabId: pageModule.setActiveInboxTabId,
    setSearchQuery: pageModule.setSearchQuery,
    setSortId: pageModule.setSortId,
    goToPreviousPage: pageModule.goToPreviousPage,
    goToNextPage: pageModule.goToNextPage,
    openFilters: pageModule.openFilters,
    closeFilters: pageModule.closeFilters,
    setFiltersSession: pageModule.setFiltersSession,
    applyFilters: pageModule.applyFilters,
    removeFilterChip: pageModule.removeFilterChip,
    clearSearchAndFilters: pageModule.clearSearchAndFilters,
    openExportDialog: pageModule.openExportDialog,
    closeExportDialog: pageModule.closeExportDialog,
    setExportScope: pageModule.setExportScope,
    setExportFormat: pageModule.setExportFormat,
    setExportIncludeGuestContact: pageModule.setExportIncludeGuestContact,
    downloadExport: pageModule.downloadExport,
    openFeedbackDetails: pageModule.openFeedbackDetails,
    closeFeedbackDetails: pageModule.closeFeedbackDetails,
    openPreviousFeedback: pageModule.openPreviousFeedback,
    openNextFeedback: pageModule.openNextFeedback,
    startInboxMarkResolved: pageModule.startInboxMarkResolved,
    startInboxMarkNoActionNeeded: pageModule.startInboxMarkNoActionNeeded,
    startInboxRecovery: pageModule.startInboxRecovery,
    closeStartRecovery: pageModule.closeStartRecovery,
    selectStartRecoveryIntent: pageModule.selectStartRecoveryIntent,
    retryStartRecovery: pageModule.retryStartRecovery,
    saveAndExitRespondToGuest: pageModule.saveAndExitRespondToGuest,
    closeRespondToGuest: pageModule.closeRespondToGuest,
    backRespondToGuest: pageModule.backRespondToGuest,
    setRespondToGuestChannel: pageModule.setRespondToGuestChannel,
    setRespondToGuestPurpose: pageModule.setRespondToGuestPurpose,
    setRespondToGuestTone: pageModule.setRespondToGuestTone,
    setRespondToGuestIncludeNotes: pageModule.setRespondToGuestIncludeNotes,
    continueRespondToGuestSetup: pageModule.continueRespondToGuestSetup,
    writeRespondToGuestManually: pageModule.writeRespondToGuestManually,
    prepareRespondToGuestDraft: pageModule.prepareRespondToGuestDraft,
    rewriteRespondToGuestDraft: pageModule.rewriteRespondToGuestDraft,
    retryRespondToGuestAiDraft: pageModule.retryRespondToGuestAiDraft,
    dismissRespondToGuestPreparingOverlay:
      pageModule.dismissRespondToGuestPreparingOverlay,
    setRespondToGuestSubject: pageModule.setRespondToGuestSubject,
    setRespondToGuestMessage: pageModule.setRespondToGuestMessage,
    continueRespondToGuestWrite: pageModule.continueRespondToGuestWrite,
    openRespondToGuestSendConfirm: pageModule.openRespondToGuestSendConfirm,
    cancelRespondToGuestSendConfirm: pageModule.cancelRespondToGuestSendConfirm,
    confirmRespondToGuestSend: pageModule.confirmRespondToGuestSend,
    keepRespondToGuestInProgress: pageModule.keepRespondToGuestInProgress,
    markRespondToGuestResolved: pageModule.markRespondToGuestResolved,
    retryFeedbackDetails: pageModule.retryFeedbackDetails,
    startClassificationCorrection: pageModule.startClassificationCorrection,
    setClassificationDraftSentiment: pageModule.setClassificationDraftSentiment,
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
    setFeedbackWorkflowStatus: pageModule.setFeedbackWorkflowStatus,
    reopenFeedbackDetails: pageModule.reopenFeedbackDetails,
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
  }
}

export type { OperatorFeedbackPageSnapshot }
