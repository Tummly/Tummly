import {
  createGuestProfileFilteredListKernel,
  type GuestProfileFilteredListWorkspace,
} from "@/lib/operatorGuestProfile/createGuestProfileFilteredListKernel"
import {
  chipCount,
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { guestActivityFilterSheetSchema } from "@/lib/operatorGuestProfile/guestActivityFilterSheetSchema"
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

const ACTIVITY_SCHEMA = guestActivityFilterSheetSchema()

export type GuestActivityTabWorkspaceInput = GuestProfileFilteredListWorkspace

export type GuestActivityTabSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorGuestActivityViewModel | null
  sortId: OperatorGuestActivitySortId
  filterChips: FilterChip[]
  filterChipCount: number
  filtersSession: FilterSheetSession | null
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
  /** Clears completed-fetch markers and reloads when the tab workspace is active. */
  invalidate: () => Promise<void>
  setSortId: (sortId: OperatorGuestActivitySortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: FilterSheetSession) => void
  applyFilters: (filters: OperatorFilterSelection) => void
  removeFilterChip: (chip: FilterChip) => void
  clearFilters: () => void
}

export function createGuestActivityTabModule(
  adapters: GuestActivityTabAdapters
): GuestActivityTabModule {
  const getNow = adapters.getNow ?? (() => new Date())
  const kernel = createGuestProfileFilteredListKernel<
    OperatorGuestActivityViewModel,
    OperatorGuestActivitySortId,
    OperatorFilterSelection,
    FilterSheetSession
  >({
    defaultSortId: OPERATOR_GUEST_ACTIVITY_DEFAULT_SORT_ID,
    emptyFilters: () => emptySelection(ACTIVITY_SCHEMA),
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
        hasActiveFilters: chipCount(ACTIVITY_SCHEMA, filters) > 0,
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
      filterChips: projectChips(ACTIVITY_SCHEMA, core.appliedFilters),
      filterChipCount: chipCount(ACTIVITY_SCHEMA, core.appliedFilters),
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
    invalidate() {
      return kernel.reload()
    },
    setSortId: kernel.setSortId,
    goToPreviousPage: kernel.goToPreviousPage,
    goToNextPage: kernel.goToNextPage,
    openFilters() {
      const { appliedFilters } = kernel.getCoreSnapshot()
      kernel.openFiltersSession(openSession(appliedFilters))
    },
    closeFilters: kernel.closeFilters,
    setFiltersSession: kernel.setFiltersSession,
    applyFilters(filters) {
      // Keep sheet open on Apply (ADR-0017); replaceFilters does not null session.
      const keepOpen = kernel.getCoreSnapshot().filtersSession != null
      kernel.replaceFilters(filters)
      if (keepOpen) {
        kernel.setFiltersSession(openSession(filters))
      }
    },
    removeFilterChip(chip) {
      const { appliedFilters } = kernel.getCoreSnapshot()
      kernel.replaceFilters(
        removeAppliedChip(ACTIVITY_SCHEMA, appliedFilters, chip)
      )
    },
    clearFilters: kernel.clearFilters,
  }
}
