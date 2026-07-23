import { useSyncExternalStore } from "react"

import { useGuestActivityTabModuleApi } from "@/components/dashboard/operator/GuestProfile/utils/guestActivityTabModuleContext"
import type {
  GuestActivityTabModule,
  GuestActivityTabSnapshot,
} from "@/lib/operatorGuestProfile/createGuestActivityTabModule"

export type GuestActivityTabModuleApi = {
  snapshot: GuestActivityTabSnapshot
  syncWorkspace: GuestActivityTabModule["syncWorkspace"]
  retryLoad: GuestActivityTabModule["retryLoad"]
  setSortId: GuestActivityTabModule["setSortId"]
  goToPreviousPage: GuestActivityTabModule["goToPreviousPage"]
  goToNextPage: GuestActivityTabModule["goToNextPage"]
  openFilters: GuestActivityTabModule["openFilters"]
  closeFilters: GuestActivityTabModule["closeFilters"]
  setFiltersSession: GuestActivityTabModule["setFiltersSession"]
  applyFilters: GuestActivityTabModule["applyFilters"]
  removeFilterChip: GuestActivityTabModule["removeFilterChip"]
  clearFilters: GuestActivityTabModule["clearFilters"]
}

export function useGuestActivityTabModule(): GuestActivityTabModuleApi {
  const tabModule = useGuestActivityTabModuleApi()
  const snapshot = useSyncExternalStore(
    tabModule.subscribe,
    tabModule.getSnapshot,
    tabModule.getSnapshot
  )

  return {
    snapshot,
    syncWorkspace: tabModule.syncWorkspace,
    retryLoad: tabModule.retryLoad,
    setSortId: tabModule.setSortId,
    goToPreviousPage: tabModule.goToPreviousPage,
    goToNextPage: tabModule.goToNextPage,
    openFilters: tabModule.openFilters,
    closeFilters: tabModule.closeFilters,
    setFiltersSession: tabModule.setFiltersSession,
    applyFilters: tabModule.applyFilters,
    removeFilterChip: tabModule.removeFilterChip,
    clearFilters: tabModule.clearFilters,
  }
}
