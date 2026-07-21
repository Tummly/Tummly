import { useRef, useSyncExternalStore } from "react"

import {
  createOperatorGuestsPageModule,
  type OperatorGuestsPageModule,
  type OperatorGuestsPageSnapshot,
} from "@/lib/operatorGuests/createOperatorGuestsPageModule"

export type OperatorGuestsPageModuleApi = {
  snapshot: OperatorGuestsPageSnapshot
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
  const moduleRef = useRef<OperatorGuestsPageModule | null>(null)

  if (moduleRef.current == null) {
    moduleRef.current = createOperatorGuestsPageModule()
  }

  const pageModule = moduleRef.current
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
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
