import { useSyncExternalStore } from "react"

import { useGuestsPageModuleApi } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import type { ManageMarketingPreferencesSnapshot } from "@/lib/operatorGuests/createManageMarketingPreferencesSessionModule"
import type {
  OperatorGuestsPageModule,
  OperatorGuestsPageSnapshot,
} from "@/lib/operatorGuests/createOperatorGuestsPageModule"

export type OperatorGuestsPageModuleApi = {
  snapshot: OperatorGuestsPageSnapshot
  clearTabCache: OperatorGuestsPageModule["clearTabCache"]
  retryLoad: OperatorGuestsPageModule["retryLoad"]
  setActiveSmartGroupId: OperatorGuestsPageModule["setActiveSmartGroupId"]
  setSearchQuery: OperatorGuestsPageModule["setSearchQuery"]
  setSortId: OperatorGuestsPageModule["setSortId"]
  goToPreviousPage: OperatorGuestsPageModule["goToPreviousPage"]
  goToNextPage: OperatorGuestsPageModule["goToNextPage"]
  toggleGuestSelection: OperatorGuestsPageModule["toggleGuestSelection"]
  toggleSelectAllVisibleRows: OperatorGuestsPageModule["toggleSelectAllVisibleRows"]
  clearSelection: OperatorGuestsPageModule["clearSelection"]
  clearSearchAndFilters: OperatorGuestsPageModule["clearSearchAndFilters"]
  applyFilters: OperatorGuestsPageModule["applyFilters"]
  removeFilterChip: OperatorGuestsPageModule["removeFilterChip"]
  openFilters: OperatorGuestsPageModule["openFilters"]
  closeFilters: OperatorGuestsPageModule["closeFilters"]
  setFiltersSession: OperatorGuestsPageModule["setFiltersSession"]
  reloadForOverviewDateRange: OperatorGuestsPageModule["reloadForOverviewDateRange"]
  exportCsv: OperatorGuestsPageModule["exportCsv"]
  exportSelectedCsv: OperatorGuestsPageModule["exportSelectedCsv"]
  exportGuestCsv: OperatorGuestsPageModule["exportGuestCsv"]
  openAddTag: OperatorGuestsPageModule["openAddTag"]
  closeAddTag: OperatorGuestsPageModule["closeAddTag"]
  stageAddTag: OperatorGuestsPageModule["stageAddTag"]
  unstageAddTag: OperatorGuestsPageModule["unstageAddTag"]
  setAddTagSearch: OperatorGuestsPageModule["setAddTagSearch"]
  setAddTagCreateOpen: OperatorGuestsPageModule["setAddTagCreateOpen"]
  setAddTagCreateName: OperatorGuestsPageModule["setAddTagCreateName"]
  createAndStageAddTag: OperatorGuestsPageModule["createAndStageAddTag"]
  applyAddTag: OperatorGuestsPageModule["applyAddTag"]
  openGuestDetails: OperatorGuestsPageModule["openGuestDetails"]
  closeGuestDetails: OperatorGuestsPageModule["closeGuestDetails"]
  retryGuestDetails: OperatorGuestsPageModule["retryGuestDetails"]
  setGuestDetailsNoteDraft: OperatorGuestsPageModule["setGuestDetailsNoteDraft"]
  createGuestDetailsNote: OperatorGuestsPageModule["createGuestDetailsNote"]
  openFeedbackDetails: OperatorGuestsPageModule["openFeedbackDetails"]
  closeFeedbackDetails: OperatorGuestsPageModule["closeFeedbackDetails"]
  retryFeedbackDetails: OperatorGuestsPageModule["retryFeedbackDetails"]
  startClassificationCorrection: OperatorGuestsPageModule["startClassificationCorrection"]
  setClassificationDraftSentiment: OperatorGuestsPageModule["setClassificationDraftSentiment"]
  setClassificationDraftReason: OperatorGuestsPageModule["setClassificationDraftReason"]
  setClassificationDraftNote: OperatorGuestsPageModule["setClassificationDraftNote"]
  cancelClassificationCorrection: OperatorGuestsPageModule["cancelClassificationCorrection"]
  saveClassificationCorrection: OperatorGuestsPageModule["saveClassificationCorrection"]
  startFeedbackEditTags: OperatorGuestsPageModule["startFeedbackEditTags"]
  stageFeedbackEditTag: OperatorGuestsPageModule["stageFeedbackEditTag"]
  unstageFeedbackEditTag: OperatorGuestsPageModule["unstageFeedbackEditTag"]
  setFeedbackEditTagsSentiment: OperatorGuestsPageModule["setFeedbackEditTagsSentiment"]
  cancelFeedbackEditTags: OperatorGuestsPageModule["cancelFeedbackEditTags"]
  applyFeedbackEditTags: OperatorGuestsPageModule["applyFeedbackEditTags"]
  setFeedbackWorkflowStatus: OperatorGuestsPageModule["setFeedbackWorkflowStatus"]
  reopenFeedback: OperatorGuestsPageModule["reopenFeedback"]
  startFeedbackMarkNoActionNeeded: OperatorGuestsPageModule["startFeedbackMarkNoActionNeeded"]
  startFeedbackMarkResolved: OperatorGuestsPageModule["startFeedbackMarkResolved"]
  setFeedbackCloseOutReason: OperatorGuestsPageModule["setFeedbackCloseOutReason"]
  setFeedbackCloseOutNoteDraft: OperatorGuestsPageModule["setFeedbackCloseOutNoteDraft"]
  setFeedbackCloseOutAcknowledged: OperatorGuestsPageModule["setFeedbackCloseOutAcknowledged"]
  cancelFeedbackCloseOut: OperatorGuestsPageModule["cancelFeedbackCloseOut"]
  confirmFeedbackCloseOut: OperatorGuestsPageModule["confirmFeedbackCloseOut"]
  setFeedbackInternalNoteDraft: OperatorGuestsPageModule["setFeedbackInternalNoteDraft"]
  createFeedbackInternalNote: OperatorGuestsPageModule["createFeedbackInternalNote"]
  startFeedbackNoteEdit: OperatorGuestsPageModule["startFeedbackNoteEdit"]
  setFeedbackNoteEditDraft: OperatorGuestsPageModule["setFeedbackNoteEditDraft"]
  cancelFeedbackNoteEdit: OperatorGuestsPageModule["cancelFeedbackNoteEdit"]
  saveFeedbackNoteEdit: OperatorGuestsPageModule["saveFeedbackNoteEdit"]
  startFeedbackNoteDelete: OperatorGuestsPageModule["startFeedbackNoteDelete"]
  cancelFeedbackNoteDelete: OperatorGuestsPageModule["cancelFeedbackNoteDelete"]
  confirmFeedbackNoteDelete: OperatorGuestsPageModule["confirmFeedbackNoteDelete"]
  startRecovery: OperatorGuestsPageModule["startRecovery"]
  closeStartRecovery: OperatorGuestsPageModule["closeStartRecovery"]
  selectStartRecoveryIntent: OperatorGuestsPageModule["selectStartRecoveryIntent"]
  retryStartRecovery: OperatorGuestsPageModule["retryStartRecovery"]
  recoveryWizards: OperatorGuestsPageModule["recoveryWizards"]
  marketingPreferencesSnapshot: ManageMarketingPreferencesSnapshot
  openManageMarketingPreferences: OperatorGuestsPageModule["openManageMarketingPreferences"]
  closeManageMarketingPreferences: OperatorGuestsPageModule["closeManageMarketingPreferences"]
  saveManageMarketingPreferences: OperatorGuestsPageModule["saveManageMarketingPreferences"]
  setMarketingPreferenceDraft: OperatorGuestsPageModule["marketingPreferences"]["setDraftPreference"]
  setMarketingPreferenceNote: OperatorGuestsPageModule["marketingPreferences"]["setDraftNote"]
}

