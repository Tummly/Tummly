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
import { guestFeedbacksFilterSheetSchema } from "@/lib/operatorGuestProfile/guestFeedbacksFilterSheetSchema"
import {
  buildGuestFeedbacksListQueryParams,
  GUEST_FEEDBACKS_PAGE_SIZE,
  type GuestFeedbacksListQueryParams,
} from "@/lib/operatorGuestProfile/guestFeedbacksListQueryParams"
import { mapGuestFeedbacksApiResponseToViewModel } from "@/lib/operatorGuestProfile/mapGuestFeedbacksApiResponseToViewModel"
import { OPERATOR_GUEST_FEEDBACKS_DEFAULT_SORT_ID } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import type { GuestFeedbacksListResponse } from "@/types/dashboard"
import type {
  OperatorGuestFeedbacksSortId,
  OperatorGuestFeedbacksViewModel,
} from "@/types/operatorGuestProfile"

const DEFAULT_SEARCH_DEBOUNCE_MS = 300
const FEEDBACKS_SCHEMA = guestFeedbacksFilterSheetSchema()

export type GuestFeedbacksTabWorkspaceInput = GuestProfileFilteredListWorkspace

export type GuestFeedbacksTabSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorGuestFeedbacksViewModel | null
  searchQuery: string
  sortId: OperatorGuestFeedbacksSortId
  filterChips: FilterChip[]
  filterChipCount: number
  filtersSession: FilterSheetSession | null
}

export type GuestFeedbacksTabAdapters = {
  getGuestFeedbacks: (
    params: GuestFeedbacksListQueryParams
  ) => Promise<GuestFeedbacksListResponse>
  debounceMs?: number
  getNow?: () => Date
}

export type GuestFeedbacksTabModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => GuestFeedbacksTabSnapshot
  syncWorkspace: (input: GuestFeedbacksTabWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  /** Clears completed-fetch markers and reloads when the tab workspace is active. */
  invalidate: () => Promise<void>
  setSearchQuery: (query: string) => void
  setSortId: (sortId: OperatorGuestFeedbacksSortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: FilterSheetSession) => void
  applyFilters: (filters: OperatorFilterSelection) => void
  removeFilterChip: (chip: FilterChip) => void
  clearSearchAndFilters: () => void
}

function hasActiveFeedbacksQuery(
  searchQuery: string,
  filters: OperatorFilterSelection
): boolean {
  if (searchQuery.trim().length > 0) {
    return true
  }
  return chipCount(FEEDBACKS_SCHEMA, filters) > 0
}

export function createGuestFeedbacksTabModule(
  adapters: GuestFeedbacksTabAdapters
): GuestFeedbacksTabModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS
  const getNow = adapters.getNow ?? (() => new Date())
  let searchQuery = ""
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null
  const listeners = new Set<() => void>()

  const clearSearchDebounce = () => {
    if (searchDebounceTimer != null) {
      clearTimeout(searchDebounceTimer)
      searchDebounceTimer = null
    }
  }

  const kernel = createGuestProfileFilteredListKernel<
    OperatorGuestFeedbacksViewModel,
    OperatorGuestFeedbacksSortId,
    OperatorFilterSelection,
    FilterSheetSession
  >({
    defaultSortId: OPERATOR_GUEST_FEEDBACKS_DEFAULT_SORT_ID,
    emptyFilters: () => emptySelection(FEEDBACKS_SCHEMA),
    async load({ guestId, locationId, sortId, page, filters }) {
      const query = searchQuery
      const response = await adapters.getGuestFeedbacks(
        buildGuestFeedbacksListQueryParams({
          guestId,
          locationId,
          q: query,
          sort: sortId,
          page,
          pageSize: GUEST_FEEDBACKS_PAGE_SIZE,
          filters,
          now: getNow(),
        })
      )
      return mapGuestFeedbacksApiResponseToViewModel({
        response,
        sortId,
        hasActiveQuery: hasActiveFeedbacksQuery(query, filters),
      })
    },
  })

  const projectSnapshot = (): GuestFeedbacksTabSnapshot => {
    const core = kernel.getCoreSnapshot()
    return {
      loadStatus: core.loadStatus,
      viewModel: core.viewModel,
      searchQuery,
      sortId: core.sortId,
      filterChips: projectChips(FEEDBACKS_SCHEMA, core.appliedFilters),
      filterChipCount: chipCount(FEEDBACKS_SCHEMA, core.appliedFilters),
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
    async syncWorkspace(input) {
      const priorWorkspace = kernel.getCoreSnapshot().workspace
      const shouldResetSearch =
        input.guestId == null ||
        input.selectedLocationId == null ||
        priorWorkspace?.guestId !== input.guestId ||
        priorWorkspace?.selectedLocationId !== input.selectedLocationId
      if (shouldResetSearch) {
        clearSearchDebounce()
        searchQuery = ""
      }
      await kernel.syncWorkspace(input)
    },
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
    setSearchQuery(query) {
      const core = kernel.getCoreSnapshot()
      if (core.loadStatus === "loaded" && !core.viewModel?.toolbarEnabled) {
        return
      }
      searchQuery = query
      clearSearchDebounce()
      kernel.resetPage()
      publish()
      searchDebounceTimer = setTimeout(() => {
        searchDebounceTimer = null
        void kernel.reload()
      }, debounceMs)
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
        removeAppliedChip(FEEDBACKS_SCHEMA, appliedFilters, chip)
      )
    },
    clearSearchAndFilters() {
      clearSearchDebounce()
      searchQuery = ""
      kernel.clearFilters()
    },
  }
}
