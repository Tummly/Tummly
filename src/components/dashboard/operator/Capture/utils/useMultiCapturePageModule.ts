import { useSyncExternalStore } from "react"

import { useMultiCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/multiCapturePageModuleContext"
import type {
  OperatorMultiCapturePageModule,
  OperatorMultiCapturePageSnapshot,
} from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"

export type OperatorMultiCapturePageModuleApi = {
  snapshot: OperatorMultiCapturePageSnapshot
  syncWorkspace: OperatorMultiCapturePageModule["syncWorkspace"]
  retryLoad: OperatorMultiCapturePageModule["retryLoad"]
  reloadForMultiCaptureOverviewDateRange: OperatorMultiCapturePageModule["reloadForMultiCaptureOverviewDateRange"]
  navigateToLocationCapture: OperatorMultiCapturePageModule["navigateToLocationCapture"]
  getLocationRowActions: OperatorMultiCapturePageModule["getLocationRowActions"]
  openCreateDialog: OperatorMultiCapturePageModule["openCreateDialog"]
  closeCreateDialog: OperatorMultiCapturePageModule["closeCreateDialog"]
  setCreateDialogLocationId: OperatorMultiCapturePageModule["setCreateDialogLocationId"]
  createDigitalGuestLink: OperatorMultiCapturePageModule["createDigitalGuestLink"]
  openLocationPreview: OperatorMultiCapturePageModule["openLocationPreview"]
  closeGuestExperiencePreview: OperatorMultiCapturePageModule["closeGuestExperiencePreview"]
  closeGuestExperiencePreviewPicker: OperatorMultiCapturePageModule["closeGuestExperiencePreviewPicker"]
  selectGuestExperiencePreviewPickerOption: OperatorMultiCapturePageModule["selectGuestExperiencePreviewPickerOption"]
  confirmGuestExperiencePreviewPicker: OperatorMultiCapturePageModule["confirmGuestExperiencePreviewPicker"]
  requestPauseLocationCapture: OperatorMultiCapturePageModule["requestPauseLocationCapture"]
  requestActivateLocationCapture: OperatorMultiCapturePageModule["requestActivateLocationCapture"]
  cancelLocationCaptureConfirm: OperatorMultiCapturePageModule["cancelLocationCaptureConfirm"]
  confirmLocationCaptureStub: OperatorMultiCapturePageModule["confirmLocationCaptureStub"]
  setSearchQuery: OperatorMultiCapturePageModule["setSearchQuery"]
  setSortId: OperatorMultiCapturePageModule["setSortId"]
  setPage: OperatorMultiCapturePageModule["setPage"]
  goToPreviousPage: OperatorMultiCapturePageModule["goToPreviousPage"]
  goToNextPage: OperatorMultiCapturePageModule["goToNextPage"]
  clearSearchAndFilters: OperatorMultiCapturePageModule["clearSearchAndFilters"]
  applyFilters: OperatorMultiCapturePageModule["applyFilters"]
  removeFilterChip: OperatorMultiCapturePageModule["removeFilterChip"]
  openFilters: OperatorMultiCapturePageModule["openFilters"]
  closeFilters: OperatorMultiCapturePageModule["closeFilters"]
  setFiltersSession: OperatorMultiCapturePageModule["setFiltersSession"]
}

export function useMultiCapturePageModule(): OperatorMultiCapturePageModuleApi {
  const pageModule = useMultiCapturePageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    syncWorkspace: pageModule.syncWorkspace,
    retryLoad: pageModule.retryLoad,
    reloadForMultiCaptureOverviewDateRange:
      pageModule.reloadForMultiCaptureOverviewDateRange,
    navigateToLocationCapture: pageModule.navigateToLocationCapture,
    getLocationRowActions: pageModule.getLocationRowActions,
    openCreateDialog: pageModule.openCreateDialog,
    closeCreateDialog: pageModule.closeCreateDialog,
    setCreateDialogLocationId: pageModule.setCreateDialogLocationId,
    createDigitalGuestLink: pageModule.createDigitalGuestLink,
    openLocationPreview: pageModule.openLocationPreview,
    closeGuestExperiencePreview: pageModule.closeGuestExperiencePreview,
    closeGuestExperiencePreviewPicker:
      pageModule.closeGuestExperiencePreviewPicker,
    selectGuestExperiencePreviewPickerOption:
      pageModule.selectGuestExperiencePreviewPickerOption,
    confirmGuestExperiencePreviewPicker:
      pageModule.confirmGuestExperiencePreviewPicker,
    requestPauseLocationCapture: pageModule.requestPauseLocationCapture,
    requestActivateLocationCapture: pageModule.requestActivateLocationCapture,
    cancelLocationCaptureConfirm: pageModule.cancelLocationCaptureConfirm,
    confirmLocationCaptureStub: pageModule.confirmLocationCaptureStub,
    setSearchQuery: pageModule.setSearchQuery,
    setSortId: pageModule.setSortId,
    setPage: pageModule.setPage,
    goToPreviousPage: pageModule.goToPreviousPage,
    goToNextPage: pageModule.goToNextPage,
    clearSearchAndFilters: pageModule.clearSearchAndFilters,
    applyFilters: pageModule.applyFilters,
    removeFilterChip: pageModule.removeFilterChip,
    openFilters: pageModule.openFilters,
    closeFilters: pageModule.closeFilters,
    setFiltersSession: pageModule.setFiltersSession,
  }
}
