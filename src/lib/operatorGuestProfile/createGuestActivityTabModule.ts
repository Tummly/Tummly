import {
  createGuestProfileFilteredListKernel,
  type GuestProfileFilteredListWorkspace,
} from "@/lib/operatorGuestProfile/createGuestProfileFilteredListKernel"
import {
  activityFilterChipCount,
  emptyActivitySelection,
  hasActiveActivityFilters,
  openActivityFiltersSession,
  projectActivityFilterChips,
  removeActivityFilterChip,
  type ActivityFilterChip,
  type ActivityFiltersPanelSession,
  type GuestActivityFilterSelection,
} from "@/lib/operatorGuestProfile/guestActivityFilterSelection"
import {
  buildGuestActivityListQueryParams,
  GUEST_ACTIVITY_PAGE_SIZE,
  type GuestActivityListQueryParams,
} from "@/lib/operatorGuestProfile/guestActivityListQueryParams"
import { mapGuestActivityApiResponseToViewModel } from "@/lib/operatorGuestProfile/mapGuestActivityApiResponseToViewModel"
import { OPERATOR_GUEST_ACTIVITY_DEFAULT_SORT_ID } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import type { GuestActivityListResponse } from "@/types/dashboard"
import type {
  OperatorGuestActivitySortId,
  OperatorGuestActivityViewModel,
} from "@/types/operatorGuestProfile"

export type GuestActivityTabWorkspaceInput = GuestProfileFilteredListWorkspace

export type GuestActivityTabSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorGuestActivityViewModel | null
  sortId: OperatorGuestActivitySortId
  filterChips: ActivityFilterChip[]
  filterChipCount: number
  filtersSession: ActivityFiltersPanelSession | null
}

export type GuestActivityTabAdapters = {
  getGuestActivity: (
    params: GuestActivityListQueryParams
  ) => Promise<GuestActivityListResponse>
  getNow?: () => Date
}

export type GuestActivityTabModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => GuestActivityTabSnapshot
  syncWorkspace: (input: GuestActivityTabWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setSortId: (sortId: OperatorGuestActivitySortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: ActivityFiltersPanelSession) => void
  applyFilters: (filters: GuestActivityFilterSelection) => void
  removeFilterChip: (chip: ActivityFilterChip) => void
  clearFilters: () => void
}

export function createGuestActivityTabModule(
  adapters: GuestActivityTabAdapters
): GuestActivityTabModule {
  const getNow = adapters.getNow ?? (() => new Date())
  const kernel = createGuestProfileFilteredListKernel<
    OperatorGuestActivityViewModel,
    OperatorGuestActivitySortId,
    GuestActivityFilterSelection,
    ActivityFiltersPanelSession
  >({
    defaultSortId: OPERATOR_GUEST_ACTIVITY_DEFAULT_SORT_ID,
    emptyFilters: emptyActivitySelection,
    async load({ guestId, locationId, sortId, page, filters }) {
      const response = await adapters.getGuestActivity(
        buildGuestActivityListQueryParams({
          guestId,
          locationId,
          sort: sortId,
          page,
          pageSize: GUEST_ACTIVITY_PAGE_SIZE,
          filters,
          now: getNow(),
        })
      )
      return mapGuestActivityApiResponseToViewModel({
        response,
        sortId,
        hasActiveFilters: hasActiveActivityFilters(filters),
      })
    },
  })

  const listeners = new Set<() => void>()

  const projectSnapshot = (): GuestActivityTabSnapshot => {
    const core = kernel.getCoreSnapshot()
    return {
      loadStatus: core.loadStatus,
      viewModel: core.viewModel,
      sortId: core.sortId,
      filterChips: projectActivityFilterChips(core.appliedFilters),
      filterChipCount: activityFilterChipCount(core.appliedFilters),
      filtersSession: core.filtersSession,
    }
  }

  let snapshot = projectSnapshot()

  const publish = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  kernel.subscribe(publish)

  return {
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot() {
      return snapshot
    },
    syncWorkspace: kernel.syncWorkspace,
    async retryLoad() {
      const { workspace } = kernel.getCoreSnapshot()
      if (
        workspace == null ||
        !workspace.active ||
        workspace.guestId == null ||
        workspace.selectedLocationId == null
      ) {
        return
      }
      await kernel.reload()
    },
    setSortId: kernel.setSortId,
    goToPreviousPage: kernel.goToPreviousPage,
    goToNextPage: kernel.goToNextPage,
    openFilters() {
      const { appliedFilters } = kernel.getCoreSnapshot()
      kernel.openFiltersSession(openActivityFiltersSession(appliedFilters))
    },
    closeFilters: kernel.closeFilters,
    setFiltersSession: kernel.setFiltersSession,
    applyFilters: kernel.applyFilters,
    removeFilterChip(chip) {
      const { appliedFilters } = kernel.getCoreSnapshot()
      kernel.replaceFilters(removeActivityFilterChip(appliedFilters, chip))
    },
    clearFilters: kernel.clearFilters,
  }
}
