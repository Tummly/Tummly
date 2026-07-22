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
  }
}
