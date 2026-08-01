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
  setRowWorkflowStatus: OperatorFeedbackPageModule["setRowWorkflowStatus"]
  reopenFeedback: OperatorFeedbackPageModule["reopenFeedback"]
  markFeedbackNoActionNeeded: OperatorFeedbackPageModule["markFeedbackNoActionNeeded"]
  retryFeedbackDetails: OperatorFeedbackPageModule["retryFeedbackDetails"]
  startClassificationCorrection: OperatorFeedbackPageModule["startClassificationCorrection"]
  setClassificationDraftSentiment: OperatorFeedbackPageModule["setClassificationDraftSentiment"]
  cancelClassificationCorrection: OperatorFeedbackPageModule["cancelClassificationCorrection"]
  saveClassificationCorrection: OperatorFeedbackPageModule["saveClassificationCorrection"]
  setFeedbackWorkflowStatus: OperatorFeedbackPageModule["setFeedbackWorkflowStatus"]
  reopenFeedbackDetails: OperatorFeedbackPageModule["reopenFeedbackDetails"]
  markFeedbackDetailsNoActionNeeded: OperatorFeedbackPageModule["markFeedbackDetailsNoActionNeeded"]
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
    setRowWorkflowStatus: pageModule.setRowWorkflowStatus,
    reopenFeedback: pageModule.reopenFeedback,
    markFeedbackNoActionNeeded: pageModule.markFeedbackNoActionNeeded,
    retryFeedbackDetails: pageModule.retryFeedbackDetails,
    startClassificationCorrection: pageModule.startClassificationCorrection,
    setClassificationDraftSentiment: pageModule.setClassificationDraftSentiment,
    cancelClassificationCorrection: pageModule.cancelClassificationCorrection,
    saveClassificationCorrection: pageModule.saveClassificationCorrection,
    setFeedbackWorkflowStatus: pageModule.setFeedbackWorkflowStatus,
    reopenFeedbackDetails: pageModule.reopenFeedbackDetails,
    markFeedbackDetailsNoActionNeeded: pageModule.markFeedbackDetailsNoActionNeeded,
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