export function useGuestsPageModule(): OperatorGuestsPageModuleApi {
  const pageModule = useGuestsPageModuleApi()
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
    clearTabCache: pageModule.clearTabCache,
    retryLoad: pageModule.retryLoad,
    setActiveSmartGroupId: pageModule.setActiveSmartGroupId,
    setSearchQuery: pageModule.setSearchQuery,
    setSortId: pageModule.setSortId,
    goToPreviousPage: pageModule.goToPreviousPage,
    goToNextPage: pageModule.goToNextPage,
    toggleGuestSelection: pageModule.toggleGuestSelection,
    toggleSelectAllVisibleRows: pageModule.toggleSelectAllVisibleRows,
    clearSelection: pageModule.clearSelection,
    clearSearchAndFilters: pageModule.clearSearchAndFilters,
    applyFilters: pageModule.applyFilters,
    removeFilterChip: pageModule.removeFilterChip,
    openFilters: pageModule.openFilters,
    closeFilters: pageModule.closeFilters,
    setFiltersSession: pageModule.setFiltersSession,
    reloadForOverviewDateRange: pageModule.reloadForOverviewDateRange,
    exportCsv: pageModule.exportCsv,
    exportSelectedCsv: pageModule.exportSelectedCsv,
    exportGuestCsv: pageModule.exportGuestCsv,
    openAddTag: pageModule.openAddTag,
    closeAddTag: pageModule.closeAddTag,
    stageAddTag: pageModule.stageAddTag,
    unstageAddTag: pageModule.unstageAddTag,
    setAddTagSearch: pageModule.setAddTagSearch,
    setAddTagCreateOpen: pageModule.setAddTagCreateOpen,
    setAddTagCreateName: pageModule.setAddTagCreateName,
    createAndStageAddTag: pageModule.createAndStageAddTag,
    applyAddTag: pageModule.applyAddTag,
    openGuestDetails: pageModule.openGuestDetails,
    closeGuestDetails: pageModule.closeGuestDetails,
    retryGuestDetails: pageModule.retryGuestDetails,
    setGuestDetailsNoteDraft: pageModule.setGuestDetailsNoteDraft,
    createGuestDetailsNote: pageModule.createGuestDetailsNote,
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
    marketingPreferencesSnapshot,
    openManageMarketingPreferences: pageModule.openManageMarketingPreferences,
    closeManageMarketingPreferences: pageModule.closeManageMarketingPreferences,
    saveManageMarketingPreferences: pageModule.saveManageMarketingPreferences,
    setMarketingPreferenceDraft:
      pageModule.marketingPreferences.setDraftPreference,
    setMarketingPreferenceNote: pageModule.marketingPreferences.setDraftNote,
  }
}
