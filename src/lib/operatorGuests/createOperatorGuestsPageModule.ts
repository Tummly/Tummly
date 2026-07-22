import { mapGuestsApiResponseToViewModel } from "@/lib/operatorGuests/mapGuestsApiResponseToViewModel"
import { OPERATOR_GUEST_DEFAULT_SORT_ID } from "@/lib/operatorGuests/guestsPresentation"
import {
  computeVisibleSelectionState,
  formatGuestSelectionLabel,
  sortedSelectionIds,
  toggleAllVisibleInSelection,
  toggleGuestInSelection,
} from "@/lib/operatorGuests/guestSelection"
import type { GuestsResponse } from "@/types/dashboard"
import type {
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestsViewModel,
} from "@/types/operatorGuests"

export type OperatorGuestsWorkspaceInput = {
  selectedLocationId: number | null
}

export type OperatorGuestsPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorGuestsViewModel | null
  searchQuery: string
  sortId: OperatorGuestSortId
  selectedGuestIds: readonly string[]
  selectedCount: number
  bulkSelectionLabel: string | null
  isAllVisibleSelected: boolean
  isSomeVisibleSelected: boolean
  isGuestSelected: (guestId: string) => boolean
}

export type OperatorGuestsPageAdapters = {
  getGuests: (params: {
    locationId: number
    smartGroup: OperatorGuestSmartGroupId
    q: string
    sort: OperatorGuestSortId
    page: number
    pageSize: number
  }) => Promise<GuestsResponse>
  debounceMs?: number
}

export type OperatorGuestsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorGuestsPageSnapshot
  syncWorkspace: (input: OperatorGuestsWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setActiveSmartGroupId: (id: OperatorGuestSmartGroupId) => void
  setSearchQuery: (query: string) => void
  setSortId: (id: OperatorGuestSortId) => void
  setPage: (page: number) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  toggleGuestSelection: (guestId: string) => void
  toggleSelectAllVisibleRows: () => void
  clearSelection: () => void
  clearSearchAndFilters: () => void
}

type ModuleState = {
  loadStatus: OperatorGuestsPageSnapshot["loadStatus"]
  viewModel: OperatorGuestsViewModel | null
  workspace: OperatorGuestsWorkspaceInput | null
  activeSmartGroupId: OperatorGuestSmartGroupId
  searchQuery: string
  sortId: OperatorGuestSortId
  page: number
  loadGeneration: number
}

const DEFAULT_PAGE_SIZE = 25
const DEFAULT_SEARCH_DEBOUNCE_MS = 300

function selectionSetsEqual(
  left: ReadonlySet<string>,
  right: ReadonlySet<string>
): boolean {
  if (left.size !== right.size) {
    return false
  }

  for (const id of left) {
    if (!right.has(id)) {
      return false
    }
  }

  return true
}

function buildSnapshot(
  state: ModuleState,
  selectedGuestIds: ReadonlySet<string>
): OperatorGuestsPageSnapshot {
  const visibleGuestIds =
    state.viewModel?.tableRows.map((row) => row.id) ?? []
  const visibleSelection = computeVisibleSelectionState(
    selectedGuestIds,
    visibleGuestIds
  )
  const selectedCount = selectedGuestIds.size

  return {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    searchQuery: state.searchQuery,
    sortId: state.sortId,
    selectedGuestIds: sortedSelectionIds(selectedGuestIds),
    selectedCount,
    bulkSelectionLabel: formatGuestSelectionLabel(selectedCount),
    isAllVisibleSelected: visibleSelection.isAllVisibleSelected,
    isSomeVisibleSelected: visibleSelection.isSomeVisibleSelected,
    isGuestSelected(guestId: string) {
      return selectedGuestIds.has(guestId)
    },
  }
}

