import { useSyncExternalStore } from "react"

import { useGuestFeedbacksTabModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestFeedbacksTabModuleContext"
import type {
  GuestFeedbacksTabModule,
  GuestFeedbacksTabSnapshot,
} from "@/lib/operatorGuestProfile/createGuestFeedbacksTabModule"

export type GuestFeedbacksTabModuleApi = {
  snapshot: GuestFeedbacksTabSnapshot
  syncWorkspace: GuestFeedbacksTabModule["syncWorkspace"]
  retryLoad: GuestFeedbacksTabModule["retryLoad"]
  setSearchQuery: GuestFeedbacksTabModule["setSearchQuery"]
  setSortId: GuestFeedbacksTabModule["setSortId"]
  goToPreviousPage: GuestFeedbacksTabModule["goToPreviousPage"]
  goToNextPage: GuestFeedbacksTabModule["goToNextPage"]
  openFilters: GuestFeedbacksTabModule["openFilters"]
  closeFilters: GuestFeedbacksTabModule["closeFilters"]
  setFiltersSession: GuestFeedbacksTabModule["setFiltersSession"]
  applyFilters: GuestFeedbacksTabModule["applyFilters"]
  removeFilterChip: GuestFeedbacksTabModule["removeFilterChip"]
  clearSearchAndFilters: GuestFeedbacksTabModule["clearSearchAndFilters"]
}

export function useGuestFeedbacksTabModule(): GuestFeedbacksTabModuleApi {
  const tabModule = useGuestFeedbacksTabModuleApi()
  const snapshot = useSyncExternalStore(
    tabModule.subscribe,
    tabModule.getSnapshot,
    tabModule.getSnapshot
  )

  return {
    snapshot,
    syncWorkspace: tabModule.syncWorkspace,
    retryLoad: tabModule.retryLoad,
    setSearchQuery: tabModule.setSearchQuery,
    setSortId: tabModule.setSortId,
    goToPreviousPage: tabModule.goToPreviousPage,
    goToNextPage: tabModule.goToNextPage,
    openFilters: tabModule.openFilters,
    closeFilters: tabModule.closeFilters,
    setFiltersSession: tabModule.setFiltersSession,
    applyFilters: tabModule.applyFilters,
    removeFilterChip: tabModule.removeFilterChip,
    clearSearchAndFilters: tabModule.clearSearchAndFilters,
  }
}
