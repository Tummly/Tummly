import { useSyncExternalStore } from "react"

import { useGuestsPageModuleApi } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import type {
  OperatorGuestsPageModule,
  OperatorGuestsPageSnapshot,
} from "@/lib/operatorGuests/createOperatorGuestsPageModule"

export type OperatorGuestsPageModuleApi = {
  snapshot: OperatorGuestsPageSnapshot
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
  openAddTag: OperatorGuestsPageModule["openAddTag"]
  closeAddTag: OperatorGuestsPageModule["closeAddTag"]
  stageAddTag: OperatorGuestsPageModule["stageAddTag"]
  unstageAddTag: OperatorGuestsPageModule["unstageAddTag"]
  setAddTagSearch: OperatorGuestsPageModule["setAddTagSearch"]
  setAddTagCreateOpen: OperatorGuestsPageModule["setAddTagCreateOpen"]
  setAddTagCreateName: OperatorGuestsPageModule["setAddTagCreateName"]
  createAndStageAddTag: OperatorGuestsPageModule["createAndStageAddTag"]
  applyAddTag: OperatorGuestsPageModule["applyAddTag"]
}

export function useGuestsPageModule(): OperatorGuestsPageModuleApi {
  const pageModule = useGuestsPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
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
    openAddTag: pageModule.openAddTag,
    closeAddTag: pageModule.closeAddTag,
    stageAddTag: pageModule.stageAddTag,
    unstageAddTag: pageModule.unstageAddTag,
    setAddTagSearch: pageModule.setAddTagSearch,
    setAddTagCreateOpen: pageModule.setAddTagCreateOpen,
    setAddTagCreateName: pageModule.setAddTagCreateName,
    createAndStageAddTag: pageModule.createAndStageAddTag,
    applyAddTag: pageModule.applyAddTag,
  }
}