export function createOperatorGuestsPageModule(
  adapters: OperatorGuestsPageAdapters
): OperatorGuestsPageModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS

  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    activeSmartGroupId: "all-guests",
    searchQuery: "",
    sortId: OPERATOR_GUEST_DEFAULT_SORT_ID,
    page: 1,
    loadGeneration: 0,
  }
  let selectedGuestIds = new Set<string>()
  let snapshot = buildSnapshot(state, selectedGuestIds)
  const listeners = new Set<() => void>()
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

  const publish = () => {
    snapshot = buildSnapshot(state, selectedGuestIds)
    for (const listener of listeners) {
      listener()
    }
  }

  const clearSearchDebounce = () => {
    if (searchDebounceTimer != null) {
      clearTimeout(searchDebounceTimer)
      searchDebounceTimer = null
    }
  }

  const fetchGuests = async (options?: { quiet?: boolean }) => {
    const selectedLocationId = state.workspace?.selectedLocationId
    if (selectedLocationId == null) {
      return
    }

    const generation = state.loadGeneration + 1
    const isQuiet = options?.quiet === true && state.viewModel != null

    state = {
      ...state,
      loadStatus: isQuiet ? state.loadStatus : "loading",
      loadGeneration: generation,
    }
    publish()

    try {
      const response = await adapters.getGuests({
        locationId: selectedLocationId,
        smartGroup: state.activeSmartGroupId,
        q: state.searchQuery,
        sort: state.sortId,
        page: state.page,
        pageSize: DEFAULT_PAGE_SIZE,
      })

      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: mapGuestsApiResponseToViewModel({
          response,
          activeSmartGroupId: state.activeSmartGroupId,
          sortId: state.sortId,
        }),
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "error",
      }
      publish()
    }
  }

  const scheduleSearchFetch = () => {
    clearSearchDebounce()
    searchDebounceTimer = setTimeout(() => {
      searchDebounceTimer = null
      void fetchGuests()
    }, debounceMs)
  }

  const clearSelectionIfNeeded = () => {
    if (selectedGuestIds.size === 0) {
      return
    }
    selectedGuestIds = new Set()
  }

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
    syncWorkspace: async (input) => {
      if (input.selectedLocationId == null) {
        clearSearchDebounce()
        state = {
          loadStatus: "idle",
          viewModel: null,
          workspace: null,
          activeSmartGroupId: "all-guests",
          searchQuery: "",
          sortId: OPERATOR_GUEST_DEFAULT_SORT_ID,
          page: 1,
          loadGeneration: state.loadGeneration,
        }
        selectedGuestIds = new Set()
        publish()
        return
      }

      const previousLocationId = state.workspace?.selectedLocationId ?? null
      const locationChanged = previousLocationId !== input.selectedLocationId

      state = {
        ...state,
        workspace: input,
      }

      if (locationChanged) {
        clearSearchDebounce()
        clearSelectionIfNeeded()
        state = {
          ...state,
          activeSmartGroupId: "all-guests",
          searchQuery: "",
          sortId: OPERATOR_GUEST_DEFAULT_SORT_ID,
          page: 1,
        }
        await fetchGuests()
        return
      }

      publish()
    },
    retryLoad: () => fetchGuests(),
    setActiveSmartGroupId(id) {
      if (state.activeSmartGroupId === id) {
        return
      }

      clearSearchDebounce()
      clearSelectionIfNeeded()
      state = {
        ...state,
        activeSmartGroupId: id,
        page: 1,
      }
      void fetchGuests({ quiet: true })
    },
    setSearchQuery(query) {
      if (state.searchQuery === query) {
        return
      }

      clearSelectionIfNeeded()
      state = {
        ...state,
        searchQuery: query,
        page: 1,
      }
      publish()
      scheduleSearchFetch()
    },
    setSortId(id) {
      if (state.sortId === id) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        sortId: id,
        page: 1,
      }
      void fetchGuests({ quiet: true })
    },
    setPage(page) {
      if (page < 1 || state.page === page) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        page,
      }
      void fetchGuests({ quiet: true })
    },
    goToPreviousPage() {
      if (state.page <= 1) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        page: state.page - 1,
      }
      void fetchGuests({ quiet: true })
    },
    goToNextPage() {
      const totalFilteredCount = state.viewModel?.totalFilteredCount ?? 0
      const maxPage = Math.max(
        1,
        Math.ceil(totalFilteredCount / DEFAULT_PAGE_SIZE)
      )
      if (state.page >= maxPage) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        page: state.page + 1,
      }
      void fetchGuests({ quiet: true })
    },
    toggleGuestSelection(guestId) {
      const nextSelectedGuestIds = toggleGuestInSelection(
        selectedGuestIds,
        guestId
      )

      if (selectionSetsEqual(selectedGuestIds, nextSelectedGuestIds)) {
        return
      }

      selectedGuestIds = nextSelectedGuestIds
      publish()
    },
    toggleSelectAllVisibleRows() {
      const visibleGuestIds =
        snapshot.viewModel?.tableRows.map((row) => row.id) ?? []
      const nextSelectedGuestIds = toggleAllVisibleInSelection(
        selectedGuestIds,
        visibleGuestIds
      )

      if (selectionSetsEqual(selectedGuestIds, nextSelectedGuestIds)) {
        return
      }

      selectedGuestIds = nextSelectedGuestIds
      publish()
    },
    clearSelection() {
      if (selectedGuestIds.size === 0) {
        return
      }

      selectedGuestIds = new Set()
      publish()
    },
    clearSearchAndFilters() {
      if (
        state.searchQuery === "" &&
        state.activeSmartGroupId === "all-guests" &&
        state.page === 1
      ) {
        return
      }

      clearSearchDebounce()
      clearSelectionIfNeeded()
      state = {
        ...state,
        searchQuery: "",
        activeSmartGroupId: "all-guests",
        page: 1,
      }
      void fetchGuests({ quiet: true })
    },
  }
}
