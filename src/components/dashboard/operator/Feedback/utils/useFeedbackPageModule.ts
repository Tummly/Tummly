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
  editRespondToGuestText: OperatorFeedbackPageModule["editRespondToGuestText"]
  openRespondToGuestGuestPreview: OperatorFeedbackPageModule["openRespondToGuestGuestPreview"]
  closeRespondToGuestGuestPreview: OperatorFeedbackPageModule["closeRespondToGuestGuestPreview"]
  openRespondToGuestSendConfirm: OperatorFeedbackPageModule["openRespondToGuestSendConfirm"]
  cancelRespondToGuestSendConfirm: OperatorFeedbackPageModule["cancelRespondToGuestSendConfirm"]
  confirmRespondToGuestSend: OperatorFeedbackPageModule["confirmRespondToGuestSend"]
  keepRespondToGuestInProgress: OperatorFeedbackPageModule["keepRespondToGuestInProgress"]
  markRespondToGuestResolved: OperatorFeedbackPageModule["markRespondToGuestResolved"]
  saveAndExitRecordInternalAction: OperatorFeedbackPageModule["saveAndExitRecordInternalAction"]
  closeRecordInternalAction: OperatorFeedbackPageModule["closeRecordInternalAction"]
  backRecordInternalAction: OperatorFeedbackPageModule["backRecordInternalAction"]
  setRecordInternalActionCategory: OperatorFeedbackPageModule["setRecordInternalActionCategory"]
  setRecordInternalActionNote: OperatorFeedbackPageModule["setRecordInternalActionNote"]
  continueRecordInternalActionRecorder: OperatorFeedbackPageModule["continueRecordInternalActionRecorder"]
  openRecordInternalActionConfirm: OperatorFeedbackPageModule["openRecordInternalActionConfirm"]
  cancelRecordInternalActionConfirm: OperatorFeedbackPageModule["cancelRecordInternalActionConfirm"]
  confirmRecordInternalAction: OperatorFeedbackPageModule["confirmRecordInternalAction"]
  keepRecordInternalActionInProgress: OperatorFeedbackPageModule["keepRecordInternalActionInProgress"]
  markRecordInternalActionResolved: OperatorFeedbackPageModule["markRecordInternalActionResolved"]
  saveAndExitRespondAndRecord: OperatorFeedbackPageModule["saveAndExitRespondAndRecord"]
  closeRespondAndRecord: OperatorFeedbackPageModule["closeRespondAndRecord"]
  backRespondAndRecord: OperatorFeedbackPageModule["backRespondAndRecord"]
  setRespondAndRecordCategory: OperatorFeedbackPageModule["setRespondAndRecordCategory"]
  setRespondAndRecordNote: OperatorFeedbackPageModule["setRespondAndRecordNote"]
  setRespondAndRecordUseConfirmedAction: OperatorFeedbackPageModule["setRespondAndRecordUseConfirmedAction"]
  continueRespondAndRecordRecorder: OperatorFeedbackPageModule["continueRespondAndRecordRecorder"]
  editRespondAndRecordInternalAction: OperatorFeedbackPageModule["editRespondAndRecordInternalAction"]
  setRespondAndRecordChannel: OperatorFeedbackPageModule["setRespondAndRecordChannel"]
  setRespondAndRecordPurpose: OperatorFeedbackPageModule["setRespondAndRecordPurpose"]
  setRespondAndRecordTone: OperatorFeedbackPageModule["setRespondAndRecordTone"]
  setRespondAndRecordIncludeNotes: OperatorFeedbackPageModule["setRespondAndRecordIncludeNotes"]
  continueRespondAndRecordSetup: OperatorFeedbackPageModule["continueRespondAndRecordSetup"]
  writeRespondAndRecordManually: OperatorFeedbackPageModule["writeRespondAndRecordManually"]
  prepareRespondAndRecordDraft: OperatorFeedbackPageModule["prepareRespondAndRecordDraft"]
  rewriteRespondAndRecordDraft: OperatorFeedbackPageModule["rewriteRespondAndRecordDraft"]
  retryRespondAndRecordAiDraft: OperatorFeedbackPageModule["retryRespondAndRecordAiDraft"]
  dismissRespondAndRecordPreparingOverlay: OperatorFeedbackPageModule["dismissRespondAndRecordPreparingOverlay"]
  setRespondAndRecordSubject: OperatorFeedbackPageModule["setRespondAndRecordSubject"]
  setRespondAndRecordMessage: OperatorFeedbackPageModule["setRespondAndRecordMessage"]
  continueRespondAndRecordWrite: OperatorFeedbackPageModule["continueRespondAndRecordWrite"]
  editRespondAndRecordText: OperatorFeedbackPageModule["editRespondAndRecordText"]
  openRespondAndRecordSendConfirm: OperatorFeedbackPageModule["openRespondAndRecordSendConfirm"]
  cancelRespondAndRecordSendConfirm: OperatorFeedbackPageModule["cancelRespondAndRecordSendConfirm"]
  confirmRespondAndRecordSend: OperatorFeedbackPageModule["confirmRespondAndRecordSend"]
  keepRespondAndRecordInProgress: OperatorFeedbackPageModule["keepRespondAndRecordInProgress"]
  markRespondAndRecordResolved: OperatorFeedbackPageModule["markRespondAndRecordResolved"]
  saveAndExitRespondWithRecoveryOffer: OperatorFeedbackPageModule["saveAndExitRespondWithRecoveryOffer"]
  closeRespondWithRecoveryOffer: OperatorFeedbackPageModule["closeRespondWithRecoveryOffer"]
  backRespondWithRecoveryOffer: OperatorFeedbackPageModule["backRespondWithRecoveryOffer"]
  setRespondWithRecoveryOfferChannel: OperatorFeedbackPageModule["setRespondWithRecoveryOfferChannel"]
  setRespondWithRecoveryOfferTone: OperatorFeedbackPageModule["setRespondWithRecoveryOfferTone"]
  setRespondWithRecoveryOfferIncludeNotes: OperatorFeedbackPageModule["setRespondWithRecoveryOfferIncludeNotes"]
  continueRespondWithRecoveryOfferSetup: OperatorFeedbackPageModule["continueRespondWithRecoveryOfferSetup"]
  setRespondWithRecoveryOfferType: OperatorFeedbackPageModule["setRespondWithRecoveryOfferType"]
  setRespondWithRecoveryOfferDiscountPercentage: OperatorFeedbackPageModule["setRespondWithRecoveryOfferDiscountPercentage"]
  setRespondWithRecoveryOfferDiscountAmount: OperatorFeedbackPageModule["setRespondWithRecoveryOfferDiscountAmount"]
  setRespondWithRecoveryOfferFreeItemText: OperatorFeedbackPageModule["setRespondWithRecoveryOfferFreeItemText"]
  setRespondWithRecoveryOfferPurchaseRequirement: OperatorFeedbackPageModule["setRespondWithRecoveryOfferPurchaseRequirement"]
  setRespondWithRecoveryOfferMinimumSpend: OperatorFeedbackPageModule["setRespondWithRecoveryOfferMinimumSpend"]
  setRespondWithRecoveryOfferAdditionalExclusions: OperatorFeedbackPageModule["setRespondWithRecoveryOfferAdditionalExclusions"]
  setRespondWithRecoveryOfferReplacementItemText: OperatorFeedbackPageModule["setRespondWithRecoveryOfferReplacementItemText"]
  setRespondWithRecoveryOfferTitle: OperatorFeedbackPageModule["setRespondWithRecoveryOfferTitle"]
  setRespondWithRecoveryOfferDescription: OperatorFeedbackPageModule["setRespondWithRecoveryOfferDescription"]
  setRespondWithRecoveryOfferValidity: OperatorFeedbackPageModule["setRespondWithRecoveryOfferValidity"]
  setRespondWithRecoveryOfferExpiryDate: OperatorFeedbackPageModule["setRespondWithRecoveryOfferExpiryDate"]
  setRespondWithRecoveryOfferStaffInstructions: OperatorFeedbackPageModule["setRespondWithRecoveryOfferStaffInstructions"]
  prepareRespondWithRecoveryOfferDescription: OperatorFeedbackPageModule["prepareRespondWithRecoveryOfferDescription"]
  continueRespondWithRecoveryOfferDetails: OperatorFeedbackPageModule["continueRespondWithRecoveryOfferDetails"]
  editRespondWithRecoveryOffer: OperatorFeedbackPageModule["editRespondWithRecoveryOffer"]
  writeRespondWithRecoveryOfferManually: OperatorFeedbackPageModule["writeRespondWithRecoveryOfferManually"]
  prepareRespondWithRecoveryOfferDraft: OperatorFeedbackPageModule["prepareRespondWithRecoveryOfferDraft"]
  rewriteRespondWithRecoveryOfferDraft: OperatorFeedbackPageModule["rewriteRespondWithRecoveryOfferDraft"]
  retryRespondWithRecoveryOfferAiDraft: OperatorFeedbackPageModule["retryRespondWithRecoveryOfferAiDraft"]
  dismissRespondWithRecoveryOfferPreparingOverlay: OperatorFeedbackPageModule["dismissRespondWithRecoveryOfferPreparingOverlay"]
  setRespondWithRecoveryOfferSubject: OperatorFeedbackPageModule["setRespondWithRecoveryOfferSubject"]
  setRespondWithRecoveryOfferMessage: OperatorFeedbackPageModule["setRespondWithRecoveryOfferMessage"]
  continueRespondWithRecoveryOfferWrite: OperatorFeedbackPageModule["continueRespondWithRecoveryOfferWrite"]
  editRespondWithRecoveryOfferText: OperatorFeedbackPageModule["editRespondWithRecoveryOfferText"]
  openRespondWithRecoveryOfferSendConfirm: OperatorFeedbackPageModule["openRespondWithRecoveryOfferSendConfirm"]
  cancelRespondWithRecoveryOfferSendConfirm: OperatorFeedbackPageModule["cancelRespondWithRecoveryOfferSendConfirm"]
  confirmRespondWithRecoveryOfferSend: OperatorFeedbackPageModule["confirmRespondWithRecoveryOfferSend"]
  keepRespondWithRecoveryOfferInProgress: OperatorFeedbackPageModule["keepRespondWithRecoveryOfferInProgress"]
  markRespondWithRecoveryOfferResolved: OperatorFeedbackPageModule["markRespondWithRecoveryOfferResolved"]
  retryFeedbackDetails: OperatorFeedbackPageModule["retryFeedbackDetails"]
  startClassificationCorrection: OperatorFeedbackPageModule["startClassificationCorrection"]
  setClassificationDraftSentiment: OperatorFeedbackPageModule["setClassificationDraftSentiment"]
  setClassificationDraftReason: OperatorFeedbackPageModule["setClassificationDraftReason"]
  setClassificationDraftNote: OperatorFeedbackPageModule["setClassificationDraftNote"]
  cancelClassificationCorrection: OperatorFeedbackPageModule["cancelClassificationCorrection"]
  saveClassificationCorrection: OperatorFeedbackPageModule["saveClassificationCorrection"]
  setFeedbackWorkflowStatus: OperatorFeedbackPageModule["setFeedbackWorkflowStatus"]
  reopenFeedbackDetails: OperatorFeedbackPageModule["reopenFeedbackDetails"]
  startFeedbackMarkNoActionNeeded: OperatorFeedbackPageModule["startFeedbackMarkNoActionNeeded"]
  startFeedbackMarkResolved: OperatorFeedbackPageModule["startFeedbackMarkResolved"]
  setFeedbackCloseOutReason: OperatorFeedbackPageModule["setFeedbackCloseOutReason"]
  setFeedbackCloseOutNoteDraft: OperatorFeedbackPageModule["setFeedbackCloseOutNoteDraft"]
  setFeedbackCloseOutAcknowledged: OperatorFeedbackPageModule["setFeedbackCloseOutAcknowledged"]
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
    editRespondToGuestText: pageModule.editRespondToGuestText,
    openRespondToGuestGuestPreview: pageModule.openRespondToGuestGuestPreview,
    closeRespondToGuestGuestPreview: pageModule.closeRespondToGuestGuestPreview,
    openRespondToGuestSendConfirm: pageModule.openRespondToGuestSendConfirm,
    cancelRespondToGuestSendConfirm: pageModule.cancelRespondToGuestSendConfirm,
    confirmRespondToGuestSend: pageModule.confirmRespondToGuestSend,
    keepRespondToGuestInProgress: pageModule.keepRespondToGuestInProgress,
    markRespondToGuestResolved: pageModule.markRespondToGuestResolved,
    saveAndExitRecordInternalAction: pageModule.saveAndExitRecordInternalAction,
    closeRecordInternalAction: pageModule.closeRecordInternalAction,
    backRecordInternalAction: pageModule.backRecordInternalAction,
    setRecordInternalActionCategory: pageModule.setRecordInternalActionCategory,
    setRecordInternalActionNote: pageModule.setRecordInternalActionNote,
    continueRecordInternalActionRecorder:
      pageModule.continueRecordInternalActionRecorder,
    openRecordInternalActionConfirm: pageModule.openRecordInternalActionConfirm,
    cancelRecordInternalActionConfirm:
      pageModule.cancelRecordInternalActionConfirm,
    confirmRecordInternalAction: pageModule.confirmRecordInternalAction,
    keepRecordInternalActionInProgress:
      pageModule.keepRecordInternalActionInProgress,
    markRecordInternalActionResolved: pageModule.markRecordInternalActionResolved,
    saveAndExitRespondAndRecord: pageModule.saveAndExitRespondAndRecord,
    closeRespondAndRecord: pageModule.closeRespondAndRecord,
    backRespondAndRecord: pageModule.backRespondAndRecord,
    setRespondAndRecordCategory: pageModule.setRespondAndRecordCategory,
    setRespondAndRecordNote: pageModule.setRespondAndRecordNote,
    setRespondAndRecordUseConfirmedAction:
      pageModule.setRespondAndRecordUseConfirmedAction,
    continueRespondAndRecordRecorder:
      pageModule.continueRespondAndRecordRecorder,
    editRespondAndRecordInternalAction:
      pageModule.editRespondAndRecordInternalAction,
    setRespondAndRecordChannel: pageModule.setRespondAndRecordChannel,
    setRespondAndRecordPurpose: pageModule.setRespondAndRecordPurpose,
    setRespondAndRecordTone: pageModule.setRespondAndRecordTone,
    setRespondAndRecordIncludeNotes: pageModule.setRespondAndRecordIncludeNotes,
    continueRespondAndRecordSetup: pageModule.continueRespondAndRecordSetup,
    writeRespondAndRecordManually: pageModule.writeRespondAndRecordManually,
    prepareRespondAndRecordDraft: pageModule.prepareRespondAndRecordDraft,
    rewriteRespondAndRecordDraft: pageModule.rewriteRespondAndRecordDraft,
    retryRespondAndRecordAiDraft: pageModule.retryRespondAndRecordAiDraft,
    dismissRespondAndRecordPreparingOverlay:
      pageModule.dismissRespondAndRecordPreparingOverlay,
    setRespondAndRecordSubject: pageModule.setRespondAndRecordSubject,
    setRespondAndRecordMessage: pageModule.setRespondAndRecordMessage,
    continueRespondAndRecordWrite: pageModule.continueRespondAndRecordWrite,
    editRespondAndRecordText: pageModule.editRespondAndRecordText,
    openRespondAndRecordSendConfirm: pageModule.openRespondAndRecordSendConfirm,
    cancelRespondAndRecordSendConfirm:
      pageModule.cancelRespondAndRecordSendConfirm,
    confirmRespondAndRecordSend: pageModule.confirmRespondAndRecordSend,
    keepRespondAndRecordInProgress: pageModule.keepRespondAndRecordInProgress,
    markRespondAndRecordResolved: pageModule.markRespondAndRecordResolved,
    saveAndExitRespondWithRecoveryOffer:
      pageModule.saveAndExitRespondWithRecoveryOffer,
    closeRespondWithRecoveryOffer: pageModule.closeRespondWithRecoveryOffer,
    backRespondWithRecoveryOffer: pageModule.backRespondWithRecoveryOffer,
    setRespondWithRecoveryOfferChannel:
      pageModule.setRespondWithRecoveryOfferChannel,
    setRespondWithRecoveryOfferTone: pageModule.setRespondWithRecoveryOfferTone,
    setRespondWithRecoveryOfferIncludeNotes:
      pageModule.setRespondWithRecoveryOfferIncludeNotes,
    continueRespondWithRecoveryOfferSetup:
      pageModule.continueRespondWithRecoveryOfferSetup,
    setRespondWithRecoveryOfferType: pageModule.setRespondWithRecoveryOfferType,
    setRespondWithRecoveryOfferDiscountPercentage:
      pageModule.setRespondWithRecoveryOfferDiscountPercentage,
    setRespondWithRecoveryOfferDiscountAmount:
      pageModule.setRespondWithRecoveryOfferDiscountAmount,
    setRespondWithRecoveryOfferFreeItemText:
      pageModule.setRespondWithRecoveryOfferFreeItemText,
    setRespondWithRecoveryOfferPurchaseRequirement:
      pageModule.setRespondWithRecoveryOfferPurchaseRequirement,
    setRespondWithRecoveryOfferMinimumSpend:
      pageModule.setRespondWithRecoveryOfferMinimumSpend,
    setRespondWithRecoveryOfferAdditionalExclusions:
      pageModule.setRespondWithRecoveryOfferAdditionalExclusions,
    setRespondWithRecoveryOfferReplacementItemText:
      pageModule.setRespondWithRecoveryOfferReplacementItemText,
    setRespondWithRecoveryOfferTitle:
      pageModule.setRespondWithRecoveryOfferTitle,
    setRespondWithRecoveryOfferDescription:
      pageModule.setRespondWithRecoveryOfferDescription,
    setRespondWithRecoveryOfferValidity:
      pageModule.setRespondWithRecoveryOfferValidity,
    setRespondWithRecoveryOfferExpiryDate:
      pageModule.setRespondWithRecoveryOfferExpiryDate,
    setRespondWithRecoveryOfferStaffInstructions:
      pageModule.setRespondWithRecoveryOfferStaffInstructions,
    prepareRespondWithRecoveryOfferDescription:
      pageModule.prepareRespondWithRecoveryOfferDescription,
    continueRespondWithRecoveryOfferDetails:
      pageModule.continueRespondWithRecoveryOfferDetails,
    editRespondWithRecoveryOffer: pageModule.editRespondWithRecoveryOffer,
    writeRespondWithRecoveryOfferManually:
      pageModule.writeRespondWithRecoveryOfferManually,
    prepareRespondWithRecoveryOfferDraft:
      pageModule.prepareRespondWithRecoveryOfferDraft,
    rewriteRespondWithRecoveryOfferDraft:
      pageModule.rewriteRespondWithRecoveryOfferDraft,
    retryRespondWithRecoveryOfferAiDraft:
      pageModule.retryRespondWithRecoveryOfferAiDraft,
    dismissRespondWithRecoveryOfferPreparingOverlay:
      pageModule.dismissRespondWithRecoveryOfferPreparingOverlay,
    setRespondWithRecoveryOfferSubject:
      pageModule.setRespondWithRecoveryOfferSubject,
    setRespondWithRecoveryOfferMessage:
      pageModule.setRespondWithRecoveryOfferMessage,
    continueRespondWithRecoveryOfferWrite:
      pageModule.continueRespondWithRecoveryOfferWrite,
    editRespondWithRecoveryOfferText:
      pageModule.editRespondWithRecoveryOfferText,
    openRespondWithRecoveryOfferSendConfirm:
      pageModule.openRespondWithRecoveryOfferSendConfirm,
    cancelRespondWithRecoveryOfferSendConfirm:
      pageModule.cancelRespondWithRecoveryOfferSendConfirm,
    confirmRespondWithRecoveryOfferSend:
      pageModule.confirmRespondWithRecoveryOfferSend,
    keepRespondWithRecoveryOfferInProgress:
      pageModule.keepRespondWithRecoveryOfferInProgress,
    markRespondWithRecoveryOfferResolved:
      pageModule.markRespondWithRecoveryOfferResolved,
    retryFeedbackDetails: pageModule.retryFeedbackDetails,
    startClassificationCorrection: pageModule.startClassificationCorrection,
    setClassificationDraftSentiment: pageModule.setClassificationDraftSentiment,
    setClassificationDraftReason: pageModule.setClassificationDraftReason,
    setClassificationDraftNote: pageModule.setClassificationDraftNote,
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
    setFeedbackWorkflowStatus: pageModule.setFeedbackWorkflowStatus,
    reopenFeedbackDetails: pageModule.reopenFeedbackDetails,
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
  }
}

export type { OperatorFeedbackPageSnapshot }
